using Meridian.Api.Common.Domain;
using Meridian.Api.Features.Authentication.Domain;
using Meridian.Api.Features.Documentation.Domain;
using Meridian.Api.Features.Meetings.Domain;
using Meridian.Api.Features.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meridian.Api.Infrastructure.Persistence.Configurations;

public class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> b)
    {
        b.ToTable("meetings");
        b.HasKey(m => m.Id);
        b.Property(m => m.Title).HasMaxLength(200).IsRequired();
        b.Property(m => m.Description).HasMaxLength(4000);
        b.Property(m => m.Location).HasMaxLength(300);
        b.Property(m => m.OnlineLink).HasMaxLength(500);

        b.HasOne(m => m.Organizer)
            .WithMany()
            .HasForeignKey(m => m.OrganizerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MeetingParticipantConfiguration : IEntityTypeConfiguration<MeetingParticipant>
{
    public void Configure(EntityTypeBuilder<MeetingParticipant> b)
    {
        b.ToTable("meeting_participants");
        b.HasKey(mp => new { mp.MeetingId, mp.UserId });

        b.HasOne(mp => mp.Meeting)
            .WithMany(m => m.Participants)
            .HasForeignKey(mp => mp.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(mp => mp.User)
            .WithMany()
            .HasForeignKey(mp => mp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TeamDocumentationConfiguration : IEntityTypeConfiguration<TeamDocumentation>
{
    public void Configure(EntityTypeBuilder<TeamDocumentation> b)
    {
        b.ToTable("team_documentation");
        b.HasKey(d => d.Id);
        b.Property(d => d.Title).HasMaxLength(200).IsRequired();
        b.Property(d => d.Category).HasConversion<string>().HasMaxLength(30);
        b.Property(d => d.Content).IsRequired();

        b.HasOne(d => d.Team)
            .WithMany(t => t.Documentation)
            .HasForeignKey(d => d.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("notifications");
        b.HasKey(n => n.Id);
        b.Property(n => n.Title).HasMaxLength(200).IsRequired();
        b.Property(n => n.Message).HasMaxLength(1000).IsRequired();
        b.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
        b.HasIndex(n => new { n.UserId, n.IsRead });

        b.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(a => a.Id);
        b.Property(a => a.Action).HasMaxLength(100).IsRequired();
        b.Property(a => a.EntityName).HasMaxLength(100).IsRequired();
        b.Property(a => a.EntityId).HasMaxLength(64);
        b.HasIndex(a => a.Timestamp);
    }
}

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> b)
    {
        b.ToTable("password_reset_tokens");
        b.HasKey(p => p.Id);
        b.Property(p => p.Token).HasMaxLength(200).IsRequired();
        b.HasIndex(p => p.Token);

        b.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
