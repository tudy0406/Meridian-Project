using Meridian.Api.Common.Exceptions;
using Meridian.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Features.Notifications;

public sealed record NotificationDto(
    int Id, string Title, string Message, string Type, bool IsRead, DateTime CreatedAt);

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> ListAsync(int userId, bool unreadOnly, CancellationToken ct = default);
    Task<int> UnreadCountAsync(int userId, CancellationToken ct = default);
    Task MarkAsReadAsync(int userId, int notificationId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(int userId, CancellationToken ct = default);
}

public sealed class NotificationService : INotificationService
{
    private readonly MeridianDbContext _db;
    public NotificationService(MeridianDbContext db) => _db = db;

    public async Task<IReadOnlyList<NotificationDto>> ListAsync(int userId, bool unreadOnly, CancellationToken ct = default)
    {
        var query = _db.Notifications.Where(n => n.UserId == userId);
        if (unreadOnly) query = query.Where(n => !n.IsRead);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.Type.ToString(), n.IsRead, n.CreatedAt))
            .ToListAsync(ct);
    }

    public Task<int> UnreadCountAsync(int userId, CancellationToken ct = default) =>
        _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task MarkAsReadAsync(int userId, int notificationId, CancellationToken ct = default)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct)
            ?? throw NotFoundException.For("Notification", notificationId);
        notification.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkAllAsReadAsync(int userId, CancellationToken ct = default)
    {
        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }
}
