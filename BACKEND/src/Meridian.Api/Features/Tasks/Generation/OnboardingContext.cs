namespace Meridian.Api.Features.Tasks.Generation;

/// <summary>
/// Immutable inputs a task-generation strategy needs to decide which templates
/// apply to a new employee.
/// </summary>
public sealed record OnboardingContext(
    int EmployeeId,
    int? DepartmentId,
    int? TeamId,
    int? MentorId);
