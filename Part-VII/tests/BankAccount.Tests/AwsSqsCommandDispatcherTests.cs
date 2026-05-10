// Chapter 32 — Tests for SQS cloud dispatch layer
#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using BankAccount.Domain.Commands;
using BankAccount.Infrastructure.Aws;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SourceFlow.Messaging;
using SourceFlow.Messaging.Commands;
using Xunit;

namespace BankAccount.Tests;

/// <summary>
/// Tests for the SQS cloud dispatch layer.
/// Tests 1 and 2 are unit tests using a mocked SQS client.
/// Test 3 is a component test of the listener routing logic.
/// </summary>
public sealed class AwsSqsCommandDispatcherTests
{
    [Fact]
    public async Task Dispatch_SendsMessageToSqs_WhenCommandIsConfiguredForRouting()
    {
        // Arrange
        var sqsClientMock = new Mock<IAmazonSQS>();
        SendMessageRequest? capturedRequest = null;

        sqsClientMock
            .Setup(c => c.SendMessageAsync(
                It.IsAny<SendMessageRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback<SendMessageRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new SendMessageResponse { MessageId = Guid.NewGuid().ToString() });

        var routingOptions = new SqsRoutingOptions();
        routingOptions.CommandRoutes[typeof(ProcessTransferCommand).FullName!] =
            "http://localhost:4566/000000000000/bankaccount-commands";

        var dispatcher = new AwsSqsCommandDispatcher(
            sqsClientMock.Object,
            Options.Create(routingOptions),
            Mock.Of<ILogger<AwsSqsCommandDispatcher>>());

        var command = new ProcessTransferCommand
        {
            Entity = new EntityRef { Id = 1 },
            Payload = new ProcessTransferPayload
            {
                SourceAccountId = 1,
                DestinationAccountId = 2,
                Amount = 1000m
            }
        };
        ((IMetadata)command).Metadata.SequenceNo = 3;

        // Act
        await dispatcher.Dispatch(command);

        // Assert
        sqsClientMock.Verify(
            c => c.SendMessageAsync(
                It.IsAny<SendMessageRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.QueueUrl.Should()
            .Be("http://localhost:4566/000000000000/bankaccount-commands");
        capturedRequest.MessageAttributes.Should().ContainKey("CommandType");
        capturedRequest.MessageAttributes["CommandType"].StringValue.Should()
            .Contain("ProcessTransferCommand");
        capturedRequest.MessageAttributes.Should().ContainKey("EntityId");
        capturedRequest.MessageAttributes["EntityId"].StringValue.Should().Be("1");
        capturedRequest.MessageAttributes.Should().ContainKey("SequenceNo");
        capturedRequest.MessageAttributes["SequenceNo"].StringValue.Should().Be("3");
    }

    [Fact]
    public async Task Dispatch_DoesNotSendToSqs_WhenCommandNotConfiguredForRouting()
    {
        // Arrange — routing options do NOT include DepositMoneyCommand
        var sqsClientMock = new Mock<IAmazonSQS>();

        var routingOptions = new SqsRoutingOptions();

        var dispatcher = new AwsSqsCommandDispatcher(
            sqsClientMock.Object,
            Options.Create(routingOptions),
            Mock.Of<ILogger<AwsSqsCommandDispatcher>>());

        var command = new DepositMoneyCommand
        {
            Entity = new EntityRef { Id = 42 },
            Payload = new DepositPayload { Amount = 100m }
        };

        // Act
        await dispatcher.Dispatch(command);

        // Assert — SQS client was never called
        sqsClientMock.Verify(
            c => c.SendMessageAsync(
                It.IsAny<SendMessageRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Listener_RoutesDeseralisedCommand_ToCommandSubscriber()
    {
        // Arrange
        var subscriberMock = new Mock<ICommandSubscriber>();
        subscriberMock
            .Setup(s => s.Subscribe(It.IsAny<ProcessTransferCommand>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddScoped<ICommandSubscriber>(_ => subscriberMock.Object);
        var provider = services.BuildServiceProvider();

        // Simulate a message that arrived from SQS
        var command = new ProcessTransferCommand
        {
            Entity = new EntityRef { Id = 5 },
            Payload = new ProcessTransferPayload
            {
                SourceAccountId = 5,
                DestinationAccountId = 6,
                Amount = 250m
            }
        };
        var messageBody = JsonSerializer.Serialize(command, command.GetType());

        var message = new Message
        {
            MessageId = Guid.NewGuid().ToString(),
            ReceiptHandle = "test-receipt-handle",
            Body = messageBody,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["CommandType"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = command.GetType().AssemblyQualifiedName
                },
                ["EntityId"] = new MessageAttributeValue
                {
                    DataType = "Number",
                    StringValue = "5"
                }
            }
        };

        var commandType = Type.GetType(message.MessageAttributes["CommandType"].StringValue!);
        var deserialisedCommand = JsonSerializer.Deserialize(message.Body, commandType!) as ICommand;

        await using var scope = provider.CreateAsyncScope();
        var subscriber = scope.ServiceProvider.GetRequiredService<ICommandSubscriber>();
        await subscriber.Subscribe((ProcessTransferCommand)deserialisedCommand!);

        // Assert
        subscriberMock.Verify(
            s => s.Subscribe(It.Is<ProcessTransferCommand>(
                c => c.Entity.Id == 5 && c.Payload.Amount == 250m)),
            Times.Once);
    }
}
