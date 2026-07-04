using System.ComponentModel.DataAnnotations;
using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Exceptions;
using Meridian.Api.Features.Departments.Domain;
using Meridian.Api.Features.Users;
using Meridian.Api.Infrastructure.Audit;
using Meridian.Api.Infrastructure.Auth;
using Meridian.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Features.Departments;

public sealed record CreateDepartmentRequest(
    [Required, StringLength(120)] string Name,
    [StringLength(2000)] string? Description);

public sealed record UpdateDepartmentRequest(
    [Required, StringLength(120)] string Name,
    [StringLength(2000)] string? Description,
    int? ManagerId);

public sealed record DepartmentDto(
    int Id, string Name, string? Description, int? ManagerId, string? ManagerName, int TeamCount);

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> ListAsync(CancellationToken ct = default);
    Task<DepartmentDto> GetAsync(int id, CancellationToken ct = default);
    Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request, CancellationToken ct = default);
    Task<DepartmentDto> UpdateAsync(int id, UpdateDepartmentRequest request, CancellationToken ct = default);
}

/// <summary>
/// Department management. HR/Admin have full control (including assigning the
/// manager); a Manager may edit the name/description of a department they own but
/// cannot reassign it to someone else.
/// </summary>
public sealed class DepartmentService : IDepartmentService
{
    private readonly MeridianDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly ICurrentUser _currentUser;
    private readonly IOrgRoleReconciler _reconciler;

    public DepartmentService(MeridianDbContext db, IAuditLogger audit, ICurrentUser currentUser,
        IOrgRoleReconciler reconciler)
    {
        _db = db;
        _audit = audit;
        _currentUser = currentUser;
        _reconciler = reconciler;
    }

    public async Task<IReadOnlyList<DepartmentDto>> ListAsync(CancellationToken ct = default)
    {
        var departments = await _db.Departments.Include(d => d.Teams).OrderBy(d => d.Name).ToListAsync(ct);
        var managerNames = await ManagerNameLookupAsync(departments.Select(d => d.ManagerId), ct);
        return departments.Select(d => ToDto(d, managerNames)).ToList();
    }

    public async Task<DepartmentDto> GetAsync(int id, CancellationToken ct = default)
    {
        var department = await _db.Departments.Include(d => d.Teams).FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw NotFoundException.For("Department", id);
        var managerNames = await ManagerNameLookupAsync(new[] { department.ManagerId }, ct);
        return ToDto(department, managerNames);
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (await _db.Departments.AnyAsync(d => d.Name == name, ct))
            throw new ConflictException($"A department named '{name}' already exists.");

        var department = new Department { Name = name, Description = request.Description };
        _db.Departments.Add(department);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("DepartmentCreated", "Department", department.Id.ToString(), ct: ct);
        return await GetAsync(department.Id, ct);
    }

    public async Task<DepartmentDto> UpdateAsync(int id, UpdateDepartmentRequest request, CancellationToken ct = default)
    {
        var department = await _db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw NotFoundException.For("Department", id);

        var isPrivileged = _currentUser.IsInRole(RoleNames.Administrator);
        var isOwningManager = _currentUser.IsInRole(RoleNames.Manager) && department.ManagerId == _currentUser.UserId;
        if (!isPrivileged && !isOwningManager)
            throw new ForbiddenException("You may only edit a department you manage.");

        department.Name = request.Name.Trim();
        department.Description = request.Description;

        var previousManagerId = department.ManagerId;
        var managerChanged = false;

        // Only an Administrator may (re)assign the department manager.
        if (isPrivileged && request.ManagerId != department.ManagerId)
        {
            if (request.ManagerId is int managerId)
            {
                var candidate = await _db.Users.FirstOrDefaultAsync(u => u.Id == managerId, ct)
                    ?? throw new BusinessRuleException("The selected manager does not exist.");
                // The manager must belong to the department they manage.
                if (candidate.DepartmentId != department.Id)
                    throw new BusinessRuleException("The manager must belong to this department.");
            }
            department.ManagerId = request.ManagerId;
            managerChanged = true;

            // A department has exactly one manager; propagate to its teams so every
            // view (team pages, profiles) reflects the same manager.
            var teams = await _db.Teams.Where(t => t.DepartmentId == department.Id).ToListAsync(ct);
            foreach (var team in teams) team.ManagerId = request.ManagerId;
        }

        await _db.SaveChangesAsync(ct);

        // Keep the Manager role in sync with department-manager assignments.
        if (managerChanged)
        {
            if (previousManagerId is int previous) await _reconciler.ReconcileManagerRoleAsync(previous, ct);
            if (request.ManagerId is int current) await _reconciler.ReconcileManagerRoleAsync(current, ct);
        }

        await _audit.LogAsync("DepartmentModified", "Department", department.Id.ToString(), _currentUser.UserId, ct);
        return await GetAsync(department.Id, ct);
    }

    private async Task<Dictionary<int, string>> ManagerNameLookupAsync(IEnumerable<int?> managerIds, CancellationToken ct)
    {
        var ids = managerIds.Where(id => id is not null).Select(id => id!.Value).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, string>();
        return await _db.Users.Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FirstName + " " + u.LastName, ct);
    }

    private static DepartmentDto ToDto(Department d, IReadOnlyDictionary<int, string> managerNames) => new(
        d.Id, d.Name, d.Description, d.ManagerId,
        d.ManagerId is int mid && managerNames.TryGetValue(mid, out var name) ? name : null,
        d.Teams.Count);
}
