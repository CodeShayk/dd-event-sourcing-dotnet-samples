// Chapter 19 — AccountSummaryProjection tests
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SourceFlow;
using SourceFlow.Messaging.Bus;
using BankAccount.Domain.Commands;
using BankAccount.Domain.Views;
using BankAccount.Tests.Infrastructure;
using Xunit;

namespace BankAccount.Tests.Projections;

public sealed class AccountSummaryProjectionTests : IAsyncLifetime
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
    public async Task Withdrawal_ShouldDecrementBalance_InSummaryView()
    {
        var open = new OpenAccountCommand(
            true, new OpenAccountPayload("ACC-020", "Grace Hopper", 800.00m));
        await _commandBus.Publish(open);
        int id = open.Entity.Id;

        await _commandBus.Publish(new WithdrawMoneyCommand(
            id, new WithdrawPayload(300.00m, "Groceries")));

        var view = await _viewModelStore.Get<AccountSummaryView>(id);
        view!.Balance.Should().Be(500.00m);
        view.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task OpenAndClose_ShouldMarkViewInactive_WithCorrectBalance()
    {
        var open = new OpenAccountCommand(
            true, new OpenAccountPayload("ACC-021", "Hank Pym", 100.00m));
        await _commandBus.Publish(open);
        int id = open.Entity.Id;

        await _commandBus.Publish(new CloseAccountCommand(
            id, new CloseAccountPayload("Merged account")));

        var view = await _viewModelStore.Get<AccountSummaryView>(id);
        view!.IsActive.Should().BeFalse();
        view.Balance.Should().Be(100.00m);
    }

    [Fact]
    public async Task SequentialDeposits_ShouldAccumulateBalance_Correctly()
    {
        var open = new OpenAccountCommand(
            true, new OpenAccountPayload("ACC-022", "Iris West", 0.00m));
        await _commandBus.Publish(open);
        int id = open.Entity.Id;

        await _commandBus.Publish(new DepositMoneyCommand(
            id, new DepositPayload(100.00m, "Deposit 1")));
        await _commandBus.Publish(new DepositMoneyCommand(
            id, new DepositPayload(200.00m, "Deposit 2")));
        await _commandBus.Publish(new DepositMoneyCommand(
            id, new DepositPayload(300.00m, "Deposit 3")));

        var view = await _viewModelStore.Get<AccountSummaryView>(id);
        view!.Balance.Should().Be(600.00m);
    }
}
