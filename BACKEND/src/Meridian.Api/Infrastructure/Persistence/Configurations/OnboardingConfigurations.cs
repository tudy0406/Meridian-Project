using Meridian.Api.Features.OnboardingProcess.Domain;
using Meridian.Api.Features.Tasks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meridian.Api.Infrastructure.Persistence.Configurations;

public class OnboardingConfiguration : IEntityTypeConfiguration<Onboarding>
{
    public void Configure(EntityTypeBuilder<Onboarding> b)
    {
        b.ToTable("onboardings");
        b.HasKey(o => o.Id);
        b.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(o => o.EmployeeId).IsUnique();

        b.HasOne(o => o.Mentor)
            .WithMany()
            .HasForeignKey(o => o.MentorId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(o => o.Tasks)
            .WithOne(t => t.Onboarding)
            .HasForeignKey(t => t.OnboardingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TaskTemplateConfiguration : IEntityTypeConfiguration<TaskTemplate>
{
    public void Configure(EntityTypeBuilder<TaskTemplate> b)
    {
        b.ToTable("task_templates");
        b.HasKey(t => t.Id);
        b.Property(t => t.Title).HasMaxLength(200).IsRequired();
        b.Property(t => t.Description).HasMaxLength(4000);
        b.Property(t => t.Requirements).HasMaxLength(4000);
        b.Property(t => t.Category).HasConversion<string>().HasMaxLength(20);
        b.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);
    }
}

public class EmployeeTaskConfiguration : IEntityTypeConfiguration<EmployeeTask>
{
    public void Configure(EntityTypeBuilder<EmployeeTask> b)
    {
        b.ToTable("employee_tasks");
        b.HasKey(t => t.Id);
        b.Property(t => t.Title).HasMaxLength(200).IsRequired();
        b.Property(t => t.Description).HasMaxLength(4000);
        b.Property(t => t.Requirements).HasMaxLength(4000);
        b.Property(t => t.Category).HasConversion<string>().HasMaxLength(20);
        b.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(t => t.Priority).HasConversion<string>().HasMaxLength(20);

        b.HasOne(t => t.AssignedBy)
            .WithMany()
            .HasForeignKey(t => t.AssignedById)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(t => t.ContactPerson)
            .WithMany()
            .HasForeignKey(t => t.ContactPersonId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasMany(t => t.Attachments)
            .WithOne(a => a.EmployeeTask)
            .HasForeignKey(a => a.EmployeeTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(t => t.Comments)
            .WithOne(c => c.EmployeeTask)
            .HasForeignKey(c => c.EmployeeTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(t => t.History)
            .WithOne(h => h.EmployeeTask)
            .HasForeignKey(h => h.EmployeeTaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class EmployeeTaskAttachmentConfiguration : IEntityTypeConfiguration<EmployeeTaskAttachment>
{
    public void Configure(EntityTypeBuilder<EmployeeTaskAttachment> b)
    {
        b.ToTable("employee_task_attachments");
        b.HasKey(a => a.Id);
        b.Property(a => a.FileName).HasMaxLength(255).IsRequired();
        b.Property(a => a.Url).HasMaxLength(1000).IsRequired();
    }
}

public class EmployeeTaskCommentConfiguration : IEntityTypeConfiguration<EmployeeTaskComment>
{
    public void Configure(EntityTypeBuilder<EmployeeTaskComment> b)
    {
        b.ToTable("employee_task_comments");
        b.HasKey(c => c.Id);
        b.Property(c => c.Text).HasMaxLength(2000).IsRequired();

        b.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeTaskHistoryConfiguration : IEntityTypeConfiguration<EmployeeTaskHistory>
{
    public void Configure(EntityTypeBuilder<EmployeeTaskHistory> b)
    {
        b.ToTable("employee_task_history");
        b.HasKey(h => h.Id);
        b.Property(h => h.Status).HasConversion<string>().HasMaxLength(20);

        b.HasOne(h => h.ChangedBy)
            .WithMany()
            .HasForeignKey(h => h.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
