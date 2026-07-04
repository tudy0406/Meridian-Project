using System.Security.Claims;

namespace Meridian.Api.Infrastructure.Auth;

/// <summary>
/// Ambient accessor for the authenticated principal. Services depend on this
/// abstraction (not on <c>HttpContext</c>) to enforce ownership rules and record
/// who performed an action.
/// </summary>
public interface ICurrentUser
{
    int? UserId { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);

    /// <summary>The authenticated user id, or throws if the request is anonymous.</summary>
    int RequireUserId();
}

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public int? UserId =>
        int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? Array.Empty<string>();

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;

    public int RequireUserId() =>
        UserId ?? throw new Common.Exceptions.UnauthorizedException("No authenticated user.");
}
