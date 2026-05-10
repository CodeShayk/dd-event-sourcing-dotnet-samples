// Chapter 18 — AccountStatementProjection tests
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SourceFlow;
using SourceFlow.Messaging.Bus;
using BankAccount.Domain.Commands;
using BankAccount.Domain.Views;
using BankAccount.Tests.Infrastructure;
using Xunit;

namespace BankAccount.Tests.Projections;

public sealed class AccountStatementProjectionTests : IAsyncLifetime
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
    public async Task OpenAccount_ShouldCreateStatementWithOpeningBalanceEntry()
    {
        var cmd = new OpenAccountCommand(
            true, new OpenAccountPayload("ACC-010", "Diana Prince", 500.00m));
        await _commandBus.Publish(cmd);

        var statement = await _viewModelStore.Get<AccountStatementView>(cmd.Entity.Id);
        statement.Should().NotBeNull();
        statement!.Entries.Should().HaveCount(1);
        statement.Entries[0].TransactionType.Should().Be("OpeningBalance");
        statement.Entries[0].Amount.Should().Be(500.00m);
        statement.Entries[0].RunningBalance.Should().Be(500.00m);
    }

    [Fact]
    public async Task MultipleTransactions_ShouldAppendEntriesInOrder()
    {
        var open = new OpenAccountCommand(
            true, new OpenAccountPayload("ACC-011", "Eve Torres", 1_000.00m));
        await _commandBus.Publish(open);
        int id = open.Entity.Id;

        await _commandBus.Publish(new DepositMoneyCommand(
            id, new DepositPayload(250.00m, "Bonus")));

        await _commandBus.Publish(new WithdrawMoneyCommand(
            id, new WithdrawPayload(100.00m, "ATM")));

        var statement = await _viewModelStore.Get<AccountStatementView>(id);
        statement!.Entries.Should().HaveCount(3);
        statement.Entries[1].TransactionType.Should().Be("Deposit");
        statement.Entries[1].Amount.Should().Be(250.00m);
        statement.Entries[2].TransactionType.Should().Be("Withdrawal");
        statement.Entries[2].Amount.Should().Be(-100.00m);
        statement.Entries[2].RunningBalance.Should().Be(1_150.00m);
        statement.CurrentBalance.Should().Be(1_150.00m);
    }

    [Fact]
    public async Task CloseAccount_ShouldAppendClosureEntryToStatement()
    {
        var open = new OpenAccountCommand(
            true, new OpenAccountPayload("ACC-012", "Frank Castle", 300.00m));
        await _commandBus.Publish(open);
        int id = open.Entity.Id;

        await _commandBus.Publish(new CloseAccountCommand(
            id, new CloseAccountPayload("Account holder deceased")));

        var statement = await _viewModelStore.Get<AccountStatementView>(id);
        statement!.Entries.Should().HaveCount(2);
        statement.Entries[1].TransactionType.Should().Be("AccountClosure");
        statement.Entries[1].Amount.Should().Be(0m);
        statement.Entries[1].RunningBalance.Should().Be(300.00m);
    }
}
