using Meridian.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Features.OnboardingProcess;

/// <summary>
/// Resolves the set of users who are allowed to see (and be notified about) a
/// new employee's onboarding tasks and progress: the employee, their mentor,
/// the team's Team Lead and Manager. Centralizing this (GRASP: Information
/// Expert) keeps the rule in one place for both authorization and notifications.
/// </summary>
public interface IOnboardingAudienceResolver
{
    Task<IReadOnlyCollection<int>> ResolveAsync(int onboardingEmployeeId, CancellationToken ct = default);
}

public sealed class OnboardingAudienceResolver : IOnboardingAudienceResolver
{
    private readonly MeridianDbContext _db;

    public OnboardingAudienceResolver(MeridianDbContext db) => _db = db;

    public async Task<IReadOnlyCollection<int>> ResolveAsync(int onboardingEmployeeId, CancellationToken ct = default)
    {
        var audience = new HashSet<int> { onboardingEmployeeId };

        var employee = await _db.Users
            .Include(u => u.Team)
            .Include(u => u.Onboarding)
            .FirstOrDefaultAsync(u => u.Id == onboardingEmployeeId, ct);

        if (employee is null) return audience;

        if (employee.Onboarding?.MentorId is int mentorId)
            audience.Add(mentorId);

        if (employee.Team is { } team)
        {
            if (team.TeamLeadId is int leadId) audience.Add(leadId);
            if (team.ManagerId is int managerId) audience.Add(managerId);
        }

        return audience;
    }
}
