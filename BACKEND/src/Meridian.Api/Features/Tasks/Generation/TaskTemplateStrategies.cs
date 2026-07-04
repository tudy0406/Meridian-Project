using Meridian.Api.Common.Domain;
using Meridian.Api.Features.Tasks.Domain;
using Meridian.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Features.Tasks.Generation;

/// <summary>Company-wide HR templates apply to every new employee.</summary>
public sealed class HrTaskTemplateStrategy : ITaskTemplateStrategy
{
    private readonly MeridianDbContext _db;
    public HrTaskTemplateStrategy(MeridianDbContext db) => _db = db;

    public TaskCategory Category => TaskCategory.Hr;

    public async Task<IReadOnlyList<TaskTemplate>> SelectTemplatesAsync(OnboardingContext ctx, CancellationToken ct = default) =>
        await _db.TaskTemplates
            .Where(t => t.IsActive && t.Category == TaskCategory.Hr)
            .ToListAsync(ct);
}

/// <summary>Department templates apply when scoped to the employee's department.</summary>
public sealed class DepartmentTaskTemplateStrategy : ITaskTemplateStrategy
{
    private readonly MeridianDbContext _db;
    public DepartmentTaskTemplateStrategy(MeridianDbContext db) => _db = db;

    public TaskCategory Category => TaskCategory.Department;

    public async Task<IReadOnlyList<TaskTemplate>> SelectTemplatesAsync(OnboardingContext ctx, CancellationToken ct = default)
    {
        if (ctx.DepartmentId is null) return Array.Empty<TaskTemplate>();
        return await _db.TaskTemplates
            .Where(t => t.IsActive && t.Category == TaskCategory.Department && t.DepartmentId == ctx.DepartmentId)
            .ToListAsync(ct);
    }
}

/// <summary>Team templates apply when scoped to the employee's team.</summary>
public sealed class TeamTaskTemplateStrategy : ITaskTemplateStrategy
{
    private readonly MeridianDbContext _db;
    public TeamTaskTemplateStrategy(MeridianDbContext db) => _db = db;

    public TaskCategory Category => TaskCategory.Team;

    public async Task<IReadOnlyList<TaskTemplate>> SelectTemplatesAsync(OnboardingContext ctx, CancellationToken ct = default)
    {
        if (ctx.TeamId is null) return Array.Empty<TaskTemplate>();
        return await _db.TaskTemplates
            .Where(t => t.IsActive && t.Category == TaskCategory.Team && t.TeamId == ctx.TeamId)
            .ToListAsync(ct);
    }
}

/// <summary>
/// Personal templates apply when authored by the employee's assigned mentor, so
/// a mentor's own templates are automatically assigned to the people they mentor.
/// </summary>
public sealed class PersonalTaskTemplateStrategy : ITaskTemplateStrategy
{
    private readonly MeridianDbContext _db;
    public PersonalTaskTemplateStrategy(MeridianDbContext db) => _db = db;

    public TaskCategory Category => TaskCategory.Personal;

    public async Task<IReadOnlyList<TaskTemplate>> SelectTemplatesAsync(OnboardingContext ctx, CancellationToken ct = default)
    {
        if (ctx.MentorId is null) return Array.Empty<TaskTemplate>();
        return await _db.TaskTemplates
            .Where(t => t.IsActive && t.Category == TaskCategory.Personal && t.CreatedById == ctx.MentorId)
            .ToListAsync(ct);
    }
}
