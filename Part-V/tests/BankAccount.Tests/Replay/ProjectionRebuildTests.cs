// Chapter 22 — Projection rebuild (replay) tests
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SourceFlow;
using SourceFlow.Messaging.Bus;
using BankAccount.Domain.Commands;
using BankAccount.Domain.Views;
using BankAccount.Tests.Infrastructure;
using Xunit;

namespace BankAccount.Tests.Replay;

public sealed class ProjectionRebuildTests : IAsyncLifetime
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
    public async Task Replay_ShouldRebuildAccountSummaryView_FromCommandHistory()
    {
        var open = new OpenAccountCommand(
            true, new OpenAccountPayload("ACC-300", "Replay Test", 1_000.00m));
        await _commandBus.Publish(open);
        int id = open.Entity.Id;

        await _commandBus.Publish(new DepositMoneyCommand(
            id, new DepositPayload(500.00m, "First deposit")));
        await _commandBus.Publish(new WithdrawMoneyCommand(
            id, new WithdrawPayload(200.00m, "First withdrawal")));

        var summary = await _viewModelStore.Get<AccountSummaryView>(id);
        await _viewModelStore.Delete(summary!);

        var deletedView = await _viewModelStore.Get<AccountSummaryView>(id);
        deletedView.Should().BeNull();

        await _commandBus.Replay(id);

        var rebuilt = await _viewModelStore.Get<AccountSummaryView>(id);
        rebuilt.Should().NotBeNull();
        rebuilt!.Balance.Should().Be(1_300.00m);
        rebuilt.AccountHolder.Should().Be("Replay Test");
        rebuilt.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Replay_ShouldRebuildAccountStatementView_WithAllEntries()
    {
        var open = new OpenAccountCommand(
            true, new OpenAccountPayload("ACC-301", "Statement Rebuild", 200.00m));
        await _commandBus.Publish(open);
        int id = open.Entity.Id;

        await _commandBus.Publish(new DepositMoneyCommand(
            id, new DepositPayload(100.00m, "Bonus")));

        var statement = await _viewModelStore.Get<AccountStatementView>(id);
        await _viewModelStore.Delete(statement!);

        await _commandBus.Replay(id);

        var rebuilt = await _viewModelStore.Get<AccountStatementView>(id);
        rebuilt.Should().NotBeNull();
        rebuilt!.Entries.Should().HaveCount(2);
        rebuilt.Entries[0].TransactionType.Should().Be("OpeningBalance");
        rebuilt.Entries[1].TransactionType.Should().Be("Deposit");
        rebuilt.CurrentBalance.Should().Be(300.00m);
    }

    [Fact]
    public async Task Replay_ShouldBeIdempotent_WhenProjectionIsCorrect()
    {
        var open = new OpenAccountCommand(
            true, new OpenAccountPayload("ACC-302", "Determinism Test", 500.00m));
        await _commandBus.Publish(open);
        int id = open.Entity.Id;

        await _commandBus.Publish(new DepositMoneyCommand(
            id, new DepositPayload(250.00m, "Deterministic deposit")));

        var beforeRebuild = await _viewModelStore.Get<AccountSummaryView>(id);
        var balanceBefore = beforeRebuild!.Balance;

        await _viewModelStore.Delete(beforeRebuild);
        await _commandBus.Replay(id);

        var afterRebuild = await _viewModelStore.Get<AccountSummaryView>(id);
        afterRebuild!.Balance.Should().Be(balanceBefore);
        afterRebuild.AccountHolder.Should().Be("Determinism Test");
    }
}
