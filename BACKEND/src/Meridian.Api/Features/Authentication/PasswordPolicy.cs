using Meridian.Api.Common.Exceptions;

namespace Meridian.Api.Features.Authentication;

/// <summary>Enforces the application's strong-password rules (server-authoritative).</summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;

    public static void Validate(string password)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinLength)
            errors.Add($"Password must be at least {MinLength} characters long.");
        if (!password.Any(char.IsUpper)) errors.Add("Password must contain an uppercase letter.");
        if (!password.Any(char.IsLower)) errors.Add("Password must contain a lowercase letter.");
        if (!password.Any(char.IsDigit)) errors.Add("Password must contain a digit.");

        if (errors.Count > 0)
            throw new ValidationException(new Dictionary<string, string[]> { ["password"] = errors.ToArray() });
    }
}
