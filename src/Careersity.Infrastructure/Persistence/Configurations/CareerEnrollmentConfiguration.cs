using Careersity.Domain.Careers;
using Careersity.Domain.Enums;
using Careersity.Domain.Identity;
using Careersity.Domain.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class CareerEnrollmentConfiguration : IEntityTypeConfiguration<CareerEnrollment>
{
    public void Configure(EntityTypeBuilder<CareerEnrollment> builder)
    {
        builder.ConfigureAuditableEntity("CareerEnrollments");
        builder.ToTable("CareerEnrollments", table =>
        {
            table.HasCheckConstraint("CK_CareerEnrollments_PausedAfterEnrollment", "\"PausedAtUtc\" IS NULL OR \"PausedAtUtc\" >= \"EnrolledAtUtc\"");
            table.HasCheckConstraint("CK_CareerEnrollments_CompletedAfterEnrollment", "\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"EnrolledAtUtc\"");
            table.HasCheckConstraint("CK_CareerEnrollments_WithdrawnAfterEnrollment", "\"WithdrawnAtUtc\" IS NULL OR \"WithdrawnAtUtc\" >= \"EnrolledAtUtc\"");
        });
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.EnrolledAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.StartedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.PausedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CompletedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.WithdrawnAtUtc).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => x.UserId); builder.HasIndex(x => x.CareerId); builder.HasIndex(x => x.CareerPathwayId); builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.UserId, x.CareerPathwayId });
        builder.HasIndex(x => new { x.UserId, x.CareerPathwayId }).IsUnique().HasFilter("\"Status\" <> 'Withdrawn'")
            .HasDatabaseName("UX_CareerEnrollments_User_Pathway_NonWithdrawn");
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Career>().WithMany().HasForeignKey(x => x.CareerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CareerPathway>().WithMany().HasForeignKey(x => x.CareerPathwayId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.CourseProgressRecords).WithOne().HasForeignKey(x => x.CareerEnrollmentId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.CourseProgressRecords).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
