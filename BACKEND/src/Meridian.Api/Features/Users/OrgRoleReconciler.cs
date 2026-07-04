using Meridian.Api.Common.Domain;
using Meridian.Api.Features.Users.Domain;
using Meridian.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Features.Users;

/// <summary>
/// Keeps the positional roles (Manager, Team Lead) in sync with the actual
/// organizational assignments. A user holds the Manager role iff they manage at
/// least one department, and the Team Lead role iff they lead at least one team.
/// These roles are therefore never edited directly — they follow the Organization
/// page's manager/team-lead assignments.
/// </summary>
public interface IOrgRoleReconciler
{
    /// <summary>Roles that are derived from org assignments and cannot be set manually.</summary>
    static readonly IReadOnlySet<string> DerivedRoles =
        new HashSet<string> { RoleNames.Manager, RoleNames.TeamLead };

    Task ReconcileManagerRoleAsync(int userId, CancellationToken ct = default);
    Task ReconcileTeamLeadRoleAsync(int userId, CancellationToken ct = default);
}

public sealed class OrgRoleReconciler : IOrgRoleReconciler
{
    private readonly MeridianDbContext _db;

    public OrgRoleReconciler(MeridianDbContext db) => _db = db;

    public async Task ReconcileManagerRoleAsync(int userId, CancellationToken ct = default)
    {
        var manages = await _db.Departments.AnyAsync(d => d.ManagerId == userId, ct);
        await SetRolePresenceAsync(userId, RoleNames.Manager, manages, ct);
    }

    public async Task ReconcileTeamLeadRoleAsync(int userId, CancellationToken ct = default)
    {
        var leads = await _db.Teams.AnyAsync(t => t.TeamLeadId == userId, ct);
        await SetRolePresenceAsync(userId, RoleNames.TeamLead, leads, ct);
    }

    private async Task SetRolePresenceAsync(int userId, string roleName, bool shouldHave, CancellationToken ct)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return;

        var existing = user.UserRoles.FirstOrDefault(ur => ur.Role.Name == roleName);

        if (shouldHave && existing is null)
        {
            var role = await _db.Roles.FirstAsync(r => r.Name == roleName, ct);
            user.UserRoles.Add(new UserRole { RoleId = role.Id });
            await _db.SaveChangesAsync(ct);
        }
        else if (!shouldHave && existing is not null)
        {
            user.UserRoles.Remove(existing);
            await _db.SaveChangesAsync(ct);
        }
    }
}
