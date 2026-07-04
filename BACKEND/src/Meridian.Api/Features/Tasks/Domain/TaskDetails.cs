using Meridian.Api.Common.Domain;
using Meridian.Api.Features.Users.Domain;

namespace Meridian.Api.Features.Tasks.Domain;

/// <summary>A file/link attached to an onboarding task (stored as a reference URL).</summary>
public class EmployeeTaskAttachment : BaseEntity
{
    public int EmployeeTaskId { get; set; }
    public EmployeeTask EmployeeTask { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A comment left on a task by the employee or a supervisor.</summary>
public class EmployeeTaskComment : BaseEntity
{
    public int EmployeeTaskId { get; set; }
    public EmployeeTask EmployeeTask { get; set; } = null!;

    public int AuthorId { get; set; }
    public User? Author { get; set; }

    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>A single status transition, forming the task's completion history.</summary>
public class EmployeeTaskHistory : BaseEntity
{
    public int EmployeeTaskId { get; set; }
    public EmployeeTask EmployeeTask { get; set; } = null!;

    public EmployeeTaskStatus Status { get; set; }
    public int ChangedById { get; set; }
    public User? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
