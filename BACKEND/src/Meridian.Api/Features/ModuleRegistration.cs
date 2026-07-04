using Meridian.Api.Features.Authentication;
using Meridian.Api.Features.Departments;
using Meridian.Api.Features.Documentation;
using Meridian.Api.Features.Meetings;
using Meridian.Api.Features.Notifications;
using Meridian.Api.Features.OnboardingProcess;
using Meridian.Api.Features.Tasks;
using Meridian.Api.Features.Teams;
using Meridian.Api.Features.Users;

namespace Meridian.Api.Features;

/// <summary>
/// Aggregates every feature module's registration. Each module owns its own
/// wiring, so adding or extracting a module (towards microservices, if the
/// company grows) is a localized change.
/// </summary>
public static class ModuleRegistration
{
    public static IServiceCollection AddFeatureModules(this IServiceCollection services)
    {
        services.AddAuthenticationFeature();
        services.AddUsersFeature();
        services.AddDepartmentsFeature();
        services.AddTeamsFeature();
        services.AddOnboardingFeature();
        services.AddTasksFeature();
        services.AddMeetingsFeature();
        services.AddDocumentationFeature();
        services.AddNotificationsFeature();
        return services;
    }
}
