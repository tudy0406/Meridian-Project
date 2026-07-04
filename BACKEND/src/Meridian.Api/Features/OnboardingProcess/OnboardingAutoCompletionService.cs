using Meridian.Api.Common.Domain;
using Meridian.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Features.OnboardingProcess;

/// <summary>
/// Background worker that automatically completes onboarding processes that have
/// been running longer than a configured duration. This closes out onboardings
/// that were never explicitly finished (independent of task completion).
///
/// Configuration ("Onboarding" section):
///   AutoCompleteAfterDays        – age at which an active onboarding is completed (default 90)
///   AutoCompleteCheckIntervalHours – how often to scan (default 6); 0 disables the worker
/// </summary>
public sealed class OnboardingAutoCompletionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OnboardingAutoCompletionService> _logger;

    public OnboardingAutoCompletionService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<OnboardingAutoCompletionService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = _configuration.GetValue("Onboarding:AutoCompleteCheckIntervalHours", 6);
        if (intervalHours <= 0)
        {
            _logger.LogInformation("Onboarding auto-completion is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(intervalHours));
        do
        {
            try
            {
                await CompleteExpiredOnboardingsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Onboarding auto-completion cycle failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CompleteExpiredOnboardingsAsync(CancellationToken ct)
    {
        var days = _configuration.GetValue("Onboarding:AutoCompleteAfterDays", 90);
        var cutoff = DateTime.UtcNow.AddDays(-days);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MeridianDbContext>();

        var expired = await db.Onboardings
            .Where(o => o.Status == OnboardingStatus.Active && o.StartDate <= cutoff)
            .ToListAsync(ct);

        if (expired.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var onboarding in expired)
        {
            onboarding.Status = OnboardingStatus.Completed;
            onboarding.EndDate = now;
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Auto-completed {Count} onboarding(s) older than {Days} days.", expired.Count, days);
    }
}
