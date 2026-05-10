// File: BankAccount.Domain/Views/AccountStatementView.cs
using SourceFlow.Projections;

namespace BankAccount.Domain.Views;

/// <summary>
/// The statement view for a bank account. Contains a chronological list of
/// all transactions, each with a running balance. This view is append-only
/// in character: new entries are added by each deposit/withdrawal event,
/// never overwritten. Optimised for audit and ledger-style queries.
///
/// Note: The Entries list is serialised to a JSON column by EfViewModelStore
/// when using the Entity Framework Core persistence backend. If you run this
/// view model against a real database before configuring Part V, you will need
/// a value converter or JSON column mapping for this property. See Chapter 23
/// for the full EF Core configuration.
/// </summary>
public sealed class AccountStatementView : IViewModel
{
    /// <summary>The account's unique identifier. Matches the write-model entity ID.</summary>
    public int Id { get; set; }

    /// <summary>The account number string.</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>The current balance (kept in sync with AccountSummaryView).</summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>
    /// The ordered list of transaction entries. The first entry is always
    /// an "OpeningBalance" entry created when the account was opened.
    /// Subsequent entries are appended in chronological order.
    ///
    /// When persisted via EfViewModelStore, this list is stored as a JSON
    /// column in a single database row. Chapter 23 covers the configuration
    /// required for this serialisation.
    /// </summary>
    public List<StatementEntry> Entries { get; set; } = new();
}

/// <summary>
/// A single transaction entry in the statement. Represents one event that
/// changed the account balance (opening, deposit, or withdrawal).
/// </summary>
public sealed class StatementEntry
{
    /// <summary>UTC timestamp when the transaction occurred.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Human-readable description (e.g., "Salary deposit").</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The transaction amount. Positive for deposits and opening balances,
    /// negative for withdrawals.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>The account balance immediately after this transaction.</summary>
    public decimal RunningBalance { get; set; }

    /// <summary>
    /// The category of the transaction: "OpeningBalance", "Deposit", "Withdrawal",
    /// or "AccountClosure".
    /// </summary>
    public string TransactionType { get; set; } = string.Empty;
}
