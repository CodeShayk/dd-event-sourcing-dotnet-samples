// File: BankAccount.Domain/Events/IDomainEventHandler.cs
#nullable enable

namespace BankAccount.Domain.Events;

/// <summary>
/// Handles a specific type of domain event.
/// Implementations subscribe to a particular event type and react to it —
/// sending notifications, updating read models, writing audit records.
/// </summary>
/// <typeparam name="TEvent">The domain event type this handler processes.</typeparam>
public interface IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    /// <summary>Processes the given domain event.</summary>
    Task Handle(TEvent @event);
}
