// BankAccount.Domain/Commands.cs
#nullable enable

namespace BankAccount.Domain;

/// <summary>
/// Command instructing the system to open a new bank account.
/// </summary>
public sealed record OpenAccountCommand(int AccountId, string OwnerName, decimal InitialDeposit);

/// <summary>
/// Command instructing the system to deposit money into an account.
/// </summary>
public sealed record DepositMoneyCommand(int AccountId, decimal Amount);

/// <summary>
/// Command instructing the system to withdraw money from an account.
/// </summary>
public sealed record WithdrawMoneyCommand(int AccountId, decimal Amount);

/// <summary>
/// Command instructing the system to close a bank account.
/// </summary>
public sealed record CloseAccountCommand(int AccountId, string Reason);
