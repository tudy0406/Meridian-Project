using System.ComponentModel.DataAnnotations;

namespace Meridian.Api.Features.Users;

public sealed record CreateEmployeeRequest(
    [Required, StringLength(100)] string FirstName,
    [Required, StringLength(100)] string LastName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Phone, StringLength(32)] string? PhoneNumber,
    [StringLength(150)] string? JobTitle,
    [StringLength(64)] string? InOfficeDays,
    [Required] int DepartmentId,
    [Required] int TeamId,
    int? MentorId);

/// <summary>Returned once on creation; includes the generated temporary password.</summary>
public sealed record CreateEmployeeResponse(int UserId, string Email, string TemporaryPassword);

public sealed record UpdateProfileRequest(
    [Phone, StringLength(32)] string? PhoneNumber,
    [StringLength(150)] string? JobTitle,
    [StringLength(64)] string? InOfficeDays);

public sealed record UserProfileDto(
    int Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? JobTitle,
    int? DepartmentId,
    string? DepartmentName,
    int? TeamId,
    string? TeamName,
    string? InOfficeDays,
    bool IsOnboarding,
    string? OnboardingStatus,
    bool CanAccessOnboarding,
    IReadOnlyCollection<string> Roles);

public sealed record UserSummaryDto(
    int Id,
    string FullName,
    string Email,
    string? JobTitle,
    int? DepartmentId,
    int? TeamId,
    IReadOnlyCollection<string> Roles);
