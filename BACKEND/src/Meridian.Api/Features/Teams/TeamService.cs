using System.ComponentModel.DataAnnotations;
using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Exceptions;
using Meridian.Api.Features.Teams.Domain;
using Meridian.Api.Features.Users;
using Meridian.Api.Infrastructure.Audit;
using Meridian.Api.Infrastructure.Auth;
using Meridian.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Features.Teams;

public sealed record CreateTeamRequest(
    [Required, StringLength(120)] string Name,
    [StringLength(2000)] string? Description,
    [Required] int DepartmentId,
    int? TeamLeadId);

public sealed record UpdateTeamRequest(
    [Required, StringLength(120)] string Name,
    [StringLength(2000)] string? Description);

public sealed record TeamDto(
    int Id, string Name, string? Description, int DepartmentId, string DepartmentName,
    int? ManagerId, string? ManagerName, int? TeamLeadId, string? TeamLeadName, int MemberCount);

public interface ITeamService
{
    Task<IReadOnlyList<TeamDto>> ListAsync(int? departmentId, CancellationToken ct = default);
    Task<TeamDto> GetAsync(int id, CancellationToken ct = default);
    Task<TeamDto> CreateAsync(CreateTeamRequest request, CancellationToken ct = default);
    Task<TeamDto> UpdateAsync(int id, UpdateTeamRequest request, CancellationToken ct = default);
    Task AssignEmployeeAsync(int teamId, int userId, CancellationToken ct = default);
    Task AssignTeamLeadAsync(int teamId, int userId, CancellationToken ct = default);
}

/// <summary>
/// Team management with ownership enforcement: HR/Admin have full access, while
/// a Manager may only act on teams inside a department they manage.
/// </summary>
public sealed class TeamService : ITeamService
{
    private readonly MeridianDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _audit;
    private readonly IOrgRoleReconciler _reconciler;

    public TeamService(MeridianDbContext db, ICurrentUser currentUser, IAuditLogger audit,
        IOrgRoleReconciler reconciler)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
        _reconciler = reconciler;
    }

    public async Task<IReadOnlyList<TeamDto>> ListAsync(int? departmentId, CancellationToken ct = default)
    {
        var query = TeamQuery();
        if (departmentId is not null) query = query.Where(t => t.DepartmentId == departmentId);
        var teams = await query.OrderBy(t => t.Name).ToListAsync(ct);
        return teams.Select(ToDto).ToList();
    }

    public async Task<TeamDto> GetAsync(int id, CancellationToken ct = default) =>
        ToDto(await FindTeamOrThrow(id, ct));

    public async Task<TeamDto> CreateAsync(CreateTeamRequest request, CancellationToken ct = default)
    {
        var department = await _db.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId, ct)
            ?? throw NotFoundException.For("Department", request.DepartmentId);

        await EnsureCanManageDepartmentAsync(department.Id, ct);

        var team = new Team
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            DepartmentId = department.Id,
            ManagerId = department.ManagerId, // inherited from the department
            // A new team has no members yet, so its lead is assigned separately
            // (from the team's members) once people have joined.
            TeamLeadId = null
        };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("TeamCreated", "Team", team.Id.ToString(), _currentUser.UserId, ct);
        return await GetAsync(team.Id, ct);
    }

    public async Task<TeamDto> UpdateAsync(int id, UpdateTeamRequest request, CancellationToken ct = default)
    {
        var team = await FindTeamOrThrow(id, ct);
        EnsureCanEditTeam(team);
        team.Name = request.Name.Trim();
        team.Description = request.Description;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("TeamModified", "Team", team.Id.ToString(), _currentUser.UserId, ct);
        return ToDto(team);
    }

    public async Task AssignEmployeeAsync(int teamId, int userId, CancellationToken ct = default)
    {
        var team = await FindTeamOrThrow(teamId, ct);
        await EnsureCanManageDepartmentAsync(team.DepartmentId, ct);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw NotFoundException.For("User", userId);

        // Assigning to a team inherits the team's department automatically.
        user.TeamId = team.Id;
        user.DepartmentId = team.DepartmentId;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("EmployeeAssignedToTeam", "User", user.Id.ToString(), _currentUser.UserId, ct);
    }

    public async Task AssignTeamLeadAsync(int teamId, int userId, CancellationToken ct = default)
    {
        var team = await FindTeamOrThrow(teamId, ct);
        await EnsureCanManageDepartmentAsync(team.DepartmentId, ct);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw NotFoundException.For("User", userId);

        // The Team Lead must be a member of the team they lead.
        if (user.TeamId != team.Id)
            throw new BusinessRuleException("The Team Lead must be a member of this team.");

        var previousLeadId = team.TeamLeadId;
        if (previousLeadId == userId) return;

        team.TeamLeadId = userId;
        await _db.SaveChangesAsync(ct);

        // Keep the Team Lead role in sync with team-lead assignments.
        if (previousLeadId is int previous) await _reconciler.ReconcileTeamLeadRoleAsync(previous, ct);
        await _reconciler.ReconcileTeamLeadRoleAsync(userId, ct);
        await _audit.LogAsync("TeamLeadAssigned", "Team", team.Id.ToString(), _currentUser.UserId, ct);
    }

    private IQueryable<Team> TeamQuery() => _db.Teams
        .Include(t => t.Department)
        .Include(t => t.Manager)
        .Include(t => t.TeamLead)
        .Include(t => t.Members);

    private async Task<Team> FindTeamOrThrow(int id, CancellationToken ct) =>
        await TeamQuery().FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw NotFoundException.For("Team", id);

    /// <summary>
    /// Editing team information is allowed for HR/Admin, the department's Manager,
    /// or the team's own Team Lead.
    /// </summary>
    private void EnsureCanEditTeam(Team team)
    {
        if (_currentUser.IsInRole(RoleNames.Administrator))
            return;
        if (_currentUser.IsInRole(RoleNames.Manager) && team.ManagerId == _currentUser.UserId)
            return;
        if (_currentUser.IsInRole(RoleNames.TeamLead) && team.TeamLeadId == _currentUser.UserId)
            return;
        throw new ForbiddenException("You may only edit your own team.");
    }

    /// <summary>Ownership rule: Admin always passes; a Manager must own the department.</summary>
    private async Task EnsureCanManageDepartmentAsync(int departmentId, CancellationToken ct)
    {
        if (_currentUser.IsInRole(RoleNames.Administrator))
            return;

        if (_currentUser.IsInRole(RoleNames.Manager))
        {
            var owns = await _db.Departments.AnyAsync(
                d => d.Id == departmentId && d.ManagerId == _currentUser.UserId, ct);
            if (owns) return;
        }

        throw new ForbiddenException("You may only manage teams within a department you manage.");
    }

    private static TeamDto ToDto(Team t) => new(
        t.Id, t.Name, t.Description, t.DepartmentId, t.Department?.Name ?? string.Empty,
        t.ManagerId, t.Manager?.FullName, t.TeamLeadId, t.TeamLead?.FullName, t.Members.Count);
}
