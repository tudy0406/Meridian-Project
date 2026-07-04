using Meridian.Api.Common.Domain;
using Meridian.Api.Features.Notifications.Domain;
using Meridian.Api.Infrastructure.Persistence;
using Meridian.Api.Infrastructure.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Meridian.Api.Features.Notifications;

public sealed class Notifier : INotifier
{
    private readonly MeridianDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;

    public Notifier(MeridianDbContext db, IHubContext<NotificationHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task NotifyAsync(int userId, NotificationType type, string title, string message, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        await _hub.Clients.Group(NotificationHub.GroupFor(userId))
            .SendAsync(RealtimeEvents.NotificationReceived, new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                Type = type.ToString(),
                notification.CreatedAt
            }, ct);
    }

    public async Task NotifyManyAsync(IEnumerable<int> userIds, NotificationType type, string title, string message, CancellationToken ct = default)
    {
        foreach (var userId in userIds.Distinct())
            await NotifyAsync(userId, type, title, message, ct);
    }

    public async Task PushTasksChangedAsync(IEnumerable<int> userIds, int onboardingEmployeeId, int progressPercentage, CancellationToken ct = default)
    {
        var payload = new { onboardingEmployeeId, progressPercentage };
        foreach (var userId in userIds.Distinct())
        {
            await _hub.Clients.Group(NotificationHub.GroupFor(userId))
                .SendAsync(RealtimeEvents.TasksChanged, payload, ct);
            await _hub.Clients.Group(NotificationHub.GroupFor(userId))
                .SendAsync(RealtimeEvents.ProgressChanged, payload, ct);
        }
    }
}
