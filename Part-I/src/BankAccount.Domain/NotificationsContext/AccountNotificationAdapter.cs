// File: BankAccount.Domain/NotificationsContext/AccountNotificationAdapter.cs
#nullable enable

using BankAccount.Domain.AccountsContext;

namespace BankAccount.Domain.NotificationsContext;

/// <summary>
/// Translates Accounts context models into Notifications context models.
/// This is the anti-corruption layer between the two contexts.
/// The Notifications context defines this adapter — it is the consumer's responsibility
/// to translate the upstream model into terms it understands.
/// Notice that the Accounts context types appear only in the method signatures here,
/// not in the Notifications context's domain model itself.
/// </summary>
public sealed class AccountNotificationAdapter
{
    /// <summary>
    /// Translates an <see cref="AccountHolder"/> from the Accounts context
    /// into a <see cref="NotificationTarget"/> for the Notifications context.
    /// In a real system, the email address would come from a Customer profile service.
    /// We simulate it here with a deterministic placeholder.
    /// </summary>
    /// <param name="holder">The account holder from the Accounts context.</param>
    /// <returns>A notification target suitable for the Notifications context.</returns>
    public NotificationTarget ToNotificationTarget(AccountHolder holder)
    {
        // The Notifications context does not use "AccountNumber" — it uses "AccountReference",
        // which is a shortened display version. This translation is deliberate: the downstream
        // context defines its terms, not the upstream.
        var accountReference = holder.AccountNumber.Length > 8
            ? $"***{holder.AccountNumber[^4..]}"
            : holder.AccountNumber;

        return new NotificationTarget(
            RecipientName: holder.FullName,
            EmailAddress: $"{holder.FullName.Replace(" ", ".").ToLower()}@example.com",
            AccountReference: accountReference);
    }
}
