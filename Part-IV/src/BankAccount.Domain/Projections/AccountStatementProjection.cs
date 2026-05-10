// File: BankAccount.Domain/Projections/AccountStatementProjection.cs
using Microsoft.Extensions.Logging;
using SourceFlow;
using SourceFlow.Projections;
using BankAccount.Domain.Events;
using BankAccount.Domain.Views;

namespace BankAccount.Domain.Projections;

/// <summary>
/// Projects all four BankAccount events into the AccountStatementView read model.
/// The statement view is append-only: AccountOpened creates the initial entry,
/// and each subsequent event appends a new transaction entry.
/// </summary>
public sealed class AccountStatementProjection :
    View<AccountStatementView>,
    IProjectOn<AccountOpened>,
    IProjectOn<MoneyDeposited>,
    IProjectOn<MoneyWithdrawn>,
    IProjectOn<AccountClosed>
{
    /// <summary>
    /// Initialises the projection with the view model store adapter and logger.
    /// </summary>
    public AccountStatementProjection(
        IViewModelStoreAdapter viewModelStore,
        ILogger<IView> logger)
        : base(viewModelStore, logger) { }

    /// <summary>
    /// Creates the AccountStatementView with an initial "OpeningBalance" entry
    /// when the account is opened. This is the only handler that creates the view
    /// from scratch; all subsequent handlers load and append to it.
    /// </summary>
    public Task<IViewModel> On(AccountOpened @event)
    {
        var account = @event.Payload;

        var view = new AccountStatementView
        {
            Id             = account.Id,
            AccountNumber  = account.AccountNumber,
            CurrentBalance = account.Balance,
            Entries        =
            [
                new StatementEntry
                {
                    Timestamp       = DateTime.UtcNow,
                    Description     = "Account opened",
                    Amount          = account.Balance,
                    RunningBalance  = account.Balance,
                    TransactionType = "OpeningBalance"
                }
            ]
        };

        return Task.FromResult<IViewModel>(view);
    }

    /// <summary>
    /// Appends a "Deposit" entry to the statement and updates the current balance.
    /// </summary>
    public async Task<IViewModel> On(MoneyDeposited @event)
    {
        var account = @event.Payload;
        var view    = await Find<AccountStatementView>(account.Id);

        // The amount deposited = new balance minus old balance (balance in payload
        // is the POST-deposit balance; we calculate the delta).
        var previousBalance = view.CurrentBalance;
        var depositAmount   = account.Balance - previousBalance;

        view.CurrentBalance = account.Balance;
        view.Entries.Add(new StatementEntry
        {
            Timestamp       = DateTime.UtcNow,
            Description     = "Money deposited",
            Amount          = depositAmount,
            RunningBalance  = account.Balance,
            TransactionType = "Deposit"
        });

        return view;
    }

    /// <summary>
    /// Appends a "Withdrawal" entry to the statement and updates the current balance.
    /// </summary>
    public async Task<IViewModel> On(MoneyWithdrawn @event)
    {
        var account = @event.Payload;
        var view    = await Find<AccountStatementView>(account.Id);

        var previousBalance  = view.CurrentBalance;
        var withdrawalAmount = previousBalance - account.Balance; // positive delta

        view.CurrentBalance = account.Balance;
        view.Entries.Add(new StatementEntry
        {
            Timestamp       = DateTime.UtcNow,
            Description     = "Money withdrawn",
            Amount          = -withdrawalAmount, // negative: money left the account
            RunningBalance  = account.Balance,
            TransactionType = "Withdrawal"
        });

        return view;
    }

    /// <summary>
    /// Appends an "AccountClosure" entry to mark the end of the ledger.
    /// Does not change the balance. TransactionType uses the past-tense event-centric
    /// naming convention consistent with "OpeningBalance", "Deposit", and "Withdrawal".
    /// </summary>
    public async Task<IViewModel> On(AccountClosed @event)
    {
        var account = @event.Payload;
        var view    = await Find<AccountStatementView>(account.Id);

        view.Entries.Add(new StatementEntry
        {
            Timestamp       = DateTime.UtcNow,
            Description     = "Account closed",
            Amount          = 0m,
            RunningBalance  = view.CurrentBalance,
            TransactionType = "AccountClosure"
        });

        return view;
    }
}
