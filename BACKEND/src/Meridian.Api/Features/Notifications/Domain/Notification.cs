using Meridian.Api.Common.Domain;
using Meridian.Api.Features.Users.Domain;

namespace Meridian.Api.Features.Notifications.Domain;

/// <summary>An in-app notification targeted at a single user.</summary>
public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
