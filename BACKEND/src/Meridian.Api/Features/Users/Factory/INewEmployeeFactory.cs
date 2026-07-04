using Meridian.Api.Features.Users.Domain;

namespace Meridian.Api.Features.Users.Factory;

/// <summary>Fully-resolved inputs required to build a new employee aggregate.</summary>
public sealed record NewEmployeeSpec(
    string FirstName,
    string LastName,
    string Email,
    string PlainPassword,
    string? PhoneNumber,
    string? JobTitle,
    string? InOfficeDays,
    int DepartmentId,
    int TeamId,
    int? MentorId,
    Role EmployeeRole,
    int CreatedById);

/// <summary>
/// Encapsulates the multi-step construction of a new employee: the user account,
/// its Employee role, the onboarding process, and the initial set of tasks
/// generated from templates. Centralizing this (Factory pattern) keeps the HR
/// service simple and guarantees every new employee is created consistently.
/// </summary>
public interface INewEmployeeFactory
{
    Task<User> CreateAsync(NewEmployeeSpec spec, CancellationToken ct = default);
}
