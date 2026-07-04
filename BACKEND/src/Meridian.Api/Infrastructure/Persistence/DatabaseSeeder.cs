using Meridian.Api.Common.Domain;
using Meridian.Api.Features.Departments.Domain;
using Meridian.Api.Features.Tasks.Domain;
using Meridian.Api.Features.Teams.Domain;
using Meridian.Api.Features.Users.Domain;
using Meridian.Api.Infrastructure.Auth;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Infrastructure.Persistence;

/// <summary>
/// Applies migrations and seeds baseline reference data (roles), a bootstrap
/// Administrator and HR account, and a couple of starter task templates so the
/// application is usable immediately after a fresh deploy.
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly MeridianDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(MeridianDbContext db, IPasswordHasher passwordHasher,
        IConfiguration configuration, ILogger<DatabaseSeeder> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await _db.Database.MigrateAsync(ct);

        await SeedRolesAsync(ct);
        await SeedBootstrapUsersAsync(ct);
        await SeedSampleOrganizationAsync(ct);
        await ReconcileOrganizationAsync(ct);

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Idempotently brings the organizational data into a consistent state on
    /// startup: every team's manager matches its department's manager, and the
    /// Manager / Team Lead roles reflect who actually manages a department or
    /// leads a team. Fixes data created before these rules were enforced.
    /// </summary>
    private async Task ReconcileOrganizationAsync(CancellationToken ct)
    {
        var departmentManagers = await _db.Departments.ToDictionaryAsync(d => d.Id, d => d.ManagerId, ct);
        var teams = await _db.Teams.ToListAsync(ct);
        foreach (var team in teams)
        {
            var deptManager = departmentManagers.TryGetValue(team.DepartmentId, out var m) ? m : null;
            if (team.ManagerId != deptManager) team.ManagerId = deptManager;
        }
        await _db.SaveChangesAsync(ct);

        var managerRoleId = await _db.Roles.Where(r => r.Name == RoleNames.Manager).Select(r => r.Id).FirstAsync(ct);
        var leadRoleId = await _db.Roles.Where(r => r.Name == RoleNames.TeamLead).Select(r => r.Id).FirstAsync(ct);
        var managerUserIds = await _db.Departments.Where(d => d.ManagerId != null)
            .Select(d => d.ManagerId!.Value).Distinct().ToListAsync(ct);
        var leadUserIds = await _db.Teams.Where(t => t.TeamLeadId != null)
            .Select(t => t.TeamLeadId!.Value).Distinct().ToListAsync(ct);

        var users = await _db.Users.Include(u => u.UserRoles).ToListAsync(ct);
        foreach (var user in users)
        {
            SyncRole(user, managerRoleId, managerUserIds.Contains(user.Id));
            SyncRole(user, leadRoleId, leadUserIds.Contains(user.Id));
        }
        await _db.SaveChangesAsync(ct);

        // Give any authorless (seeded) templates a real creator so tasks made from
        // them are credited correctly rather than to whoever onboards the employee.
        var orphanTemplates = await _db.TaskTemplates.Where(t => t.CreatedById == 0).ToListAsync(ct);
        if (orphanTemplates.Count > 0)
        {
            var creatorId =
                await _db.Users.Where(u => u.UserRoles.Any(ur => ur.Role.Name == RoleNames.HrEmployee))
                    .OrderBy(u => u.Id).Select(u => (int?)u.Id).FirstOrDefaultAsync(ct)
                ?? await _db.Users.Where(u => u.UserRoles.Any(ur => ur.Role.Name == RoleNames.Administrator))
                    .OrderBy(u => u.Id).Select(u => (int?)u.Id).FirstOrDefaultAsync(ct);

            if (creatorId is int cid)
            {
                foreach (var template in orphanTemplates) template.CreatedById = cid;
                await _db.SaveChangesAsync(ct);
            }
        }
    }

    private static void SyncRole(User user, int roleId, bool shouldHave)
    {
        var existing = user.UserRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (shouldHave && existing is null) user.UserRoles.Add(new UserRole { RoleId = roleId });
        else if (!shouldHave && existing is not null) user.UserRoles.Remove(existing);
    }

    private async Task SeedRolesAsync(CancellationToken ct)
    {
        var existing = await _db.Roles.Select(r => r.Name).ToListAsync(ct);
        foreach (var name in RoleNames.All.Except(existing))
            _db.Roles.Add(new Role { Name = name });
        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedBootstrapUsersAsync(CancellationToken ct)
    {
        if (await _db.Users.AnyAsync(ct)) return;

        var roles = await _db.Roles.ToDictionaryAsync(r => r.Name, ct);
        var adminPassword = _configuration["Seed:AdminPassword"] ?? "Admin#12345";
        var hrPassword = _configuration["Seed:HrPassword"] ?? "HrUser#12345";

        var admin = new User
        {
            FirstName = "System", LastName = "Administrator",
            Email = "admin@meridian.local",
            PasswordHash = _passwordHasher.Hash(adminPassword),
            IsActive = true
        };
        admin.UserRoles.Add(new UserRole { Role = roles[RoleNames.Administrator] });

        var hr = new User
        {
            FirstName = "Hannah", LastName = "Reeves",
            Email = "hr@meridian.local",
            PasswordHash = _passwordHasher.Hash(hrPassword),
            IsActive = true
        };
        hr.UserRoles.Add(new UserRole { Role = roles[RoleNames.HrEmployee] });

        _db.Users.AddRange(admin, hr);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded bootstrap Administrator (admin@meridian.local) and HR (hr@meridian.local) accounts.");
    }

    private async Task SeedSampleOrganizationAsync(CancellationToken ct)
    {
        if (await _db.Departments.AnyAsync(ct)) return;

        var engineering = new Department { Name = "Engineering" };
        _db.Departments.Add(engineering);
        await _db.SaveChangesAsync(ct);

        var team = new Team
        {
            Name = "Platform",
            Description = "Core platform and services team.",
            DepartmentId = engineering.Id
        };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync(ct);

        _db.TaskTemplates.AddRange(
            new TaskTemplate
            {
                Title = "Complete HR paperwork", Category = TaskCategory.Hr,
                Priority = TaskPriority.High, EstimatedCompletionDays = 2, CreatedById = 0
            },
            new TaskTemplate
            {
                // Scoped to the Platform team (and its department) so it is only
                // assigned to new Platform employees.
                Title = "Set up development environment", Category = TaskCategory.Team,
                Priority = TaskPriority.Medium, EstimatedCompletionDays = 3,
                DepartmentId = engineering.Id, TeamId = team.Id, CreatedById = 0
            });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded sample Engineering department and Platform team.");
    }
}
