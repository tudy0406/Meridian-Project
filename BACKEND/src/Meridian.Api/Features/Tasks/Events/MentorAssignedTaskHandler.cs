using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Domain.Events;
using Meridian.Api.Features.OnboardingProcess.Events;
using Meridian.Api.Features.Tasks.Domain;
using Meridian.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Features.Tasks.Events;

/// <summary>
/// Observer that, when a mentor is assigned to an onboarding, copies that mentor's
/// active personal task templates into the mentee's task list (skipping any
/// already present). This keeps a mentor's templates flowing to the people they
/// mentor even when the mentor is assigned after onboarding has started.
/// </summary>
public sealed class MentorAssignedTaskHandler : IDomainEventHandler<MentorAssignedEvent>
{
    private readonly MeridianDbContext _db;

    public MentorAssignedTaskHandler(MeridianDbContext db) => _db = db;

    public async Task HandleAsync(MentorAssignedEvent e, CancellationToken ct = default)
    {
        var templates = await _db.TaskTemplates
            .Where(t => t.IsActive && t.Category == TaskCategory.Personal && t.CreatedById == e.MentorId)
            .ToListAsync(ct);
        if (templates.Count == 0) return;

        var onboarding = await _db.Onboardings
            .Include(o => o.Tasks)
            .FirstOrDefaultAsync(o => o.Id == e.OnboardingId, ct);
        if (onboarding is null) return;

        var alreadyAssigned = onboarding.Tasks
            .Where(t => t.TaskTemplateId != null)
            .Select(t => t.TaskTemplateId!.Value)
            .ToHashSet();

        var toAdd = templates.Where(t => !alreadyAssigned.Contains(t.Id)).ToList();
        if (toAdd.Count == 0) return;

        foreach (var template in toAdd)
        {
            onboarding.Tasks.Add(new EmployeeTask
            {
                TaskTemplateId = template.Id,
                Title = template.Title,
                Description = template.Description,
                Requirements = template.Requirements,
                Category = template.Category,
                Priority = template.Priority,
                Deadline = DateTime.UtcNow.AddDays(template.EstimatedCompletionDays),
                // Credited to the template's author (the mentor themselves).
                AssignedById = template.CreatedById != 0 ? template.CreatedById : e.MentorId,
                Status = EmployeeTaskStatus.NotStarted
            });
        }

        onboarding.RecalculateProgress();
        await _db.SaveChangesAsync(ct);
    }
}
