// File: BankAccount.Domain/Projections/AccountSummaryProjection.cs
using Microsoft.Extensions.Logging;
using SourceFlow;
using SourceFlow.Projections;
using BankAccount.Domain.Events;
using BankAccount.Domain.Views;

namespace BankAccount.Domain.Projections;

/// <summary>
/// Projects all four BankAccount events into the AccountSummaryView read model.
/// The AccountSummaryView is the primary query-facing view: it provides the
/// current balance, account status, and holder name in a single flat record.
/// </summary>
public sealed class AccountSummaryProjection :
    View<AccountSummaryView>,
    IProjectOn<AccountOpened>,
    IProjectOn<MoneyDeposited>,
    IProjectOn<MoneyWithdrawn>,
    IProjectOn<AccountClosed>
{
    /// <summary>
    /// Initialises the projection with the view model store adapter (for loading
    /// and persisting view models) and a logger.
    /// </summary>
    public AccountSummaryProjection(
        IViewModelStoreAdapter viewModelStore,
        ILogger<IView> logger)
        : base(viewModelStore, logger) { }

    /// <summary>
    /// Creates the initial AccountSummaryView when an account is opened.
    /// Because this is the first event for this account, there is no existing
    /// view to load — we construct one from scratch.
    /// </summary>
    public Task<IViewModel> On(AccountOpened @event)
    {
        // For AccountOpened, no prior view exists. Build it fresh.
        var view = new AccountSummaryView
        {
            Id            = @event.Payload.Id,
            AccountNumber = @event.Payload.AccountNumber,
            AccountHolder = @event.Payload.AccountHolder,
            Balance       = @event.Payload.Balance,
            IsActive      = true,
            LastUpdated   = DateTime.UtcNow
        };

        // Return the new view model. The View<T> base class persists it.
        // Do NOT call Persist() here — that would cause a double-write.
        return Task.FromResult<IViewModel>(view);
    }

    /// <summary>
    /// Updates the balance in AccountSummaryView after a deposit.
    /// Loads the existing view, applies the change, and returns it.
    /// The framework persists the returned value.
    /// </summary>
    public async Task<IViewModel> On(MoneyDeposited @event)
    {
        // Load the current view using the protected Find<T> helper.
        // Find<T> calls IViewModelStoreAdapter.Find<T>(id) internally.
        var view = await Find<AccountSummaryView>(@event.Payload.Id);

        view.Balance     = @event.Payload.Balance;
        view.LastUpdated = DateTime.UtcNow;

        // Return the updated view. Framework calls IViewModelStoreAdapter.Persist(view).
        return view;
    }

    /// <summary>
    /// Updates the balance in AccountSummaryView after a withdrawal.
    /// </summary>
    public async Task<IViewModel> On(MoneyWithdrawn @event)
    {
        var view = await Find<AccountSummaryView>(@event.Payload.Id);

        view.Balance     = @event.Payload.Balance;
        view.LastUpdated = DateTime.UtcNow;

        return view;
    }

    /// <summary>
    /// Marks the AccountSummaryView as inactive when the account is closed.
    /// </summary>
    public async Task<IViewModel> On(AccountClosed @event)
    {
        var view = await Find<AccountSummaryView>(@event.Payload.Id);

        view.IsActive    = false;
        view.LastUpdated = DateTime.UtcNow;

        return view;
    }
}
