using Meridian.Api.Common.Domain;
using Meridian.Api.Features.OnboardingProcess.Domain;
using Meridian.Api.Features.Tasks.Generation;
using Meridian.Api.Features.Users.Domain;
using Meridian.Api.Infrastructure.Auth;

namespace Meridian.Api.Features.Users.Factory;

public sealed class NewEmployeeFactory : INewEmployeeFactory
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOnboardingTaskComposer _taskComposer;

    public NewEmployeeFactory(IPasswordHasher passwordHasher, IOnboardingTaskComposer taskComposer)
    {
        _passwordHasher = passwordHasher;
        _taskComposer = taskComposer;
    }

    public async Task<User> CreateAsync(NewEmployeeSpec spec, CancellationToken ct = default)
    {
        var user = new User
        {
            FirstName = spec.FirstName,
            LastName = spec.LastName,
            Email = spec.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(spec.PlainPassword),
            PhoneNumber = spec.PhoneNumber,
            JobTitle = spec.JobTitle,
            InOfficeDays = spec.InOfficeDays,
            DepartmentId = spec.DepartmentId,
            TeamId = spec.TeamId,
            IsOnboarding = true,
            IsActive = true
        };

        // Every employee holds at least the Employee role.
        user.UserRoles.Add(new UserRole { Role = spec.EmployeeRole });

        // Kick off the onboarding process (Manager/Team Lead are inherited from
        // the team and therefore not stored on the onboarding itself).
        var onboarding = new Onboarding
        {
            MentorId = spec.MentorId,
            StartDate = DateTime.UtcNow,
            Status = OnboardingStatus.Active,
            Employee = user
        };
        user.Onboarding = onboarding;

        // Materialize the initial task list from the applicable templates
        // (company-wide HR, the employee's department & team, and their mentor's).
        var context = new OnboardingContext(user.Id, spec.DepartmentId, spec.TeamId, spec.MentorId);
        var tasks = await _taskComposer.ComposeAsync(context, spec.CreatedById, ct);
        foreach (var task in tasks)
            onboarding.Tasks.Add(task);

        onboarding.RecalculateProgress();
        return user;
    }
}
