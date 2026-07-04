using Meridian.Api.Common.Domain;

namespace Meridian.Api.Features.Tasks.Domain;

/// <summary>
/// A reusable onboarding task definition. Templates are never assigned directly;
/// they are copied into <see cref="EmployeeTask"/> instances when onboarding
/// begins, so later template edits never mutate an in-progress onboarding.
/// </summary>
public class TaskTemplate : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Acceptance criteria copied into tasks created from this template.</summary>
    public string? Requirements { get; set; }

    public TaskCategory Category { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public int EstimatedCompletionDays { get; set; } = 7;

    /// <summary>Scoping: Department-level templates apply to a department.</summary>
    public int? DepartmentId { get; set; }

    /// <summary>Scoping: Team-level templates apply to a team.</summary>
    public int? TeamId { get; set; }

    public int CreatedById { get; set; }

    public bool IsActive { get; set; } = true;
}
