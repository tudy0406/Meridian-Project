using Meridian.Api.Common.Domain;
using Meridian.Api.Features.Departments.Domain;
using Meridian.Api.Features.Documentation.Domain;
using Meridian.Api.Features.Users.Domain;

namespace Meridian.Api.Features.Teams.Domain;

/// <summary>
/// A working team inside a department. Manager and Team Lead are properties of
/// the team, so when a new employee is assigned to a team these relationships
/// are inherited automatically.
/// </summary>
public class Team : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public int? ManagerId { get; set; }
    public User? Manager { get; set; }

    public int? TeamLeadId { get; set; }
    public User? TeamLead { get; set; }

    public ICollection<User> Members { get; set; } = new List<User>();
    public ICollection<TeamDocumentation> Documentation { get; set; } = new List<TeamDocumentation>();
}
