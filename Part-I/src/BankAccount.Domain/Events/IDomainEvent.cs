// File: BankAccount.Domain/Events/IDomainEvent.cs
#nullable enable

namespace BankAccount.Domain.Events;

/// <summary>
/// Marker interface for all domain events in the Bank Account system.
/// Domain events are immutable records of things that have happened in the domain.
/// They are named in the past tense (AccountOpened, not OpenAccount).
/// They carry sufficient information to understand what changed and when.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// The UTC timestamp at which this event occurred.
    /// This is the business time — when the domain operation happened,
    /// not necessarily when it was recorded.
    /// </summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// The identifier of the aggregate that raised this event.
    /// Used to correlate events with the entity they describe.
    /// </summary>
    int AggregateId { get; }
}
