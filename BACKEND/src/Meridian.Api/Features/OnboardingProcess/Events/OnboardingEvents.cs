using Meridian.Api.Common.Domain.Events;

namespace Meridian.Api.Features.OnboardingProcess.Events;

/// <summary>
/// Raised when a mentor is assigned to (or changed for) an ongoing onboarding.
/// Observed by the Tasks feature to auto-assign the new mentor's personal task
/// templates to the mentee.
/// </summary>
public sealed record MentorAssignedEvent(
    int OnboardingId,
    int EmployeeId,
    int MentorId) : IDomainEvent;
