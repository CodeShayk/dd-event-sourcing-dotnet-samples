// File: BankAccount.Domain/NotificationsContext/NotificationTarget.cs
#nullable enable

namespace BankAccount.Domain.NotificationsContext;

/// <summary>
/// The Notifications context's model of a contact.
/// This is a completely separate model from <see cref="AccountsContext.AccountHolder"/>.
/// It contains only what the Notifications context needs to send a message.
/// The fact that both relate to the same real-world person is irrelevant to the type system.
/// </summary>
/// <param name="RecipientName">The display name for the notification salutation.</param>
/// <param name="EmailAddress">The email address to deliver notifications to.</param>
/// <param name="AccountReference">A short identifier shown in the notification body.</param>
public record NotificationTarget(
    string RecipientName,
    string EmailAddress,
    string AccountReference);
