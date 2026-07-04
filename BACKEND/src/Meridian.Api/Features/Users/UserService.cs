using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Exceptions;
using Meridian.Api.Features.OnboardingProcess;
using Meridian.Api.Features.Users.Domain;
using Meridian.Api.Features.Users.Factory;
using Meridian.Api.Infrastructure.Audit;
using Meridian.Api.Infrastructure.Auth;
using Meridian.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Features.Users;

public sealed class UserService : IUserService
{
    private readonly MeridianDbContext _db;
    private readonly INewEmployeeFactory _employeeFactory;
    private readonly IAuditLogger _audit;
    private readonly ICurrentUser _currentUser;
    private readonly IOnboardingAudienceResolver _audience;

    public UserService(MeridianDbContext db, INewEmployeeFactory employeeFactory, IAuditLogger audit,
        ICurrentUser currentUser, IOnboardingAudienceResolver audience)
    {
        _db = db;
        _employeeFactory = employeeFactory;
        _audit = audit;
        _currentUser = currentUser;
        _audience = audience;
    }

    public async Task<CreateEmployeeResponse> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException($"An account with email '{email}' already exists.");

        var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == request.TeamId, ct)
            ?? throw NotFoundException.For("Team", request.TeamId);

        if (team.DepartmentId != request.DepartmentId)
            throw new BusinessRuleException("The selected team does not belong to the selected department.");

        if (request.MentorId is int mentorId)
        {
            var mentorInTeam = await _db.Users.AnyAsync(u => u.Id == mentorId && u.TeamId == team.Id, ct);
            if (!mentorInTeam)
                throw new BusinessRuleException("The selected mentor must belong to the employee's team.");
        }

        var employeeRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Employee, ct)
            ?? throw new BusinessRuleException("The Employee role is not configured.");

        var temporaryPassword = TemporaryPassword.Generate();
        var spec = new NewEmployeeSpec(
            request.FirstName.Trim(),
            request.LastName.Trim(),
            email,
            temporaryPassword,
            request.PhoneNumber,
            request.JobTitle,
            request.InOfficeDays,
            request.DepartmentId,
            request.TeamId,
            request.MentorId,
            employeeRole,
            CreatedById: _currentUser.RequireUserId());

        var user = await _employeeFactory.CreateAsync(spec, ct);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync("AccountCreated", "User", user.Id.ToString(), user.Id, ct);

        return new CreateEmployeeResponse(user.Id, user.Email, temporaryPassword);
    }

    public async Task<UserProfileDto> GetProfileAsync(int userId, CancellationToken ct = default)
    {
        var user = await LoadProfileQuery().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw NotFoundException.For("User", userId);
        return ToProfileDto(user, await CanAccessOnboardingAsync(user, ct));
    }

    /// <summary>
    /// Whether the current viewer may see this employee's onboarding: the owner,
    /// HR/Admin, or someone in the onboarding audience (their mentor / team lead /
    /// manager). Drives the UI so, e.g., a mentor only sees onboarding data for the
    /// employees they actually mentor.
    /// </summary>
    private async Task<bool> CanAccessOnboardingAsync(User user, CancellationToken ct)
    {
        if (user.Onboarding is null || _currentUser.UserId is not int me) return false;
        if (user.Id == me || _currentUser.IsInRole(RoleNames.HrEmployee) || _currentUser.IsInRole(RoleNames.Administrator))
            return true;
        var audience = await _audience.ResolveAsync(user.Id, ct);
        return audience.Contains(me);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw NotFoundException.For("User", userId);

        user.PhoneNumber = request.PhoneNumber;
        user.JobTitle = request.JobTitle;
        user.InOfficeDays = request.InOfficeDays;
        await _db.SaveChangesAsync(ct);

        return await GetProfileAsync(userId, ct);
    }

    public async Task<IReadOnlyList<UserSummaryDto>> ListAsync(int? teamId, int? departmentId, CancellationToken ct = default)
    {
        var query = _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (teamId is not null) query = query.Where(u => u.TeamId == teamId);
        if (departmentId is not null) query = query.Where(u => u.DepartmentId == departmentId);

        var users = await query.OrderBy(u => u.LastName).ToListAsync(ct);
        return users.Select(ToSummaryDto).ToList();
    }

    public async Task<IReadOnlyList<UserSummaryDto>> GetTeamMembersAsync(int teamId, CancellationToken ct = default)
    {
        var users = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Where(u => u.TeamId == teamId && u.IsActive)
            .OrderBy(u => u.LastName)
            .ToListAsync(ct);
        return users.Select(ToSummaryDto).ToList();
    }

    private IQueryable<User> LoadProfileQuery() => _db.Users
        .Include(u => u.Department)
        .Include(u => u.Team)
        .Include(u => u.Onboarding)
        .Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

    private static UserProfileDto ToProfileDto(User u, bool canAccessOnboarding) => new(
        u.Id, u.FullName, u.Email, u.PhoneNumber, u.JobTitle,
        u.DepartmentId, u.Department?.Name, u.TeamId, u.Team?.Name,
        u.InOfficeDays, u.IsOnboarding, u.Onboarding?.Status.ToString(), canAccessOnboarding,
        u.UserRoles.Select(ur => ur.Role.Name).ToArray());

    private static UserSummaryDto ToSummaryDto(User u) => new(
        u.Id, u.FullName, u.Email, u.JobTitle, u.DepartmentId, u.TeamId,
        u.UserRoles.Select(ur => ur.Role.Name).ToArray());
}
