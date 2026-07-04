namespace Meridian.Api.Common.Domain.Events;

/// <summary>
/// An observer that reacts to a specific kind of <see cref="IDomainEvent"/>.
/// Multiple handlers may subscribe to the same event; each is resolved from DI
/// and invoked by the <see cref="IDomainEventDispatcher"/>.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
