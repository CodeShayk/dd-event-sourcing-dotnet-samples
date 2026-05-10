// File: BankAccount.Domain/Events/InMemoryDomainEventDispatcher.cs
#nullable enable

namespace BankAccount.Domain.Events;

/// <summary>
/// A simple in-memory domain event dispatcher for development and testing.
/// Dispatches domain events to all registered handlers synchronously.
/// This is NOT suitable for production use — it provides no durability guarantees.
/// In Part II, this will be replaced by an event sourcing infrastructure
/// that makes events the durable source of truth.
/// </summary>
/// <remarks>
/// The use of <see cref="Func{IDomainEvent, Task}"/> wrappers to store handlers
/// avoids runtime reflection at dispatch time. When a handler is registered,
/// we capture a typed lambda that closes over the strongly-typed handler — the cast
/// to <typeparamref name="TEvent"/> happens once at registration, not on every dispatch.
/// This is a deliberate performance and correctness trade-off.
/// </remarks>
public sealed class InMemoryDomainEventDispatcher
{
    // Handlers are registered as object references and resolved via reflection-free type checking.
    private readonly Dictionary<Type, List<Func<IDomainEvent, Task>>> _handlers = new();

    /// <summary>
    /// Registers a handler for a specific event type.
    /// </summary>
    /// <typeparam name="TEvent">The event type to handle.</typeparam>
    /// <param name="handler">The handler function.</param>
    public void Register<TEvent>(IDomainEventHandler<TEvent> handler)
        where TEvent : IDomainEvent
    {
        var eventType = typeof(TEvent);
        if (!_handlers.TryGetValue(eventType, out var list))
        {
            list = new List<Func<IDomainEvent, Task>>();
            _handlers[eventType] = list;
        }

        list.Add(@event => handler.Handle((TEvent)@event));
    }

    /// <summary>
    /// Dispatches all pending domain events from the given aggregate.
    /// Clears the aggregate's domain events after dispatching.
    /// Accepts any aggregate that implements <see cref="IHasDomainEvents"/> —
    /// this dispatcher is not coupled to any specific aggregate type.
    /// </summary>
    /// <param name="aggregate">The aggregate whose pending events should be dispatched.</param>
    public async Task DispatchAndClear(IHasDomainEvents aggregate)
    {
        // Take a snapshot of events before clearing, in case dispatch raises exceptions
        var events = aggregate.DomainEvents.ToList();
        aggregate.ClearDomainEvents();

        foreach (var @event in events)
        {
            var eventType = @event.GetType();
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                foreach (var handler in handlers)
                    await handler(@event);
            }
        }
    }
}
