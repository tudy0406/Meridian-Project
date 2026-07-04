namespace Meridian.Api.Common.Domain.Events;

/// <summary>
/// Publishes domain events to every registered <see cref="IDomainEventHandler{TEvent}"/>.
/// This is the "notify observers" step of the Observer pattern and keeps
/// publishers decoupled from subscribers.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
