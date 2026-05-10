// Chapter 21 — BankAccountSaga (Part IV evolution with three-layer validation)
using SourceFlow;
using SourceFlow.Saga;
using SourceFlow.Messaging.Commands;
using SourceFlow.Messaging.Events;
using BankAccount.Domain.Commands;
using BankAccount.Domain.Events;
using BankAccount.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using BankAccountEntity = BankAccount.Domain.BankAccount;

namespace BankAccount.Domain.Sagas;

/// <summary>
/// Saga for the BankAccount aggregate. Handles all four commands and enforces
/// business rules at the saga layer (Layer 2 of three-layer validation).
/// </summary>
public sealed class BankAccountSaga :
    Saga<BankAccountEntity>,
    IHandlesWithEvent<OpenAccountCommand, AccountOpened>,
    IHandlesWithEvent<DepositMoneyCommand, MoneyDeposited>,
    IHandlesWithEvent<WithdrawMoneyCommand, MoneyWithdrawn>,
    IHandles<CloseAccountCommand>
{
    public BankAccountSaga(
        Lazy<ICommandPublisher> commandPublisher,
        IEventQueue eventQueue,
        IEntityStoreAdapter entityStore,
        ILogger<ISaga> logger)
        : base(commandPublisher, eventQueue, entityStore, logger) { }

    public Task<IEntity> Handle(IEntity entity, OpenAccountCommand command)
    {
        var payload = command.Payload;
        var account = new BankAccountEntity
        {
            AccountNumber  = payload.AccountNumber,
            AccountHolder  = payload.AccountHolder,
            Balance        = payload.InitialBalance,
            IsActive       = true,
            CreatedDate    = DateTime.UtcNow
        };

        return Task.FromResult<IEntity>(account);
    }

    public Task<IEntity> Handle(IEntity entity, DepositMoneyCommand command)
    {
        var account = (BankAccountEntity)entity;

        if (!account.IsActive)
            throw new AccountClosedException(account.Id);

        account.Deposit(command.Payload.Amount);

        return Task.FromResult<IEntity>(account);
    }

    public Task<IEntity> Handle(IEntity entity, WithdrawMoneyCommand command)
    {
        var account = (BankAccountEntity)entity;

        if (!account.IsActive)
            throw new AccountClosedException(account.Id);

        account.Withdraw(command.Payload.Amount);

        return Task.FromResult<IEntity>(account);
    }

    public async Task<IEntity> Handle(IEntity entity, CloseAccountCommand command)
    {
        var account = (BankAccountEntity)entity;

        if (!account.IsActive)
        {
            logger.LogInformation(
                "CloseAccount ignored: account {Id} is already closed.", account.Id);
            return account;
        }

        account.IsActive = false;
        await Raise(new AccountClosed(account));

        return account;
    }
}
