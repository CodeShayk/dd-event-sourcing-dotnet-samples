// Chapter 32 — Custom ICommandDispatcher that selectively sends commands to AWS SQS
#nullable enable

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SourceFlow.Messaging;
using SourceFlow.Messaging.Commands;

namespace BankAccount.Infrastructure.Aws;

/// <summary>
/// A custom ICommandDispatcher that selectively sends commands to AWS SQS.
/// Only commands with a configured SQS queue route are forwarded.
/// Commands not configured for SQS are silently ignored (processed locally only).
/// </summary>
public sealed class AwsSqsCommandDispatcher : ICommandDispatcher
{
    private readonly IAmazonSQS _sqsClient;
    private readonly SqsRoutingOptions _routing;
    private readonly ILogger<AwsSqsCommandDispatcher> _logger;

    public AwsSqsCommandDispatcher(
        IAmazonSQS sqsClient,
        IOptions<SqsRoutingOptions> routing,
        ILogger<AwsSqsCommandDispatcher> logger)
    {
        _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
        _routing = routing?.Value ?? throw new ArgumentNullException(nameof(routing));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Dispatch<TCommand>(TCommand command)
        where TCommand : ICommand
    {
        if (command is null)
            return;

        if (!_routing.ShouldRouteToSqs<TCommand>())
        {
            _logger.LogDebug(
                "SqsDispatcher: Skipping command {CommandType} — no SQS route configured",
                typeof(TCommand).Name);
            return;
        }

        var queueUrl = _routing.GetQueueUrl<TCommand>();
        var metadata = (IMetadata)command;

        var messageBody = JsonSerializer.Serialize(command, command.GetType());

        var request = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = messageBody,
            MessageAttributes = new()
            {
                ["CommandType"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = command.GetType().AssemblyQualifiedName
                },
                ["EntityId"] = new MessageAttributeValue
                {
                    DataType = "Number",
                    StringValue = command.Entity.Id.ToString()
                },
                ["SequenceNo"] = new MessageAttributeValue
                {
                    DataType = "Number",
                    StringValue = metadata.Metadata.SequenceNo.ToString()
                }
            }
        };

        _logger.LogInformation(
            "SqsDispatcher: Sending command {CommandType} for entity {EntityId} " +
            "to queue {QueueUrl}",
            typeof(TCommand).Name,
            command.Entity.Id,
            queueUrl);

        await _sqsClient.SendMessageAsync(request);
    }
}
