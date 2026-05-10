// BankAccount.Tests/CommandPipelineTests.cs
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using BankAccount.Application.Sagas;
using SourceFlow;
using SourceFlow.Messaging;
using SourceFlow.Messaging.Bus;
using SourceFlow.Messaging.Commands;
using BankAccount.Domain.Commands;
using Xunit;

namespace BankAccount.Tests;

/// <summary>
/// Verifies the command pipeline's sequencing and persistence behaviour.
/// </summary>
public class CommandPipelineTests
{
    private readonly ServiceProvider _services;

    public CommandPipelineTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.UseSourceFlow(typeof(BankAccountSaga).Assembly);
        serviceCollection.AddScoped<IEntityStore, InMemoryEntityStore>();
        serviceCollection.AddScoped<ICommandStore, InMemoryCommandStore>();
        serviceCollection.AddScoped<IViewModelStore, InMemoryViewModelStore>();
        serviceCollection.AddLogging();
        _services = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public async Task Publish_AssignsSequenceNumber_BeforeDispatch()
    {
        using var scope = _services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<ICommandBus>();

        var command = new OpenAccountCommand(true, new OpenAccountPayload("Bob", 200m));
        await bus.Publish(command);

        ((IMetadata)command).Metadata.SequenceNo.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Replay_DoesNotReassignSequenceNumber()
    {
        using var scope = _services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<ICommandBus>();

        // First, create an account so the entity store has it
        var openCommand = new OpenAccountCommand(true, new OpenAccountPayload("Test", 500m));
        await bus.Publish(openCommand);
        int entityId = openCommand.Entity.Id;

        var command = new DepositMoneyCommand(entityId, new DepositMoneyPayload(100m));
        ((IMetadata)command).Metadata.IsReplay = true;
        var originalSequenceNo = ((IMetadata)command).Metadata.SequenceNo;

        await bus.Publish(command);

        ((IMetadata)command).Metadata.SequenceNo.Should().Be(originalSequenceNo);
    }
}
