using Meridian.Api.Common.Domain;

namespace Meridian.Api.Features.Notifications;

/// <summary>
/// Single entry point for notifying users. Persists an in-app notification and
/// pushes it in real time. Domain-event observers depend on this so they stay
/// unaware of persistence and transport details.
/// </summary>
public interface INotifier
{
    Task NotifyAsync(int userId, NotificationType type, string title, string message, CancellationToken ct = default);

    Task NotifyManyAsync(IEnumerable<int> userIds, NotificationType type, string title, string message, CancellationToken ct = default);

    /// <summary>Signals connected clients of a user that their task list / progress changed.</summary>
    Task PushTasksChangedAsync(IEnumerable<int> userIds, int onboardingEmployeeId, int progressPercentage, CancellationToken ct = default);
}
