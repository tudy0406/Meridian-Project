namespace Meridian.Api.Common.Domain;

/// <summary>Well-known role names used throughout the application for RBAC.</summary>
public static class RoleNames
{
    public const string Administrator = "Administrator";
    public const string HrEmployee = "HR Employee";
    public const string Manager = "Manager";
    public const string TeamLead = "Team Lead";
    public const string Mentor = "Mentor";
    public const string Employee = "Employee";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Administrator, HrEmployee, Manager, TeamLead, Mentor, Employee
    };
}

public enum OnboardingStatus
{
    Pending = 0,
    Active = 1,
    Completed = 2,
    Cancelled = 3
}

public enum EmployeeTaskStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2
}

/// <summary>Origin/scope of an onboarding task or template.</summary>
public enum TaskCategory
{
    Hr = 0,
    Department = 1,
    Team = 2,
    Personal = 3
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2
}

public enum DocumentationCategory
{
    General = 0,
    Technologies = 1,
    Communication = 2,
    Projects = 3,
    Workflow = 4,
    VersionControl = 5,
    CodingStandards = 6,
    Faq = 7
}

public enum NotificationType
{
    TaskAssigned = 0,
    TaskCompleted = 1,
    MeetingScheduled = 2,
    MeetingUpdated = 3,
    DeadlineApproaching = 4,
    DocumentationUpdated = 5
}
