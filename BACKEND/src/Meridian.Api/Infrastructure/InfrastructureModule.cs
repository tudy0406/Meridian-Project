using Meridian.Api.Common.Domain.Events;
using Meridian.Api.Common.Persistence;
using Meridian.Api.Infrastructure.Audit;
using Meridian.Api.Infrastructure.Auth;
using Meridian.Api.Infrastructure.Email;
using Meridian.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Infrastructure;

/// <summary>Registers cross-cutting infrastructure: persistence, auth, events, email, audit.</summary>
public static class InfrastructureModule
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MeridianDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Meridian")));

        // Generic Repository + Unit of Work over the same DbContext instance.
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MeridianDbContext>());

        // Observer infrastructure.
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Auth & security primitives.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Refresh roles from the DB each request so role changes apply immediately.
        services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation, RoleRefreshClaimsTransformation>();

        // Email & audit.
        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddScoped<IAuditLogger, AuditLogger>();

        return services;
    }
}
