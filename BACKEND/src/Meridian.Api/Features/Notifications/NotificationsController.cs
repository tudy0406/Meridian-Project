using Meridian.Api.Common.Web;
using Meridian.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meridian.Api.Features.Notifications;

[Authorize]
public sealed class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _notifications;
    private readonly ICurrentUser _currentUser;

    public NotificationsController(INotificationService notifications, ICurrentUser currentUser)
    {
        _notifications = notifications;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> List([FromQuery] bool unreadOnly, CancellationToken ct) =>
        Ok(await _notifications.ListAsync(_currentUser.RequireUserId(), unreadOnly, ct));

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> UnreadCount(CancellationToken ct) =>
        Ok(await _notifications.UnreadCountAsync(_currentUser.RequireUserId(), ct));

    [HttpPatch("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        await _notifications.MarkAsReadAsync(_currentUser.RequireUserId(), id, ct);
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await _notifications.MarkAllAsReadAsync(_currentUser.RequireUserId(), ct);
        return NoContent();
    }
}

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsFeature(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<INotifier, Notifier>();
        return services;
    }
}
