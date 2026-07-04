using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Meridian.Api.Infrastructure.Realtime;

/// <summary>
/// SignalR hub used to push live updates (new tasks, progress changes, meeting
/// changes, notifications) to connected clients. Each authenticated connection
/// joins a group named after the user id so the server can target individuals.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    public static string GroupFor(int userId) => $"user-{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        await base.OnConnectedAsync();
    }
}

/// <summary>Client event names emitted over the hub (kept in one place for consistency).</summary>
public static class RealtimeEvents
{
    public const string NotificationReceived = "notificationReceived";
    public const string TasksChanged = "tasksChanged";
    public const string ProgressChanged = "progressChanged";
}
