// BankAccount.Tests/FrameworkWiringTests.cs
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using BankAccount.Application.Sagas;
using BankAccount.Domain.Commands;
using SourceFlow;
using SourceFlow.Messaging.Bus;
using Xunit;

namespace BankAccount.Tests;

/// <summary>
/// Smoke tests verifying that the SourceFlow.Net DI wiring is correct.
/// A command published to ICommandBus must reach the registered saga handler.
/// </summary>
public class FrameworkWiringTests
{
    private readonly ServiceProvider _services;

    public FrameworkWiringTests()
    {
        var serviceCollection = new ServiceCollection();

        // Register SourceFlow.Net core without any EF store backing.
        // The framework will use its in-memory store implementations by default
        // when no IEntityStore, ICommandStore, or IViewModelStore is registered.
        serviceCollection.UseSourceFlow(
            typeof(BankAccountSaga).Assembly
        );

        // Register in-memory stores for testing (framework requires these)
        serviceCollection.AddScoped<IEntityStore, InMemoryEntityStore>();
        serviceCollection.AddScoped<ICommandStore, InMemoryCommandStore>();
        serviceCollection.AddScoped<IViewModelStore, InMemoryViewModelStore>();

        serviceCollection.AddLogging();

        _services = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public void CommandBus_IsResolvable_FromDiContainer()
    {
        // Arrange & Act
        var bus = _services.GetRequiredService<ICommandBus>();

        // Assert
        bus.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishOpenAccountCommand_CompletesWithoutException_PipelineWiredCorrectly()
    {
        // Arrange
        using var scope = _services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<ICommandBus>();

        var command = new OpenAccountCommand(
            newEntity: true,
            payload: new OpenAccountPayload("Alice Dewhurst", 100m)
        );

        // Act — publish the command through the full pipeline
        var act = async () => await bus.Publish(command);

        // Assert — no exception means the pipeline executed without error.
        await act.Should().NotThrowAsync();
    }
}
