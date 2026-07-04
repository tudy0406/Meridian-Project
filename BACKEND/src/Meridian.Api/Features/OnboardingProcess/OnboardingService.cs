using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Exceptions;
using Meridian.Api.Infrastructure.Audit;
using Meridian.Api.Infrastructure.Auth;
using Meridian.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Features.OnboardingProcess;

public sealed record OnboardingSummaryDto(
    int OnboardingId, int EmployeeId, string EmployeeName, string? TeamName,
    int? MentorId, string? MentorName, string Status, int ProgressPercentage,
    DateTime StartDate, int TaskCount, int CompletedTaskCount);

public interface IOnboardingService
{
    Task<OnboardingSummaryDto> GetForEmployeeAsync(int employeeId, CancellationToken ct = default);
    Task<IReadOnlyList<OnboardingSummaryDto>> ListVisibleAsync(CancellationToken ct = default);
    Task AssignMentorAsync(int employeeId, int mentorId, CancellationToken ct = default);
}

/// <summary>
/// Read/monitor onboarding progress and manage mentor assignment. Visibility is
/// scoped by role: HR/Admin see everyone, Managers their department, Team Leads
/// their teams, Mentors their assigned employees, and employees themselves.
/// </summary>
public sealed class OnboardingService : IOnboardingService
{
    private readonly MeridianDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IOnboardingAudienceResolver _audience;
    private readonly IAuditLogger _audit;

    public OnboardingService(MeridianDbContext db, ICurrentUser currentUser,
        IOnboardingAudienceResolver audience, IAuditLogger audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audience = audience;
        _audit = audit;
    }

    public async Task<OnboardingSummaryDto> GetForEmployeeAsync(int employeeId, CancellationToken ct = default)
    {
        // A user can always see their own onboarding; otherwise they must be in
        // the employee's onboarding audience (mentor/team lead/manager) or HR/Admin.
        if (employeeId != _currentUser.UserId && !_currentUser.IsInRole(RoleNames.HrEmployee)
            && !_currentUser.IsInRole(RoleNames.Administrator))
        {
            var audience = await _audience.ResolveAsync(employeeId, ct);
            if (!audience.Contains(_currentUser.RequireUserId()))
                throw new ForbiddenException("You are not allowed to view this onboarding.");
        }

        // Filter on the entity query before projecting: the projection contains
        // correlated Count subqueries, which EF cannot translate if a Where is
        // applied to the projected DTO.
        var summary = await Project(QuerySummariesEntity().Where(o => o.EmployeeId == employeeId))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"No onboarding found for employee '{employeeId}'.");
        return summary;
    }

    public async Task<IReadOnlyList<OnboardingSummaryDto>> ListVisibleAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.RequireUserId();
        var query = QuerySummariesEntity();

        // HR/Admin monitor the whole organization. Everyone else sees the UNION
        // of their supervisory scopes so a person holding several responsibilities
        // (e.g. Mentor + Team Lead) sees everyone they oversee:
        //   Mentor    -> employees they mentor
        //   Team Lead -> all new employees on their team
        //   Manager   -> all new employees in their department
        if (!_currentUser.IsInRole(RoleNames.HrEmployee) && !_currentUser.IsInRole(RoleNames.Administrator))
        {
            var isMentor = _currentUser.IsInRole(RoleNames.Mentor);
            var isTeamLead = _currentUser.IsInRole(RoleNames.TeamLead);
            var isManager = _currentUser.IsInRole(RoleNames.Manager);

            query = query.Where(o =>
                o.EmployeeId == userId
                || (isMentor && o.MentorId == userId)
                || (isTeamLead && o.Employee.Team != null && o.Employee.Team.TeamLeadId == userId)
                || (isManager && o.Employee.Department != null && o.Employee.Department.ManagerId == userId));
        }

        return await Project(query).ToListAsync(ct);
    }

    public async Task AssignMentorAsync(int employeeId, int mentorId, CancellationToken ct = default)
    {
        var onboarding = await _db.Onboardings
            .Include(o => o.Employee)
            .FirstOrDefaultAsync(o => o.EmployeeId == employeeId, ct)
            ?? throw new NotFoundException($"No onboarding found for employee '{employeeId}'.");

        await EnsureCanManageTeamAsync(onboarding.Employee.TeamId, ct);

        var mentorInTeam = await _db.Users.AnyAsync(
            u => u.Id == mentorId && u.TeamId == onboarding.Employee.TeamId, ct);
        if (!mentorInTeam)
            throw new BusinessRuleException("The mentor must belong to the employee's team.");

        onboarding.MentorId = mentorId;

        // Notify the Tasks feature so the new mentor's personal templates are
        // auto-assigned to this mentee.
        onboarding.Raise(new Events.MentorAssignedEvent(onboarding.Id, employeeId, mentorId));
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("MentorAssigned", "Onboarding", onboarding.Id.ToString(), _currentUser.UserId, ct);
    }

    /// <summary>Team Lead of the team, or HR/Admin, may reassign mentors.</summary>
    private async Task EnsureCanManageTeamAsync(int? teamId, CancellationToken ct)
    {
        if (_currentUser.IsInRole(RoleNames.HrEmployee) || _currentUser.IsInRole(RoleNames.Administrator))
            return;

        if (teamId is int id && _currentUser.IsInRole(RoleNames.TeamLead))
        {
            var isLead = await _db.Teams.AnyAsync(t => t.Id == id && t.TeamLeadId == _currentUser.UserId, ct);
            if (isLead) return;
        }

        throw new ForbiddenException("You may only reassign mentors within your own team.");
    }

    private IQueryable<Domain.Onboarding> QuerySummariesEntity() => _db.Onboardings
        .Include(o => o.Employee).ThenInclude(e => e.Team)
        .Include(o => o.Employee).ThenInclude(e => e.Department)
        .Include(o => o.Mentor)
        .Include(o => o.Tasks);

    private static IQueryable<OnboardingSummaryDto> Project(IQueryable<Domain.Onboarding> query) =>
        query.Select(o => new OnboardingSummaryDto(
            o.Id, o.EmployeeId, o.Employee.FirstName + " " + o.Employee.LastName,
            o.Employee.Team != null ? o.Employee.Team.Name : null,
            o.MentorId, o.Mentor != null ? o.Mentor.FirstName + " " + o.Mentor.LastName : null,
            o.Status.ToString(), o.ProgressPercentage, o.StartDate,
            o.Tasks.Count, o.Tasks.Count(t => t.Status == EmployeeTaskStatus.Completed)));
}
