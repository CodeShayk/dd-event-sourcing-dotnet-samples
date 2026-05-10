// Chapter 21 — Three-layer validation tests (Layers 2 & 3)
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SourceFlow.Messaging.Bus;
using BankAccount.Domain.Commands;
using BankAccount.Domain.Exceptions;
using BankAccount.Tests.Infrastructure;
using Xunit;

namespace BankAccount.Tests.Validation;

public sealed class ThreeLayerValidationTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private ICommandBus _commandBus = null!;

    public Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInMemorySourceFlow();
        _provider   = services.BuildServiceProvider();
        _commandBus = _provider.GetRequiredService<ICommandBus>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Withdraw_WhenBalanceInsufficient_ShouldThrow_InsufficientFundsException()
    {
        var open = new OpenAccountCommand(
            true, new OpenAccountPayload("ACC-200", "Layer Three Test", 50.00m));
        await _commandBus.Publish(open);
        int id = open.Entity.Id;

        var act = () => _commandBus.Publish(new WithdrawMoneyCommand(
            id, new WithdrawPayload(200.00m, "Overdraft attempt")));

        await act.Should().ThrowAsync<InsufficientFundsException>()
            .WithMessage("*insufficient funds*");
    }

    [Fact]
    public async Task Deposit_WhenAccountClosed_ShouldThrow_AccountClosedException()
    {
        var open = new OpenAccountCommand(
            true, new OpenAccountPayload("ACC-201", "Closed Account Test", 100.00m));
        await _commandBus.Publish(open);
        int id = open.Entity.Id;

        await _commandBus.Publish(new CloseAccountCommand(
            id, new CloseAccountPayload("Deliberate closure")));

        var act = () => _commandBus.Publish(new DepositMoneyCommand(
            id, new DepositPayload(50.00m, "Post-close deposit")));

        await act.Should().ThrowAsync<AccountClosedException>()
            .WithMessage("*closed*");
    }

    [Fact]
    public async Task CloseAccount_WhenAlreadyClosed_ShouldSucceed_Idempotently()
    {
        var open = new OpenAccountCommand(
            true, new OpenAccountPayload("ACC-202", "Idempotency Test", 100.00m));
        await _commandBus.Publish(open);
        int id = open.Entity.Id;

        await _commandBus.Publish(new CloseAccountCommand(
            id, new CloseAccountPayload("First closure")));

        var act = () => _commandBus.Publish(new CloseAccountCommand(
            id, new CloseAccountPayload("Duplicate closure")));

        await act.Should().NotThrowAsync();
    }
}
