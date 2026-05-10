// Chapter 32 — Background service that long-polls SQS and routes commands to sagas
#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SourceFlow.Messaging.Commands;

namespace BankAccount.Infrastructure.Aws;

/// <summary>
/// A hosted background service that long-polls a configured SQS queue and routes
/// received commands to existing ICommandSubscriber instances.
/// </summary>
public sealed class AwsSqsCommandListener : BackgroundService
{
    private const int MaxMessages = 10;
    private const int WaitTimeSeconds = 20;

    private readonly IAmazonSQS _sqsClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly SqsRoutingOptions _routing;
    private readonly ILogger<AwsSqsCommandListener> _logger;

    public AwsSqsCommandListener(
        IAmazonSQS sqsClient,
        IServiceProvider serviceProvider,
        IOptions<SqsRoutingOptions> routing,
        ILogger<AwsSqsCommandListener> logger)
    {
        _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _routing = routing?.Value ?? throw new ArgumentNullException(nameof(routing));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AwsSqsCommandListener starting.");

        var queueUrls = new HashSet<string>(_routing.CommandRoutes.Values);

        if (queueUrls.Count == 0)
        {
            _logger.LogWarning(
                "AwsSqsCommandListener: No SQS queues configured. Listener will idle.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var queueUrl in queueUrls)
            {
                try
                {
                    await ProcessQueueAsync(queueUrl, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "AwsSqsCommandListener: Error processing queue {QueueUrl}. " +
                        "Will retry on next cycle.", queueUrl);
                }
            }
        }

        _logger.LogInformation("AwsSqsCommandListener stopped.");
    }

    private async Task ProcessQueueAsync(string queueUrl, CancellationToken stoppingToken)
    {
        var receiveRequest = new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = MaxMessages,
            WaitTimeSeconds = WaitTimeSeconds,
            MessageAttributeNames = new List<string> { "All" }
        };

        var response = await _sqsClient.ReceiveMessageAsync(receiveRequest, stoppingToken);

        foreach (var message in response.Messages)
        {
            await ProcessMessageAsync(queueUrl, message, stoppingToken);
        }
    }

    private async Task ProcessMessageAsync(
        string queueUrl,
        Message message,
        CancellationToken stoppingToken)
    {
        if (!message.MessageAttributes.TryGetValue("CommandType", out var commandTypeAttr))
        {
            _logger.LogWarning(
                "SqsListener: Message {MessageId} missing CommandType attribute. Skipping.",
                message.MessageId);
            return;
        }

        var commandTypeName = commandTypeAttr.StringValue;
        var commandType = Type.GetType(commandTypeName);

        if (commandType is null)
        {
            _logger.LogWarning(
                "SqsListener: Cannot resolve command type '{CommandType}'. Skipping message {MessageId}.",
                commandTypeName, message.MessageId);
            return;
        }

        ICommand? command;
        try
        {
            command = JsonSerializer.Deserialize(message.Body, commandType) as ICommand;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "SqsListener: Failed to deserialise command type '{CommandType}'. " +
                "Message {MessageId} will not be processed.",
                commandTypeName, message.MessageId);
            return;
        }

        if (command is null)
        {
            _logger.LogWarning(
                "SqsListener: Deserialised command is null for message {MessageId}. Skipping.",
                message.MessageId);
            return;
        }

        await using var scope = _serviceProvider.CreateAsyncScope();

        try
        {
            var subscriber = scope.ServiceProvider.GetRequiredService<ICommandSubscriber>();

            _logger.LogInformation(
                "SqsListener: Routing command {CommandType} for entity {EntityId}",
                commandType.Name,
                command.Entity?.Id);

            await subscriber.Subscribe(command);

            await _sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, stoppingToken);

            _logger.LogInformation(
                "SqsListener: Successfully processed and deleted message {MessageId}",
                message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "SqsListener: Command processing failed for message {MessageId}. " +
                "Message will retry after visibility timeout.",
                message.MessageId);
        }
    }
}
