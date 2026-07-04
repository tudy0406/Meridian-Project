using Meridian.Api.Features.Departments.Domain;
using Meridian.Api.Features.Teams.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meridian.Api.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> b)
    {
        b.ToTable("departments");
        b.HasKey(d => d.Id);
        b.Property(d => d.Name).HasMaxLength(120).IsRequired();
        b.Property(d => d.Description).HasMaxLength(2000);
        b.HasIndex(d => d.Name).IsUnique();

        b.HasMany(d => d.Teams)
            .WithOne(t => t.Department)
            .HasForeignKey(t => t.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> b)
    {
        b.ToTable("teams");
        b.HasKey(t => t.Id);
        b.Property(t => t.Name).HasMaxLength(120).IsRequired();
        b.Property(t => t.Description).HasMaxLength(2000);

        // Manager and Team Lead are optional User references; never cascade so
        // that removing a user reference does not delete the team.
        b.HasOne(t => t.Manager)
            .WithMany()
            .HasForeignKey(t => t.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(t => t.TeamLead)
            .WithMany()
            .HasForeignKey(t => t.TeamLeadId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
