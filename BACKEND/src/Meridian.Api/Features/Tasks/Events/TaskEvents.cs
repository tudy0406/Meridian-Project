using Meridian.Api.Common.Domain.Events;

namespace Meridian.Api.Features.Tasks.Events;

/// <summary>Raised when an onboarding task is assigned to a new employee.</summary>
public sealed record TaskAssignedEvent(
    int EmployeeTaskId,
    int OnboardingEmployeeId,
    string Title) : IDomainEvent;

/// <summary>Raised when a new employee marks an onboarding task as completed.</summary>
public sealed record TaskCompletedEvent(
    int EmployeeTaskId,
    int OnboardingEmployeeId,
    string Title,
    int ProgressPercentage) : IDomainEvent;
