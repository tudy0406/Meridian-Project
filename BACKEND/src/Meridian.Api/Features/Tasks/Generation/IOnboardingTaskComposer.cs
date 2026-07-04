using Meridian.Api.Features.Tasks.Domain;

namespace Meridian.Api.Features.Tasks.Generation;

/// <summary>
/// Builds the initial set of <see cref="EmployeeTask"/> instances for a new
/// onboarding by running every registered <see cref="ITaskTemplateStrategy"/>
/// and copying the selected templates. Copying (rather than referencing) means
/// later template edits never alter an in-progress onboarding.
/// </summary>
public interface IOnboardingTaskComposer
{
    Task<List<EmployeeTask>> ComposeAsync(OnboardingContext context, int assignedById, CancellationToken ct = default);
}

public sealed class OnboardingTaskComposer : IOnboardingTaskComposer
{
    private readonly IEnumerable<ITaskTemplateStrategy> _strategies;

    public OnboardingTaskComposer(IEnumerable<ITaskTemplateStrategy> strategies) => _strategies = strategies;

    public async Task<List<EmployeeTask>> ComposeAsync(OnboardingContext context, int assignedById, CancellationToken ct = default)
    {
        var tasks = new List<EmployeeTask>();

        foreach (var strategy in _strategies)
        {
            var templates = await strategy.SelectTemplatesAsync(context, ct);
            foreach (var template in templates)
                tasks.Add(MaterializeFromTemplate(template, fallbackAssignedById: assignedById));
        }

        return tasks;
    }

    private static EmployeeTask MaterializeFromTemplate(TaskTemplate template, int fallbackAssignedById) => new()
    {
        TaskTemplateId = template.Id,
        Title = template.Title,
        Description = template.Description,
        Requirements = template.Requirements,
        Category = template.Category,
        Priority = template.Priority,
        Deadline = DateTime.UtcNow.AddDays(template.EstimatedCompletionDays),
        // The task is credited to whoever authored the template (e.g. the mentor,
        // team lead or manager), not to the HR/Admin who created the account.
        AssignedById = template.CreatedById != 0 ? template.CreatedById : fallbackAssignedById,
        Status = Common.Domain.EmployeeTaskStatus.NotStarted
    };
}
