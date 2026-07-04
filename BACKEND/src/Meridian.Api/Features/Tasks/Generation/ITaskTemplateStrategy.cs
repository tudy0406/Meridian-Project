using Meridian.Api.Features.Tasks.Domain;

namespace Meridian.Api.Features.Tasks.Generation;

/// <summary>
/// Strategy that selects the task templates of one category which apply to a
/// given onboarding. New categories can be supported by adding a strategy,
/// without modifying existing ones (Open/Closed Principle).
/// </summary>
public interface ITaskTemplateStrategy
{
    Common.Domain.TaskCategory Category { get; }
    Task<IReadOnlyList<TaskTemplate>> SelectTemplatesAsync(OnboardingContext context, CancellationToken ct = default);
}
