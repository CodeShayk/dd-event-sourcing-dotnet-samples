// BankAccount.Domain/Events.cs
#nullable enable

namespace BankAccount.Domain;

/// <summary>Raised when a bank account is opened.</summary>
public sealed record AccountOpenedEvent(int AccountId, string OwnerName);

/// <summary>Raised when money is deposited into an account.</summary>
public sealed record MoneyDepositedEvent(int AccountId, decimal Amount, decimal NewBalance);

/// <summary>Raised when money is withdrawn from an account.</summary>
public sealed record MoneyWithdrawnEvent(int AccountId, decimal Amount, decimal NewBalance);

/// <summary>Raised when a bank account is closed.</summary>
public sealed record AccountClosedEvent(int AccountId, string Reason);
