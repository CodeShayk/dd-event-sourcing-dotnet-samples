// BankAccount.Application/Sagas/BankAccountSaga.cs
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SourceFlow;
using SourceFlow.Messaging.Commands;
using SourceFlow.Messaging.Events;
using SourceFlow.Saga;
using BankAccount.Domain;
using BankAccount.Domain.Commands;
using BankAccount.Domain.Events;
using BankAccountEntity = BankAccount.Domain.BankAccount;

namespace BankAccount.Application.Sagas;

/// <summary>
/// Orchestrates all Bank Account commands.
/// Extends Saga<BankAccountEntity> and implements IHandles / IHandlesWithEvent
/// for each of the four Bank Account operations.
/// </summary>
public sealed class BankAccountSaga
    : Saga<BankAccountEntity>,
      IHandlesWithEvent<OpenAccountCommand, AccountOpened>,
      IHandlesWithEvent<DepositMoneyCommand, MoneyDeposited>,
      IHandlesWithEvent<WithdrawMoneyCommand, MoneyWithdrawn>,
      IHandles<CloseAccountCommand>
{
    /// <summary>
    /// Initialises the saga with its framework-provided dependencies.
    /// </summary>
    public BankAccountSaga(
        Lazy<ICommandPublisher> commandPublisher,
        IEventQueue eventQueue,
        IEntityStoreAdapter entityStore,
        ILogger<ISaga> logger)
        : base(commandPublisher, eventQueue, entityStore, logger)
    {
    }

    /// <summary>
    /// Handles OpenAccountCommand: initialises the BankAccount entity.
    /// </summary>
    public Task<IEntity> Handle(IEntity entity, OpenAccountCommand command)
    {
        var account = (BankAccountEntity)entity;
        var payload = command.Payload;

        if (string.IsNullOrWhiteSpace(payload.HolderName))
            throw new ArgumentException("Account holder name is required.", nameof(command));

        if (payload.InitialDeposit <= 0)
            throw new ArgumentException(
                "Initial deposit must be greater than zero.", nameof(command));

        account.HolderName = payload.HolderName;
        account.Balance = payload.InitialDeposit;
        account.IsActive = true;
        account.OpenedOn = DateTimeOffset.UtcNow;

        return Task.FromResult<IEntity>(account);
    }

    /// <summary>
    /// Handles DepositMoneyCommand: adds the deposit amount to the account balance.
    /// </summary>
    public Task<IEntity> Handle(IEntity entity, DepositMoneyCommand command)
    {
        var account = (BankAccountEntity)entity;

        if (!account.IsActive)
            throw new InvalidOperationException(
                $"Cannot deposit to closed account {account.Id}.");

        if (command.Payload.Amount <= 0)
            throw new ArgumentException(
                "Deposit amount must be positive.", nameof(command));

        account.Balance += command.Payload.Amount;

        return Task.FromResult<IEntity>(account);
    }

    /// <summary>
    /// Handles WithdrawMoneyCommand: deducts the withdrawal amount from the balance.
    /// </summary>
    public Task<IEntity> Handle(IEntity entity, WithdrawMoneyCommand command)
    {
        var account = (BankAccountEntity)entity;

        if (!account.IsActive)
            throw new InvalidOperationException(
                $"Cannot withdraw from closed account {account.Id}.");

        if (command.Payload.Amount <= 0)
            throw new ArgumentException(
                "Withdrawal amount must be positive.", nameof(command));

        if (account.Balance < command.Payload.Amount)
            throw new InvalidOperationException(
                $"Insufficient funds. Balance: {account.Balance}, Requested: {command.Payload.Amount}.");

        account.Balance -= command.Payload.Amount;

        return Task.FromResult<IEntity>(account);
    }

    /// <summary>
    /// Handles CloseAccountCommand: marks the account inactive.
    /// Uses Raise&lt;TEvent&gt;() because event raising is conditional.
    /// </summary>
    public async Task<IEntity> Handle(IEntity entity, CloseAccountCommand command)
    {
        var account = (BankAccountEntity)entity;

        if (!account.IsActive)
        {
            logger.LogInformation(
                "Account {AccountId} is already closed. Ignoring CloseAccountCommand.",
                account.Id);
            return account;
        }

        account.IsActive = false;

        await Raise(new AccountClosed(account));

        return account;
    }
}
