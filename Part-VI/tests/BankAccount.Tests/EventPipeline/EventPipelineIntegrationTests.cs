// Chapter 17 — Event pipeline integration tests
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SourceFlow;
using SourceFlow.Messaging.Bus;
using BankAccount.Domain.Commands;
using BankAccount.Domain.Views;
using BankAccount.Tests.Infrastructure;
using Xunit;

namespace BankAccount.Tests.EventPipeline;

public sealed class EventPipelineIntegrationTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private ICommandBus _commandBus = null!;
    private IViewModelStore _viewModelStore = null!;

    public Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInMemorySourceFlow();
        _provider       = services.BuildServiceProvider();
        _commandBus     = _provider.GetRequiredService<ICommandBus>();
        _viewModelStore = _provider.GetRequiredService<IViewModelStore>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Deposit_ShouldUpdateAccountSummaryView_AfterEventPropagates()
    {
        var openCommand = new OpenAccountCommand(
            true, new OpenAccountPayload("ACC-001", "Alice Mercer", 1_000.00m));
        await _commandBus.Publish(openCommand);

        var summaryAfterOpen = await _viewModelStore.Get<AccountSummaryView>(openCommand.Entity.Id);
        summaryAfterOpen.Should().NotBeNull();
        summaryAfterOpen!.Balance.Should().Be(1_000.00m);

        var depositCommand = new DepositMoneyCommand(
            openCommand.Entity.Id,
            new DepositPayload(500.00m, "Salary"));
        await _commandBus.Publish(depositCommand);

        var summaryAfterDeposit = await _viewModelStore.Get<AccountSummaryView>(openCommand.Entity.Id);
        summaryAfterDeposit!.Balance.Should().Be(1_500.00m);
        summaryAfterDeposit.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CloseAccount_ShouldMarkSummaryViewInactive_AfterEventPropagates()
    {
        var openCommand = new OpenAccountCommand(
            true, new OpenAccountPayload("ACC-002", "Bob Wright", 200.00m));
        await _commandBus.Publish(openCommand);
        int id = openCommand.Entity.Id;

        var closeCommand = new CloseAccountCommand(
            id, new CloseAccountPayload("Customer request"));
        await _commandBus.Publish(closeCommand);

        var view = await _viewModelStore.Get<AccountSummaryView>(id);
        view!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task AccountOpened_ShouldCreateAccountSummaryView()
    {
        var openCommand = new OpenAccountCommand(
            true, new OpenAccountPayload("ACC-003", "Carol Liu", 750.00m));
        await _commandBus.Publish(openCommand);
        int id = openCommand.Entity.Id;

        var summary = await _viewModelStore.Get<AccountSummaryView>(id);
        summary.Should().NotBeNull();
        summary!.AccountHolder.Should().Be("Carol Liu");
        summary.Balance.Should().Be(750.00m);
        summary.IsActive.Should().BeTrue();

        var statement = await _viewModelStore.Get<AccountStatementView>(id);
        statement.Should().NotBeNull();
        statement!.Entries.Should().HaveCount(1);
        statement.Entries[0].TransactionType.Should().Be("OpeningBalance");
    }
}
