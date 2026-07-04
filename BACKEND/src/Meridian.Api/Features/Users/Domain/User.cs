using Meridian.Api.Common.Domain;
using Meridian.Api.Features.Departments.Domain;
using Meridian.Api.Features.Notifications.Domain;
using Meridian.Api.Features.OnboardingProcess.Domain;
using Meridian.Api.Features.Teams.Domain;

namespace Meridian.Api.Features.Users.Domain;

/// <summary>
/// Every person in the system. Responsibilities (Manager, Team Lead, Mentor,
/// HR, Admin) are modelled through <see cref="Roles"/> rather than separate
/// tables, so one account can hold several responsibilities at once.
/// </summary>
public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? JobTitle { get; set; }

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int? TeamId { get; set; }
    public Team? Team { get; set; }

    /// <summary>True while this user is going through onboarding.</summary>
    public bool IsOnboarding { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Comma-separated office days for the hybrid model (e.g. "Mon,Tue,Wed").</summary>
    public string? InOfficeDays { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    /// <summary>The active onboarding process, if this user is a new employee.</summary>
    public Onboarding? Onboarding { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
