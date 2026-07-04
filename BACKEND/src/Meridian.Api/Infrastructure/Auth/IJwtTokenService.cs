using Meridian.Api.Features.Users.Domain;

namespace Meridian.Api.Infrastructure.Auth;

public sealed record AccessToken(string Token, DateTime ExpiresAt);

/// <summary>Issues signed JWTs carrying the user's identity and roles.</summary>
public interface IJwtTokenService
{
    AccessToken CreateToken(User user, IEnumerable<string> roles);
}
