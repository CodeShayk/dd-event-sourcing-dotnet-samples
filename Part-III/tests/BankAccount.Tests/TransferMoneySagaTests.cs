// BankAccount.Tests/TransferMoneySagaTests.cs
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
using Xunit;

namespace BankAccount.Tests;

public class TransferMoneySagaTests
{
    private readonly Mock<ICommandPublisher> _commandPublisher;
    private readonly Mock<IEventQueue> _eventQueue;
    private readonly Mock<IEntityStoreAdapter> _entityStore;
    private readonly TransferMoneySaga _saga;

    public TransferMoneySagaTests()
    {
        _commandPublisher = new Mock<ICommandPublisher>();
        _eventQueue = new Mock<IEventQueue>();
        _entityStore = new Mock<IEntityStoreAdapter>();

        var lazyPublisher = new Lazy<ICommandPublisher>(() => _commandPublisher.Object);

        _saga = new TransferMoneySaga(
            lazyPublisher,
            _eventQueue.Object,
            _entityStore.Object,
            NullLogger<ISaga>.Instance
        );
    }

    [Fact]
    public async Task Handle_TransferMoneyCommand_PublishesDebitAccountCommand()
    {
        var sourceAccount = new BankAccountEntity { Id = 10, Balance = 500m, IsActive = true };
        var payload = new TransferMoneyPayload(10, 20, 200m);
        var command = new TransferMoneyCommand(payload);

        await ((IHandles<TransferMoneyCommand>)_saga).Handle(sourceAccount, command);

        _commandPublisher.Verify(
            p => p.Publish(It.Is<DebitAccountCommand>(c =>
                c.Entity.Id == 10 && c.Payload.Amount == 200m)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CreditAccountCommand_CreditsTargetAccount()
    {
        var targetAccount = new BankAccountEntity { Id = 20, Balance = 100m, IsActive = true };
        var command = new CreditAccountCommand(20,
            new CreditAccountPayload(200m, Guid.NewGuid(), sourceAccountId: 10));

        var result = await ((IHandles<CreditAccountCommand>)_saga).Handle(targetAccount, command);

        result.Should().BeOfType<BankAccountEntity>().Which.Balance.Should().Be(300m);
        _commandPublisher.Verify(p => p.Publish(It.IsAny<ReverseDebitCommand>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CreditAccountCommand_PublishesReverseDebit_WhenTargetIsClosed()
    {
        var closedTarget = new BankAccountEntity { Id = 20, Balance = 0m, IsActive = false };
        var transferId = Guid.NewGuid();
        var command = new CreditAccountCommand(20,
            new CreditAccountPayload(200m, transferId, sourceAccountId: 10));

        await ((IHandles<CreditAccountCommand>)_saga).Handle(closedTarget, command);

        _commandPublisher.Verify(
            p => p.Publish(It.Is<ReverseDebitCommand>(c =>
                c.Entity.Id == 10 &&
                c.Payload.TransferId == transferId &&
                c.Payload.Amount == 200m)),
            Times.Once);

        closedTarget.Balance.Should().Be(0m);
    }
}
