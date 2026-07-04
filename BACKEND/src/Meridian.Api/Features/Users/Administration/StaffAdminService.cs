using System.ComponentModel.DataAnnotations;
using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Exceptions;
using Meridian.Api.Features.Users.Domain;
using Meridian.Api.Infrastructure.Audit;
using Meridian.Api.Infrastructure.Auth;
using Meridian.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Features.Users.Administration;

public sealed record CreateStaffRequest(
    [Required, StringLength(100)] string FirstName,
    [Required, StringLength(100)] string LastName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Phone, StringLength(32)] string? PhoneNumber,
    [StringLength(150)] string? JobTitle,
    [StringLength(64)] string? InOfficeDays,
    int? DepartmentId,
    int? TeamId,
    [Required, MinLength(1)] IReadOnlyList<string> Roles);

public sealed record SetRolesRequest([Required, MinLength(1)] IReadOnlyList<string> Roles);

public sealed record CreateStaffResponse(int UserId, string Email, string TemporaryPassword);

public sealed record RoleDto(int Id, string Name);

public sealed record AdminUserDto(
    int Id, string FullName, string Email, string? JobTitle,
    int? DepartmentId, string? DepartmentName, int? TeamId, string? TeamName,
    bool IsOnboarding, bool IsActive, IReadOnlyList<string> Roles);

/// <summary>
/// Administrator-facing management of user accounts and roles. Unlike HR's
/// "create employee" flow (which starts onboarding), staff created here are
/// existing employees (<c>IsOnboarding = false</c>) who can hold any roles —
/// this is how Managers, Team Leads and Mentors get into the system.
/// </summary>
public interface IStaffAdminService
{
    Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(CancellationToken ct = default);
    Task<CreateStaffResponse> CreateStaffAsync(CreateStaffRequest request, CancellationToken ct = default);
    Task<AdminUserDto> SetRolesAsync(int userId, SetRolesRequest request, CancellationToken ct = default);
}

public sealed class StaffAdminService : IStaffAdminService
{
    private readonly MeridianDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogger _audit;
    private readonly ICurrentUser _currentUser;

    public StaffAdminService(MeridianDbContext db, IPasswordHasher passwordHasher,
        IAuditLogger audit, ICurrentUser currentUser)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken ct = default) =>
        await _db.Roles.OrderBy(r => r.Name).Select(r => new RoleDto(r.Id, r.Name)).ToListAsync(ct);

    public async Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(CancellationToken ct = default)
    {
        var users = await _db.Users
            .Include(u => u.Department)
            .Include(u => u.Team)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .ToListAsync(ct);
        return users.Select(ToDto).ToList();
    }

    public async Task<CreateStaffResponse> CreateStaffAsync(CreateStaffRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException($"An account with email '{email}' already exists.");

        // Manager / Team Lead are derived from org assignments, not set directly.
        var requestedRoles = request.Roles.Where(r => !IOrgRoleReconciler.DerivedRoles.Contains(r.Trim())).ToList();
        if (requestedRoles.Count == 0)
            throw new BusinessRuleException(
                "Select at least one role. Manager and Team Lead are assigned from the Organization page.");
        var roles = await ResolveRolesAsync(requestedRoles, ct);
        await ValidateOrgAssignmentAsync(request.DepartmentId, request.TeamId, ct);

        var temporaryPassword = TemporaryPassword.Generate();
        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = _passwordHasher.Hash(temporaryPassword),
            PhoneNumber = request.PhoneNumber,
            JobTitle = request.JobTitle,
            InOfficeDays = request.InOfficeDays,
            DepartmentId = request.DepartmentId,
            TeamId = request.TeamId,
            IsOnboarding = false,
            IsActive = true
        };
        foreach (var role in roles)
            user.UserRoles.Add(new UserRole { Role = role });

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("StaffAccountCreated", "User", user.Id.ToString(), _currentUser.UserId, ct);

        return new CreateStaffResponse(user.Id, user.Email, temporaryPassword);
    }

    public async Task<AdminUserDto> SetRolesAsync(int userId, SetRolesRequest request, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.Department)
            .Include(u => u.Team)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw NotFoundException.For("User", userId);

        // Only editable roles come from the request; Manager / Team Lead are
        // derived from department/team assignments and are preserved as-is.
        var editableNames = request.Roles.Where(r => !IOrgRoleReconciler.DerivedRoles.Contains(r.Trim())).ToList();
        var editableRoles = await ResolveRolesAsync(editableNames, ct);
        var preservedDerived = user.UserRoles
            .Where(ur => IOrgRoleReconciler.DerivedRoles.Contains(ur.Role.Name))
            .Select(ur => ur.RoleId)
            .ToList();

        if (editableRoles.Count == 0 && preservedDerived.Count == 0)
            throw new BusinessRuleException("A user must have at least one role.");

        user.UserRoles.Clear();
        foreach (var role in editableRoles)
            user.UserRoles.Add(new UserRole { RoleId = role.Id });
        foreach (var roleId in preservedDerived)
            user.UserRoles.Add(new UserRole { RoleId = roleId });

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("RolesModified", "User", user.Id.ToString(), _currentUser.UserId, ct);

        // Reload with role names for the response.
        return ToDto(await _db.Users
            .Include(u => u.Department).Include(u => u.Team)
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstAsync(u => u.Id == userId, ct));
    }

    private async Task<List<Role>> ResolveRolesAsync(IReadOnlyList<string> roleNames, CancellationToken ct)
    {
        var requested = roleNames.Select(r => r.Trim()).Distinct().ToList();
        var invalid = requested.Except(RoleNames.All).ToList();
        if (invalid.Count > 0)
            throw new BusinessRuleException($"Unknown role(s): {string.Join(", ", invalid)}.");

        return await _db.Roles.Where(r => requested.Contains(r.Name)).ToListAsync(ct);
    }

    private async Task ValidateOrgAssignmentAsync(int? departmentId, int? teamId, CancellationToken ct)
    {
        if (departmentId is int deptId && !await _db.Departments.AnyAsync(d => d.Id == deptId, ct))
            throw NotFoundException.For("Department", deptId);

        if (teamId is int tId)
        {
            var team = await _db.Teams.FirstOrDefaultAsync(t => t.Id == tId, ct)
                ?? throw NotFoundException.For("Team", tId);
            if (departmentId is not null && team.DepartmentId != departmentId)
                throw new BusinessRuleException("The selected team does not belong to the selected department.");
        }
    }

    private static AdminUserDto ToDto(User u) => new(
        u.Id, u.FullName, u.Email, u.JobTitle,
        u.DepartmentId, u.Department?.Name, u.TeamId, u.Team?.Name,
        u.IsOnboarding, u.IsActive,
        u.UserRoles.Select(ur => ur.Role.Name).OrderBy(n => n).ToList());
}
