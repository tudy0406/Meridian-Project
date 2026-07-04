using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Web;
using Meridian.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meridian.Api.Features.OnboardingProcess;

[Authorize]
[Route("api/onboarding")]
public sealed class OnboardingController : ApiControllerBase
{
    private readonly IOnboardingService _onboarding;
    private readonly ICurrentUser _currentUser;

    public OnboardingController(IOnboardingService onboarding, ICurrentUser currentUser)
    {
        _onboarding = onboarding;
        _currentUser = currentUser;
    }

    /// <summary>The authenticated new employee's own onboarding summary and progress.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<OnboardingSummaryDto>> Mine(CancellationToken ct) =>
        Ok(await _onboarding.GetForEmployeeAsync(_currentUser.RequireUserId(), ct));

    /// <summary>Onboarding summary for a specific employee (subject to visibility rules).</summary>
    [HttpGet("employee/{employeeId:int}")]
    public async Task<ActionResult<OnboardingSummaryDto>> ForEmployee(int employeeId, CancellationToken ct) =>
        Ok(await _onboarding.GetForEmployeeAsync(employeeId, ct));

    /// <summary>All onboardings the caller is allowed to monitor.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OnboardingSummaryDto>>> List(CancellationToken ct) =>
        Ok(await _onboarding.ListVisibleAsync(ct));

    /// <summary>Assign or change the mentor for an employee (Team Lead of the team, or HR).</summary>
    [HttpPut("employee/{employeeId:int}/mentor/{mentorId:int}")]
    [Authorize(Roles = $"{RoleNames.TeamLead},{RoleNames.HrEmployee},{RoleNames.Administrator}")]
    public async Task<IActionResult> AssignMentor(int employeeId, int mentorId, CancellationToken ct)
    {
        await _onboarding.AssignMentorAsync(employeeId, mentorId, ct);
        return NoContent();
    }
}

public static class OnboardingModule
{
    public static IServiceCollection AddOnboardingFeature(this IServiceCollection services)
    {
        services.AddScoped<IOnboardingService, OnboardingService>();
        services.AddScoped<IOnboardingAudienceResolver, OnboardingAudienceResolver>();
        services.AddHostedService<OnboardingAutoCompletionService>();
        return services;
    }
}
