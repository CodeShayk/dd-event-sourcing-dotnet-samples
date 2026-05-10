// File: BankAccount.Domain/Views/AccountSummaryView.cs
using SourceFlow.Projections;

namespace BankAccount.Domain.Views;

/// <summary>
/// The primary query view for a bank account. Contains the current state
/// of the account: balance, holder, status, and last update timestamp.
/// Optimised for dashboard-style queries — flat, no navigation properties.
/// </summary>
public sealed class AccountSummaryView : IViewModel
{
    /// <summary>The account's unique identifier. Matches the write-model entity ID.</summary>
    public int Id { get; set; }

    /// <summary>The account number string (e.g., "ACC-001").</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>The full name of the account holder.</summary>
    public string AccountHolder { get; set; } = string.Empty;

    /// <summary>The current balance, updated after every deposit or withdrawal.</summary>
    public decimal Balance { get; set; }

    /// <summary>True if the account is open and active; false if it has been closed.</summary>
    public bool IsActive { get; set; }

    /// <summary>UTC timestamp of the last event that updated this view.</summary>
    public DateTime LastUpdated { get; set; }
}
