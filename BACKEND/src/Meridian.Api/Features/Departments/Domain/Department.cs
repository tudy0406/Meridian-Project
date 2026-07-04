using Meridian.Api.Common.Domain;
using Meridian.Api.Features.Teams.Domain;

namespace Meridian.Api.Features.Departments.Domain;

/// <summary>
/// A company department. Departments form the organizational boundary that many
/// authorization rules are scoped to (e.g. a Manager manages teams within their
/// own department).
/// </summary>
public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>The manager responsible for this department (a User).</summary>
    public int? ManagerId { get; set; }

    public ICollection<Team> Teams { get; set; } = new List<Team>();
}
