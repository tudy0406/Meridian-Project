using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Domain.Events;
using Meridian.Api.Features.Notifications;

namespace Meridian.Api.Features.Meetings.Events;

/// <summary>Raised when a meeting is scheduled or its details change.</summary>
public sealed record MeetingChangedEvent(
    int MeetingId,
    string Title,
    DateTime DateTime,
    IReadOnlyCollection<int> ParticipantIds,
    bool IsUpdate) : IDomainEvent;

/// <summary>Observer that notifies participants when a meeting is scheduled or updated.</summary>
public sealed class MeetingChangedNotificationHandler : IDomainEventHandler<MeetingChangedEvent>
{
    private readonly INotifier _notifier;

    public MeetingChangedNotificationHandler(INotifier notifier) => _notifier = notifier;

    public Task HandleAsync(MeetingChangedEvent e, CancellationToken ct = default)
    {
        var (type, title) = e.IsUpdate
            ? (NotificationType.MeetingUpdated, "Meeting updated")
            : (NotificationType.MeetingScheduled, "Meeting scheduled");

        var message = $"\"{e.Title}\" is scheduled for {e.DateTime:g} (UTC).";
        return _notifier.NotifyManyAsync(e.ParticipantIds, type, title, message, ct);
    }
}
