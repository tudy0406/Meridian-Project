using Meridian.Api.Common.Domain;
using Meridian.Api.Features.Users.Domain;

namespace Meridian.Api.Features.Authentication.Domain;

/// <summary>
/// A single-use, time-limited token backing the "reset password via email"
/// flow. Storing a token (rather than anything derived from the password) lets
/// users recover access without ever exposing credentials.
/// </summary>
public class PasswordResetToken : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Token { get; set; } = string.Empty;
    public DateTime ExpirationDate { get; set; }
    public bool Used { get; set; }

    public bool IsValid => !Used && ExpirationDate > DateTime.UtcNow;
}
