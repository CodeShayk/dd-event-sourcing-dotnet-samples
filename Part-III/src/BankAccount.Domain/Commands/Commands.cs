// BankAccount.Domain/Commands/Commands.cs
using SourceFlow.Messaging.Commands;

namespace BankAccount.Domain.Commands;

/// <summary>
/// Command to open a new bank account.
/// Set EntityRef.IsNew = true so the framework initialises a fresh BankAccount entity
/// rather than attempting to load an existing one.
/// </summary>
public sealed class OpenAccountCommand : Command<OpenAccountPayload>
{
    /// <summary>Parameterless constructor required for deserialisation.</summary>
    public OpenAccountCommand() : base() { }

    /// <summary>Creates an OpenAccountCommand for a new account.</summary>
    public OpenAccountCommand(bool newEntity, OpenAccountPayload payload)
        : base(newEntity, payload) { }
}

/// <summary>Command to deposit money into an existing bank account.</summary>
public sealed class DepositMoneyCommand : Command<DepositMoneyPayload>
{
    /// <summary>Parameterless constructor required for deserialisation.</summary>
    public DepositMoneyCommand() : base() { }

    /// <summary>Creates a DepositMoneyCommand for an existing account.</summary>
    public DepositMoneyCommand(int accountId, DepositMoneyPayload payload)
        : base(accountId, payload) { }
}

/// <summary>Command to withdraw money from an existing bank account.</summary>
public sealed class WithdrawMoneyCommand : Command<WithdrawMoneyPayload>
{
    /// <summary>Parameterless constructor required for deserialisation.</summary>
    public WithdrawMoneyCommand() : base() { }

    /// <summary>Creates a WithdrawMoneyCommand for an existing account.</summary>
    public WithdrawMoneyCommand(int accountId, WithdrawMoneyPayload payload)
        : base(accountId, payload) { }
}

/// <summary>Command to close an existing bank account.</summary>
public sealed class CloseAccountCommand : Command<CloseAccountPayload>
{
    /// <summary>Parameterless constructor required for deserialisation.</summary>
    public CloseAccountCommand() : base() { }

    /// <summary>Creates a CloseAccountCommand for an existing account.</summary>
    public CloseAccountCommand(int accountId)
        : base(accountId, new CloseAccountPayload()) { }
}
