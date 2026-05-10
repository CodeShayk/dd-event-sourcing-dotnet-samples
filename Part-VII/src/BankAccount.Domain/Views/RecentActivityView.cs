// File: BankAccount.Domain/Views/RecentActivityView.cs
using SourceFlow.Projections;

namespace BankAccount.Domain.Views;

/// <summary>
/// A sliding window of recent activity for an account. Maintains the
/// last ten transactions. Useful for "recent activity" widgets on dashboards.
/// This view demonstrates the "bounded list" pattern: older entries are
/// dropped as new ones arrive.
/// </summary>
public sealed class RecentActivityView : IViewModel
{
    /// <summary>The account's unique identifier.</summary>
    public int Id { get; set; }

    /// <summary>The last N activities, ordered from newest to oldest.</summary>
    public List<RecentActivityEntry> Activities { get; set; } = new();

    /// <summary>Maximum number of entries to retain.</summary>
    public const int MaxEntries = 10;
}

/// <summary>
/// A single entry in the recent activity view.
/// </summary>
public sealed class RecentActivityEntry
{
    /// <summary>The type of event that caused this activity.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>A brief human-readable summary of the activity.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>UTC timestamp of the activity.</summary>
    public DateTime OccurredAt { get; set; }
}
