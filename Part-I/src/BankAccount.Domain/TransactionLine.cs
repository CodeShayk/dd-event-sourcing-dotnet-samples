// File: BankAccount.Domain/TransactionLine.cs
#nullable enable

namespace BankAccount.Domain;

/// <summary>
/// The type of a transaction line: whether money moved into or out of the account.
/// </summary>
public enum TransactionType
{
    /// <summary>Money credited to the account (a deposit, incoming transfer).</summary>
    Credit,

    /// <summary>Money debited from the account (a withdrawal, outgoing transfer).</summary>
    Debit
}

/// <summary>
/// A single transaction line within a bank account's history.
/// TransactionLine is an aggregate member — it exists only within the context
/// of a <see cref="BankAccount"/> and cannot be retrieved or modified independently.
/// Its constructor and factory methods are internal to prevent creation from outside
/// the aggregate.
/// </summary>
public sealed class TransactionLine
{
    /// <summary>The monetary amount of this transaction.</summary>
    public Money Amount { get; private set; } = default!;

    /// <summary>Whether this transaction added or removed money from the account.</summary>
    public TransactionType Type { get; private set; }

    /// <summary>The UTC timestamp at which this transaction occurred.</summary>
    public DateTime OccurredOn { get; private set; }

    /// <summary>
    /// An optional reference note, e.g. "Salary payment", "Transfer from savings".
    /// May be null if no reference was provided.
    /// </summary>
    public string? Reference { get; private set; }

    // Private constructor — TransactionLine can only be created through the factory methods below.
    // This prevents application code from constructing arbitrary transaction lines
    // and inserting them into an account's history.
    private TransactionLine() { }

    /// <summary>
    /// Creates a credit transaction line (money coming in to the account).
    /// Internal to the domain assembly — only BankAccount can create transaction lines.
    /// </summary>
    internal static TransactionLine ForCredit(Money amount, DateTime occurredOn, string? reference = null)
        => new()
        {
            Amount = amount,
            Type = TransactionType.Credit,
            OccurredOn = occurredOn,
            Reference = reference
        };

    /// <summary>
    /// Creates a debit transaction line (money going out of the account).
    /// Internal to the domain assembly — only BankAccount can create transaction lines.
    /// </summary>
    internal static TransactionLine ForDebit(Money amount, DateTime occurredOn, string? reference = null)
        => new()
        {
            Amount = amount,
            Type = TransactionType.Debit,
            OccurredOn = occurredOn,
            Reference = reference
        };
}
