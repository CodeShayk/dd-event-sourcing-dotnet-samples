// Chapter 30 — Tests for AuditCommandDispatcher
#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BankAccount.Domain.Commands;
using BankAccount.Infrastructure.Audit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SourceFlow;
using SourceFlow.Messaging;
using SourceFlow.Messaging.Commands;
using Xunit;

namespace BankAccount.Tests;

/// <summary>
/// Verifies that AuditCommandDispatcher participates in the command pipeline
/// alongside the default CommandDispatcher and can be removed without code changes.
/// </summary>
public sealed class AuditCommandDispatcherTests
{
    [Fact]
    public async Task BothDispatchersReceiveSameCommand_WhenBothRegistered()
    {
        // Arrange
        var auditLogger = new Mock<ILogger<AuditCommandDispatcher>>();
        var defaultDispatcherMock = new Mock<ICommandDispatcher>();
        defaultDispatcherMock
            .Setup(d => d.Dispatch(It.IsAny<DepositMoneyCommand>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddScoped<ICommandDispatcher>(_ => defaultDispatcherMock.Object);
        services.AddScoped<ICommandDispatcher>(
            _ => new AuditCommandDispatcher(auditLogger.Object));

        var provider = services.BuildServiceProvider();

        var dispatchers = provider.GetRequiredService<IEnumerable<ICommandDispatcher>>();

        var command = new DepositMoneyCommand
        {
            Entity = new EntityRef { Id = 42 },
            Payload = new DepositPayload { Amount = 100m }
        };

        // Act
        foreach (var dispatcher in dispatchers)
            await dispatcher.Dispatch(command);

        // Assert — default dispatcher received the command
        defaultDispatcherMock.Verify(
            d => d.Dispatch(It.Is<DepositMoneyCommand>(c => c.Entity.Id == 42)),
            Times.Once);

        // Assert — audit dispatcher wrote a log entry
        auditLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("DepositMoneyCommand") &&
                    v.ToString()!.Contains("42")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RemovingAuditDispatcher_LeavesOnlyDefaultDispatcher()
    {
        // Arrange — register only the default dispatcher
        var defaultDispatcherMock = new Mock<ICommandDispatcher>();

        var services = new ServiceCollection();
        services.AddScoped<ICommandDispatcher>(_ => defaultDispatcherMock.Object);

        var provider = services.BuildServiceProvider();

        // Act
        var dispatchers = provider.GetServices<ICommandDispatcher>();

        // Assert
        dispatchers.Should().HaveCount(1);
        dispatchers.Should().AllBeAssignableTo<ICommandDispatcher>();
        dispatchers.Should().NotContain(d => d is AuditCommandDispatcher);
    }

    [Fact]
    public async Task AuditDispatcher_LogsIsReplay_WhenCommandIsReplay()
    {
        // Arrange
        var auditLogger = new Mock<ILogger<AuditCommandDispatcher>>();
        var dispatcher = new AuditCommandDispatcher(auditLogger.Object);

        var command = new DepositMoneyCommand
        {
            Entity = new EntityRef { Id = 99 },
            Payload = new DepositPayload { Amount = 50m }
        };
        ((IMetadata)command).Metadata.IsReplay = true;
        ((IMetadata)command).Metadata.SequenceNo = 7;

        // Act
        await dispatcher.Dispatch(command);

        // Assert — log entry contains IsReplay=True
        auditLogger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("IsReplay=True") &&
                    v.ToString()!.Contains("SequenceNo=7")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
