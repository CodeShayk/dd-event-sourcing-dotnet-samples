// BankAccount.Domain/BankAccount.cs (extended for replay)
#nullable enable

using System;

namespace BankAccount.Domain;

/// <summary>
/// The BankAccount aggregate, extended with Apply() methods for replay.
///
/// Handle() — production path: mutates state AND raises domain events.
/// Apply()  — replay path: mutates state ONLY. No events raised.
///
/// The critical rule: Apply() must never call any method that has side effects
/// beyond updating the aggregate's own fields. No event raising, no external
/// calls, no logging that triggers alerts.
/// </summary>
public sealed class BankAccount
{
    /// <summary>The unique identifier of this account.</summary>
    public int Id { get; private set; }

    /// <summary>The name of the account owner.</summary>
    public string OwnerName { get; private set; } = string.Empty;

    /// <summary>The current balance.</summary>
    public decimal Balance { get; private set; }

    /// <summary>Whether this account has been closed.</summary>
    public bool IsClosed { get; private set; }

    // -------------------------------------------------------------------------
    // PRODUCTION PATH — Handle() methods raise domain events after state change.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Production handler for OpenAccountCommand.
    /// Mutates state and raises AccountOpened.
    /// </summary>
    public void Handle(OpenAccountCommand command, Action<object>? raiseEvent = null)
    {
        if (command.InitialDeposit < 0)
            throw new ArgumentOutOfRangeException(nameof(command), "Initial deposit cannot be negative.");

        Id = command.AccountId;
        OwnerName = command.OwnerName;
        Balance = command.InitialDeposit;
        IsClosed = false;

        raiseEvent?.Invoke(new AccountOpenedEvent(command.AccountId, command.OwnerName));
    }

    /// <summary>
    /// Production handler for DepositMoneyCommand.
    /// Mutates state and raises MoneyDeposited.
    /// </summary>
    public void Handle(DepositMoneyCommand command, Action<object>? raiseEvent = null)
    {
        if (IsClosed)
            throw new InvalidOperationException("Cannot deposit to a closed account.");
        if (command.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(command), "Deposit amount must be positive.");

        Balance += command.Amount;

        raiseEvent?.Invoke(new MoneyDepositedEvent(command.AccountId, command.Amount, Balance));
    }

    /// <summary>
    /// Production handler for WithdrawMoneyCommand.
    /// Mutates state and raises MoneyWithdrawn.
    /// </summary>
    public void Handle(WithdrawMoneyCommand command, Action<object>? raiseEvent = null)
    {
        if (IsClosed)
            throw new InvalidOperationException("Cannot withdraw from a closed account.");
        if (command.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(command), "Withdrawal amount must be positive.");
        if (command.Amount > Balance)
            throw new InvalidOperationException("Insufficient funds.");

        Balance -= command.Amount;

        raiseEvent?.Invoke(new MoneyWithdrawnEvent(command.AccountId, command.Amount, Balance));
    }

    /// <summary>
    /// Production handler for CloseAccountCommand.
    /// Mutates state and raises AccountClosed.
    /// </summary>
    public void Handle(CloseAccountCommand command, Action<object>? raiseEvent = null)
    {
        if (IsClosed)
            throw new InvalidOperationException("Account is already closed.");

        IsClosed = true;

        raiseEvent?.Invoke(new AccountClosedEvent(command.AccountId, command.Reason));
    }

    // -------------------------------------------------------------------------
    // REPLAY PATH — Apply() methods mutate state ONLY. No events. No side effects.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Replay application of OpenAccountCommand. State mutation only.
    /// </summary>
    public void Apply(OpenAccountCommand command)
    {
        Id = command.AccountId;
        OwnerName = command.OwnerName;
        Balance = command.InitialDeposit;
        IsClosed = false;
    }

    /// <summary>
    /// Replay application of DepositMoneyCommand. State mutation only.
    /// </summary>
    public void Apply(DepositMoneyCommand command)
    {
        Balance += command.Amount;
    }

    /// <summary>
    /// Replay application of WithdrawMoneyCommand. State mutation only.
    /// </summary>
    public void Apply(WithdrawMoneyCommand command)
    {
        Balance -= command.Amount;
    }

    /// <summary>
    /// Replay application of CloseAccountCommand. State mutation only.
    /// </summary>
    public void Apply(CloseAccountCommand command)
    {
        IsClosed = true;
    }
}
