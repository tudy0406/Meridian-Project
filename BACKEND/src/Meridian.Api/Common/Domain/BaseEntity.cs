namespace Meridian.Api.Common.Domain;

/// <summary>
/// Base class for all persisted entities. Carries the surrogate key and a
/// collection of domain events that are dispatched after the entity is
/// successfully saved (see <see cref="Events.IDomainEventDispatcher"/>).
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }

    private readonly List<Events.IDomainEvent> _domainEvents = new();

    /// <summary>Domain events raised by this entity, pending dispatch.</summary>
    public IReadOnlyCollection<Events.IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void Raise(Events.IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
