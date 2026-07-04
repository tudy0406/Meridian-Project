using Meridian.Api.Common.Domain;
using Meridian.Api.Features.OnboardingProcess.Domain;
using Meridian.Api.Features.Users.Domain;

namespace Meridian.Api.Features.Tasks.Domain;

/// <summary>
/// An onboarding task assigned to a specific employee. Stores a self-contained
/// copy of the originating <see cref="TaskTemplate"/> (title/description/etc.)
/// so historical onboarding data is preserved even if the template changes.
/// </summary>
public class EmployeeTask : BaseEntity
{
    public int OnboardingId { get; set; }
    public Onboarding Onboarding { get; set; } = null!;

    /// <summary>Originating template, if any (personalized tasks have none).</summary>
    public int? TaskTemplateId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>What "done" looks like for this task (acceptance criteria).</summary>
    public string? Requirements { get; set; }

    public TaskCategory Category { get; set; }
    public EmployeeTaskStatus Status { get; set; } = EmployeeTaskStatus.NotStarted;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? Deadline { get; set; }

    public int AssignedById { get; set; }
    public User? AssignedBy { get; set; }

    /// <summary>Optional contact person linked to their employee profile.</summary>
    public int? ContactPersonId { get; set; }
    public User? ContactPerson { get; set; }

    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<EmployeeTaskAttachment> Attachments { get; set; } = new List<EmployeeTaskAttachment>();
    public ICollection<EmployeeTaskComment> Comments { get; set; } = new List<EmployeeTaskComment>();
    public ICollection<EmployeeTaskHistory> History { get; set; } = new List<EmployeeTaskHistory>();

    /// <summary>Records a status transition in the completion history.</summary>
    public void ChangeStatus(EmployeeTaskStatus status, int changedById)
    {
        Status = status;
        CompletedAt = status == EmployeeTaskStatus.Completed ? DateTime.UtcNow : null;
        History.Add(new EmployeeTaskHistory
        {
            Status = status,
            ChangedById = changedById,
            ChangedAt = DateTime.UtcNow
        });
    }
}
