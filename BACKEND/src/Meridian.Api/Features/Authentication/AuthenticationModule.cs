namespace Meridian.Api.Features.Authentication;

/// <summary>
/// DI registration for the Authentication module. Each feature owns its own
/// registration extension so the modular monolith wires up as a set of
/// independent, self-describing modules.
/// </summary>
public static class AuthenticationModule
{
    public static IServiceCollection AddAuthenticationFeature(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
