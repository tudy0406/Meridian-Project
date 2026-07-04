namespace Meridian.Api.Common.Domain.Events;

/// <summary>
/// Marker for something that has happened in the domain and that other parts of
/// the system may wish to react to (the "subject" in the Observer pattern).
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAt => DateTime.UtcNow;
}
