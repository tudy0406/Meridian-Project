using Meridian.Api.Features.Users.Administration;
using Meridian.Api.Features.Users.Factory;

namespace Meridian.Api.Features.Users;

public static class UsersModule
{
    public static IServiceCollection AddUsersFeature(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<INewEmployeeFactory, NewEmployeeFactory>();
        services.AddScoped<IStaffAdminService, StaffAdminService>();
        services.AddScoped<IOrgRoleReconciler, OrgRoleReconciler>();
        return services;
    }
}
