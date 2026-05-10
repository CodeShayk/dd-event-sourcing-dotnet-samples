// File: BankAccount.Domain/Events/IHasDomainEvents.cs
#nullable enable

namespace BankAccount.Domain.Events;

/// <summary>
/// Implemented by aggregate roots that collect domain events for deferred dispatch.
/// The dispatcher works against this interface, not against a specific aggregate type,
/// so the same dispatcher can service any aggregate in the system.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// The domain events raised by this aggregate since the last time they were cleared.
    /// </summary>
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all pending domain events.
    /// Called by the application layer after events have been dispatched.
    /// </summary>
    void ClearDomainEvents();
}
