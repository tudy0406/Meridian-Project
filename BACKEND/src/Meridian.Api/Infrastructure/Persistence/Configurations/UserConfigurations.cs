using Meridian.Api.Features.OnboardingProcess.Domain;
using Meridian.Api.Features.Users.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meridian.Api.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(u => u.Id);
        b.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        b.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        b.Property(u => u.Email).HasMaxLength(256).IsRequired();
        b.HasIndex(u => u.Email).IsUnique();
        b.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
        b.Property(u => u.PhoneNumber).HasMaxLength(32);
        b.Property(u => u.JobTitle).HasMaxLength(150);
        b.Property(u => u.InOfficeDays).HasMaxLength(64);

        b.HasOne(u => u.Department)
            .WithMany()
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(u => u.Team)
            .WithMany(t => t.Members)
            .HasForeignKey(u => u.TeamId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(u => u.Onboarding)
            .WithOne(o => o.Employee)
            .HasForeignKey<Onboarding>(o => o.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("roles");
        b.HasKey(r => r.Id);
        b.Property(r => r.Name).HasMaxLength(50).IsRequired();
        b.HasIndex(r => r.Name).IsUnique();
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.ToTable("user_roles");
        b.HasKey(ur => new { ur.UserId, ur.RoleId });

        b.HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
