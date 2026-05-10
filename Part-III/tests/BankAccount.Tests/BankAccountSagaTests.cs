// BankAccount.Tests/BankAccountSagaTests.cs
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SourceFlow;
using SourceFlow.Messaging.Commands;
using SourceFlow.Messaging.Events;
using SourceFlow.Saga;
using BankAccount.Application.Sagas;
using BankAccount.Domain;
using BankAccount.Domain.Commands;
using BankAccount.Domain.Events;
using Xunit;

namespace BankAccount.Tests;

/// <summary>
/// Unit tests for BankAccountSaga.
/// Dependencies are replaced with mocks to isolate domain logic.
/// </summary>
public class BankAccountSagaTests
{
    private readonly Mock<IEventQueue> _eventQueue;
    private readonly Mock<IEntityStoreAdapter> _entityStore;
    private readonly BankAccountSaga _saga;

    public BankAccountSagaTests()
    {
        _eventQueue = new Mock<IEventQueue>();
        _entityStore = new Mock<IEntityStoreAdapter>();

        var commandPublisher = new Lazy<ICommandPublisher>(() =>
            new Mock<ICommandPublisher>().Object);

        _saga = new BankAccountSaga(
            commandPublisher,
            _eventQueue.Object,
            _entityStore.Object,
            NullLogger<ISaga>.Instance
        );
    }

    [Fact]
    public async Task Handle_OpenAccountCommand_InitialisesAccountCorrectly()
    {
        var entity = new BankAccountEntity { Id = 1 };
        var command = new OpenAccountCommand(true, new OpenAccountPayload("Alice Dewhurst", 500m));

        var result = await ((IHandles<OpenAccountCommand>)_saga).Handle(entity, command);

        var account = result.Should().BeOfType<BankAccountEntity>().Subject;
        account.HolderName.Should().Be("Alice Dewhurst");
        account.Balance.Should().Be(500m);
        account.IsActive.Should().BeTrue();
        account.OpenedOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_DepositMoneyCommand_AddsToBalance()
    {
        var entity = new BankAccountEntity { Id = 42, Balance = 0m, IsActive = true };
        var command = new DepositMoneyCommand(42, new DepositMoneyPayload(100m));

        var result = await ((IHandles<DepositMoneyCommand>)_saga).Handle(entity, command);

        var account = result.Should().BeOfType<BankAccountEntity>().Subject;
        account.Balance.Should().Be(100m);
    }

    [Fact]
    public async Task Handle_DepositMoneyCommand_DoesNotRaiseEventDirectly()
    {
        var entity = new BankAccountEntity { Id = 42, Balance = 0m, IsActive = true };
        var command = new DepositMoneyCommand(42, new DepositMoneyPayload(100m));

        await ((IHandles<DepositMoneyCommand>)_saga).Handle(entity, command);

        _eventQueue.Verify(q => q.Enqueue(It.IsAny<MoneyDeposited>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithdrawMoneyCommand_ThrowsOnInsufficientFunds()
    {
        var entity = new BankAccountEntity { Id = 42, Balance = 50m, IsActive = true };
        var command = new WithdrawMoneyCommand(42, new WithdrawMoneyPayload(200m));

        var act = async () => await ((IHandles<WithdrawMoneyCommand>)_saga).Handle(entity, command);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Insufficient funds*");
    }

    [Fact]
    public async Task Handle_CloseAccountCommand_MarksAccountInactiveAndRaisesEvent()
    {
        var entity = new BankAccountEntity { Id = 42, Balance = 0m, IsActive = true };
        var command = new CloseAccountCommand(42);

        var result = await ((IHandles<CloseAccountCommand>)_saga).Handle(entity, command);

        var account = result.Should().BeOfType<BankAccountEntity>().Subject;
        account.IsActive.Should().BeFalse();

        _eventQueue.Verify(q => q.Enqueue(It.Is<AccountClosed>(e => e.Payload.Id == 42)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CloseAccountCommand_IsIdempotent_WhenAlreadyClosed()
    {
        var entity = new BankAccountEntity { Id = 42, Balance = 0m, IsActive = false };
        var command = new CloseAccountCommand(42);

        var result = await ((IHandles<CloseAccountCommand>)_saga).Handle(entity, command);

        _eventQueue.Verify(q => q.Enqueue(It.IsAny<AccountClosed>()), Times.Never);
        ((BankAccountEntity)result).IsActive.Should().BeFalse();
    }
}
