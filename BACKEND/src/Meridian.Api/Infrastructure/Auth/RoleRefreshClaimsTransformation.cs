using System.Security.Claims;
using Meridian.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Infrastructure.Auth;

/// <summary>
/// Rebuilds the role claims of every authenticated request from the database
/// rather than trusting the (potentially stale) roles embedded in the JWT. This
/// makes role changes — e.g. an Administrator revoking someone's Team Lead role —
/// take effect on the user's very next request, without waiting for the token to
/// expire or forcing a re-login.
/// </summary>
public sealed class RoleRefreshClaimsTransformation : IClaimsTransformation
{
    private readonly MeridianDbContext _db;

    public RoleRefreshClaimsTransformation(MeridianDbContext db) => _db = db;

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return principal;

        if (!int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return principal;

        var currentRoles = await _db.Users
            .Where(u => u.Id == userId && u.IsActive)
            .SelectMany(u => u.UserRoles)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        // Rebuild the identity with all non-role claims plus the current roles, so
        // repeated invocations are idempotent and stale roles never linger.
        var roleClaimType = identity.RoleClaimType;
        var claims = identity.Claims.Where(c => c.Type != roleClaimType).ToList();
        claims.AddRange(currentRoles.Select(r => new Claim(roleClaimType, r)));

        var refreshed = new ClaimsIdentity(claims, identity.AuthenticationType, identity.NameClaimType, roleClaimType);
        return new ClaimsPrincipal(refreshed);
    }
}
