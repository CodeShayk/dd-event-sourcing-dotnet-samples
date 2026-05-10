// BankAccount.Domain/Events/Events.cs
using SourceFlow.Messaging.Events;
using BankAccount.Domain;

namespace BankAccount.Domain.Events;

/// <summary>Domain event raised when a new bank account is opened.</summary>
public sealed class AccountOpened : Event<BankAccount>
{
    /// <summary>Creates an AccountOpened event with the opened account as payload.</summary>
    public AccountOpened(BankAccount account) : base(account) { }
}

/// <summary>Domain event raised when money is deposited into a bank account.</summary>
public sealed class MoneyDeposited : Event<BankAccount>
{
    /// <summary>Creates a MoneyDeposited event with the updated account as payload.</summary>
    public MoneyDeposited(BankAccount account) : base(account) { }
}

/// <summary>Domain event raised when money is withdrawn from a bank account.</summary>
public sealed class MoneyWithdrawn : Event<BankAccount>
{
    /// <summary>Creates a MoneyWithdrawn event with the updated account as payload.</summary>
    public MoneyWithdrawn(BankAccount account) : base(account) { }
}

/// <summary>Domain event raised when a bank account is closed.</summary>
public sealed class AccountClosed : Event<BankAccount>
{
    /// <summary>Creates an AccountClosed event with the closed account as payload.</summary>
    public AccountClosed(BankAccount account) : base(account) { }
}
