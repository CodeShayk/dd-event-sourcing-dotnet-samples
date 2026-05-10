// File: BankAccount.Domain/NotificationsContext/AccountCreditedNotification.cs
#nullable enable

namespace BankAccount.Domain.NotificationsContext;

/// <summary>
/// A notification message for the Notifications context.
/// Created when the Accounts context informs the Notifications context
/// that an account has been credited.
/// This is the Notifications context's own model of that event —
/// it is NOT the Accounts context's domain event. The two are deliberately separate.
/// </summary>
/// <param name="Target">The notification recipient.</param>
/// <param name="CreditedAmount">The amount credited, formatted as a string for display.</param>
/// <param name="NewBalance">The new balance, formatted as a string for display.</param>
/// <param name="OccurredOn">When the credit occurred.</param>
public record AccountCreditedNotification(
    NotificationTarget Target,
    string CreditedAmount,
    string NewBalance,
    DateTime OccurredOn);

/// <summary>
/// A simple notification sender interface, representing the Notifications context's
/// infrastructure port. The implementation (email, SMS, push notification) is
/// an infrastructure detail, not a domain detail.
/// </summary>
public interface INotificationSender
{
    /// <summary>Sends a credit notification to the account holder.</summary>
    Task SendCreditNotification(AccountCreditedNotification notification);
}
