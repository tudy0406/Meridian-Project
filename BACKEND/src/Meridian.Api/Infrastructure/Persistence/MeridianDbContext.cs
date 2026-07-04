using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Domain.Events;
using Meridian.Api.Common.Persistence;
using Meridian.Api.Features.Authentication.Domain;
using Meridian.Api.Features.Departments.Domain;
using Meridian.Api.Features.Documentation.Domain;
using Meridian.Api.Features.Meetings.Domain;
using Meridian.Api.Features.Notifications.Domain;
using Meridian.Api.Features.OnboardingProcess.Domain;
using Meridian.Api.Features.Tasks.Domain;
using Meridian.Api.Features.Teams.Domain;
using Meridian.Api.Features.Users.Domain;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core unit of work over the Meridian relational model. Entity mapping lives
/// in per-entity <c>IEntityTypeConfiguration</c> classes (applied via assembly
/// scan) to keep this class thin and each feature's mapping self-contained.
/// </summary>
public class MeridianDbContext : DbContext, IUnitOfWork
{
    private readonly IDomainEventDispatcher _dispatcher;

    public MeridianDbContext(DbContextOptions<MeridianDbContext> options, IDomainEventDispatcher dispatcher)
        : base(options)
    {
        _dispatcher = dispatcher;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Onboarding> Onboardings => Set<Onboarding>();
    public DbSet<TaskTemplate> TaskTemplates => Set<TaskTemplate>();
    public DbSet<EmployeeTask> EmployeeTasks => Set<EmployeeTask>();
    public DbSet<EmployeeTaskAttachment> EmployeeTaskAttachments => Set<EmployeeTaskAttachment>();
    public DbSet<EmployeeTaskComment> EmployeeTaskComments => Set<EmployeeTaskComment>();
    public DbSet<EmployeeTaskHistory> EmployeeTaskHistory => Set<EmployeeTaskHistory>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<MeetingParticipant> MeetingParticipants => Set<MeetingParticipant>();
    public DbSet<TeamDocumentation> TeamDocumentation => Set<TeamDocumentation>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MeridianDbContext).Assembly);
    }

    /// <summary>
    /// Persists staged changes, then dispatches any domain events the modified
    /// entities raised. Events are collected and cleared before dispatch so that
    /// handlers which themselves persist data (notifications, audit) do not cause
    /// re-entrant dispatch of the same events.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Persist first so that database-generated keys are available to the
        // event handlers, then collect and dispatch. Clearing before dispatch
        // prevents re-entrant handlers (which call SaveChanges again) from
        // re-processing the same events.
        var result = await base.SaveChangesAsync(cancellationToken);

        var entitiesWithEvents = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (entitiesWithEvents.Count == 0) return result;

        var domainEvents = entitiesWithEvents.SelectMany(e => e.DomainEvents).ToList();
        entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

        await _dispatcher.DispatchAsync(domainEvents, cancellationToken);
        return result;
    }
}
