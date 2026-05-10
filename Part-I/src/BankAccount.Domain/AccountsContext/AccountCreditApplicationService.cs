// File: BankAccount.Domain/AccountsContext/AccountCreditApplicationService.cs
#nullable enable

using BankAccount.Domain;
using BankAccount.Domain.NotificationsContext;

namespace BankAccount.Domain.AccountsContext;

/// <summary>
/// An application service that orchestrates the credit operation and the resulting
/// notification. This is the only place where both contexts meet — and they meet
/// at the boundary, not inside either context's domain model.
/// </summary>
public sealed class AccountCreditApplicationService
{
    private readonly BankAccountService _accountService;
    private readonly AccountNotificationAdapter _adapter;
    private readonly INotificationSender _notifications;

    /// <summary>Initialises the application service with its dependencies.</summary>
    public AccountCreditApplicationService(
        BankAccountService accountService,
        AccountNotificationAdapter adapter,
        INotificationSender notifications)
    {
        _accountService = accountService;
        _adapter = adapter;
        _notifications = notifications;
    }

    /// <summary>
    /// Credits an account and dispatches a notification to the account holder.
    /// The credit operation is a pure Accounts concern.
    /// The notification is translated at the boundary before dispatch.
    /// </summary>
    /// <param name="accountId">The account to credit.</param>
    /// <param name="amount">The amount to credit.</param>
    /// <param name="holder">The account holder, used to construct the notification target.</param>
    public async Task CreditAndNotify(int accountId, Money amount, AccountHolder holder)
    {
        await _accountService.CreditAccount(accountId, amount);

        var target = _adapter.ToNotificationTarget(holder);
        var notification = new AccountCreditedNotification(
            Target: target,
            CreditedAmount: amount.ToString(),
            // Note: CreditAndNotify cannot easily provide the updated balance without
            // re-querying the repository or changing CreditAccount's return type.
            // This is a limitation of the current design that the domain event pattern
            // in Chapter 6 resolves: the AccountCredited event carries NewBalance directly
            // from the aggregate, eliminating the need for a second query.
            NewBalance: "See account statement",
            OccurredOn: DateTime.UtcNow);

        await _notifications.SendCreditNotification(notification);
    }
}
