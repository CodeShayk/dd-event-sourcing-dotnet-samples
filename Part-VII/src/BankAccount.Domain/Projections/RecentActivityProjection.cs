// File: BankAccount.Domain/Projections/RecentActivityProjection.cs
using Microsoft.Extensions.Logging;
using SourceFlow;
using SourceFlow.Projections;
using BankAccount.Domain.Events;
using BankAccount.Domain.Views;

namespace BankAccount.Domain.Projections;

/// <summary>
/// Maintains the RecentActivityView: a sliding window of the last 10 events
/// for an account. Demonstrates the "bounded list append" projection pattern.
/// </summary>
public sealed class RecentActivityProjection :
    View<RecentActivityView>,
    IProjectOn<AccountOpened>,
    IProjectOn<MoneyDeposited>,
    IProjectOn<MoneyWithdrawn>,
    IProjectOn<AccountClosed>
{
    /// <summary>
    /// Initialises the projection with the view model store adapter and logger.
    /// </summary>
    public RecentActivityProjection(
        IViewModelStoreAdapter viewModelStore,
        ILogger<IView> logger)
        : base(viewModelStore, logger) { }

    /// <summary>
    /// Creates the RecentActivityView with the opening event as the first entry.
    /// This is a "create from scratch" handler — no prior view exists for AccountOpened.
    /// </summary>
    public Task<IViewModel> On(AccountOpened @event)
    {
        var view = new RecentActivityView
        {
            Id = @event.Payload.Id,
            Activities =
            [
                CreateEntry("AccountOpened",
                    $"Account opened for {@event.Payload.AccountHolder} with balance {@event.Payload.Balance:C}")
            ]
        };
        return Task.FromResult<IViewModel>(view);
    }

    /// <summary>
    /// Prepends a deposit entry and trims to the maximum window size.
    /// Loads the existing view (bounded-list append pattern).
    /// </summary>
    public async Task<IViewModel> On(MoneyDeposited @event)
    {
        var view  = await Find<RecentActivityView>(@event.Payload.Id);
        var entry = CreateEntry("MoneyDeposited",
            $"Deposit — new balance: {@event.Payload.Balance:C}");

        return PrependAndTrim(view, entry);
    }

    /// <summary>
    /// Prepends a withdrawal entry and trims to the maximum window size.
    /// </summary>
    public async Task<IViewModel> On(MoneyWithdrawn @event)
    {
        var view  = await Find<RecentActivityView>(@event.Payload.Id);
        var entry = CreateEntry("MoneyWithdrawn",
            $"Withdrawn — new balance: {@event.Payload.Balance:C}");

        return PrependAndTrim(view, entry);
    }

    /// <summary>
    /// Prepends an account-closed entry.
    /// </summary>
    public async Task<IViewModel> On(AccountClosed @event)
    {
        var view  = await Find<RecentActivityView>(@event.Payload.Id);
        var entry = CreateEntry("AccountClosed", "Account closed");

        return PrependAndTrim(view, entry);
    }

    // --- private helpers ---

    private static RecentActivityEntry CreateEntry(string eventType, string summary) =>
        new() { EventType = eventType, Summary = summary, OccurredAt = DateTime.UtcNow };

    private static RecentActivityView PrependAndTrim(
        RecentActivityView view,
        RecentActivityEntry entry)
    {
        view.Activities.Insert(0, entry); // newest first

        // Drop oldest entries beyond the window size
        while (view.Activities.Count > RecentActivityView.MaxEntries)
            view.Activities.RemoveAt(view.Activities.Count - 1);

        return view;
    }
}
