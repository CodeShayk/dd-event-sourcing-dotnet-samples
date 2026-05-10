// File: BankAccount.Domain/Events/AccountEvents.cs
#nullable enable

namespace BankAccount.Domain.Events;

/// <summary>
/// Raised when a new bank account is opened.
/// Carries all the information that existed at the moment of opening:
/// who the account holder is and what account number was assigned.
/// </summary>
/// <param name="AggregateId">The account's identifier (0 for a newly created account not yet persisted).</param>
/// <param name="AccountHolder">The full name of the account holder.</param>
/// <param name="AccountNumber">The assigned account number.</param>
/// <param name="OccurredOn">When the account was opened.</param>
public record AccountOpened(
    int AggregateId,
    string AccountHolder,
    string AccountNumber,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when a credit (deposit) is applied to an account.
/// Carries both the credited amount and the resulting balance —
/// the resulting balance allows subscribers to update read models without
/// needing to calculate it themselves.
/// Both <paramref name="Amount"/> and <paramref name="NewBalance"/> are <see cref="Money"/>
/// to preserve the currency information established by the Ubiquitous Language.
/// </summary>
/// <param name="AggregateId">The account's identifier.</param>
/// <param name="Amount">The amount credited, including currency.</param>
/// <param name="NewBalance">The account balance after the credit, including currency.</param>
/// <param name="Reference">The optional transaction reference.</param>
/// <param name="OccurredOn">When the credit occurred.</param>
public record AccountCredited(
    int AggregateId,
    Money Amount,
    Money NewBalance,
    string? Reference,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when a debit (withdrawal) is applied to an account.
/// Carries both the debited amount and the resulting balance.
/// Both <paramref name="Amount"/> and <paramref name="NewBalance"/> are <see cref="Money"/>
/// to preserve currency information.
/// </summary>
/// <param name="AggregateId">The account's identifier.</param>
/// <param name="Amount">The amount debited, including currency.</param>
/// <param name="NewBalance">The account balance after the debit, including currency.</param>
/// <param name="Reference">The optional transaction reference.</param>
/// <param name="OccurredOn">When the debit occurred.</param>
public record AccountDebited(
    int AggregateId,
    Money Amount,
    Money NewBalance,
    string? Reference,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when an account is closed.
/// No amount information — closing is a status change, not a monetary transaction.
/// </summary>
/// <param name="AggregateId">The account's identifier.</param>
/// <param name="OccurredOn">When the account was closed.</param>
public record AccountClosed(
    int AggregateId,
    DateTime OccurredOn) : IDomainEvent;
