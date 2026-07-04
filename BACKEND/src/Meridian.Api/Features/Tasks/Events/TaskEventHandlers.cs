using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Domain.Events;
using Meridian.Api.Features.Notifications;
using Meridian.Api.Features.OnboardingProcess;

namespace Meridian.Api.Features.Tasks.Events;

/// <summary>
/// Observer that reacts to a task being assigned: notifies the new employee and
/// everyone who supervises their onboarding, and pushes a live task-list refresh.
/// </summary>
public sealed class TaskAssignedNotificationHandler : IDomainEventHandler<TaskAssignedEvent>
{
    private readonly INotifier _notifier;
    private readonly IOnboardingAudienceResolver _audience;

    public TaskAssignedNotificationHandler(INotifier notifier, IOnboardingAudienceResolver audience)
    {
        _notifier = notifier;
        _audience = audience;
    }

    public async Task HandleAsync(TaskAssignedEvent e, CancellationToken ct = default)
    {
        var audience = await _audience.ResolveAsync(e.OnboardingEmployeeId, ct);

        // The employee gets a direct "new task" notification.
        await _notifier.NotifyAsync(e.OnboardingEmployeeId, NotificationType.TaskAssigned,
            "New onboarding task", $"A new task was assigned to you: \"{e.Title}\".", ct);

        // Everyone in the audience gets a live task-list refresh.
        await _notifier.PushTasksChangedAsync(audience, e.OnboardingEmployeeId, progressPercentage: -1, ct);
    }
}

/// <summary>
/// Observer that reacts to a task completion: refreshes the progress bar for the
/// whole audience and notifies supervisors of the milestone.
/// </summary>
public sealed class TaskCompletedNotificationHandler : IDomainEventHandler<TaskCompletedEvent>
{
    private readonly INotifier _notifier;
    private readonly IOnboardingAudienceResolver _audience;

    public TaskCompletedNotificationHandler(INotifier notifier, IOnboardingAudienceResolver audience)
    {
        _notifier = notifier;
        _audience = audience;
    }

    public async Task HandleAsync(TaskCompletedEvent e, CancellationToken ct = default)
    {
        var audience = await _audience.ResolveAsync(e.OnboardingEmployeeId, ct);

        // Supervisors (audience minus the employee) are notified of progress.
        var supervisors = audience.Where(id => id != e.OnboardingEmployeeId);
        await _notifier.NotifyManyAsync(supervisors, NotificationType.TaskCompleted,
            "Onboarding progress", $"Task \"{e.Title}\" was completed ({e.ProgressPercentage}% overall).", ct);

        await _notifier.PushTasksChangedAsync(audience, e.OnboardingEmployeeId, e.ProgressPercentage, ct);
    }
}
