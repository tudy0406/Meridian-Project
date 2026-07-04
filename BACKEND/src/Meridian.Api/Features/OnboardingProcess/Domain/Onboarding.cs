using Meridian.Api.Common.Domain;
using Meridian.Api.Features.Tasks.Domain;
using Meridian.Api.Features.Users.Domain;

namespace Meridian.Api.Features.OnboardingProcess.Domain;

/// <summary>
/// A single onboarding process for one new employee. Kept separate from
/// <see cref="User"/> so that transient onboarding data stays independent from
/// permanent employee information and can be archived/cancelled on its own.
/// </summary>
public class Onboarding : BaseEntity
{
    public int EmployeeId { get; set; }
    public User Employee { get; set; } = null!;

    public int? MentorId { get; set; }
    public User? Mentor { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public OnboardingStatus Status { get; set; } = OnboardingStatus.Pending;

    /// <summary>Denormalized 0–100 progress, recomputed from task completion.</summary>
    public int ProgressPercentage { get; set; }

    public ICollection<EmployeeTask> Tasks { get; set; } = new List<EmployeeTask>();

    /// <summary>Recalculates progress from the current set of tasks.</summary>
    public void RecalculateProgress()
    {
        var total = Tasks.Count;
        if (total == 0)
        {
            ProgressPercentage = 0;
            return;
        }

        var completed = Tasks.Count(t => t.Status == EmployeeTaskStatus.Completed);
        ProgressPercentage = (int)Math.Round(completed * 100.0 / total);

        if (ProgressPercentage == 100 && Status == OnboardingStatus.Active)
        {
            Status = OnboardingStatus.Completed;
            EndDate = DateTime.UtcNow;
        }
    }
}
