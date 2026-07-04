using System.ComponentModel.DataAnnotations;

namespace Meridian.Api.Features.Authentication;

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    int UserId,
    string FullName,
    string Email,
    IReadOnlyCollection<string> Roles,
    bool IsOnboarding);

public sealed record ForgotPasswordRequest(
    [Required, EmailAddress] string Email);

public sealed record ResetPasswordRequest(
    [Required] string Token,
    [Required] string NewPassword);

public sealed record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required] string NewPassword);
