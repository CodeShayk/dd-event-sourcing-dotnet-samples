// Chapter 21 — BankAccount entity
using SourceFlow;
using BankAccount.Domain.Exceptions;

namespace BankAccount.Domain;

/// <summary>
/// The BankAccount entity. Enforces domain invariants at Layer 3 of the
/// three-layer validation model. Business rules are the saga's responsibility;
/// this class only enforces mathematical invariants about its own state.
/// </summary>
public sealed class BankAccount : IEntity
{
    /// <summary>Database-assigned unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>The account number string (e.g., "ACC-001").</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>The full name of the account holder.</summary>
    public string AccountHolder { get; set; } = string.Empty;

    /// <summary>The current balance. Cannot be negative — enforced by Deposit/Withdraw.</summary>
    public decimal Balance { get; set; }

    /// <summary>True if the account is open; false if it has been closed.</summary>
    public bool IsActive { get; set; }

    /// <summary>UTC date and time when the account was opened.</summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Adds the specified amount to the account balance.
    /// </summary>
    public void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount),
                "Deposit amount must be positive.");

        Balance += amount;
    }

    /// <summary>
    /// Subtracts the specified amount from the account balance.
    /// Invariant: balance cannot go negative (no overdraft permitted).
    /// </summary>
    public void Withdraw(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount),
                "Withdrawal amount must be positive.");

        if (Balance < amount)
            throw new InsufficientFundsException(Id, amount, Balance);

        Balance -= amount;
    }
}
