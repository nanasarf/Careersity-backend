using Careersity.Domain.Assessments;
using Careersity.Domain.Identity;
using Careersity.Domain.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class AssessmentAttemptConfiguration : IEntityTypeConfiguration<AssessmentAttempt>
{
    public void Configure(EntityTypeBuilder<AssessmentAttempt> builder)
    {
        builder.ConfigureAuditableEntity("AssessmentAttempts");
        builder.ToTable("AssessmentAttempts", t =>
        {
            t.HasCheckConstraint("CK_AssessmentAttempts_AttemptNumber_Positive", "\"AttemptNumber\" > 0");
            t.HasCheckConstraint("CK_AssessmentAttempts_Score_Range", "\"ScorePercentage\" IS NULL OR (\"ScorePercentage\" BETWEEN 0 AND 100)");
            t.HasCheckConstraint("CK_AssessmentAttempts_PointsEarned_Nonnegative", "\"PointsEarned\" IS NULL OR \"PointsEarned\" >= 0");
            t.HasCheckConstraint("CK_AssessmentAttempts_TotalPoints_Positive", "\"TotalPoints\" IS NULL OR \"TotalPoints\" > 0");
            t.HasCheckConstraint("CK_AssessmentAttempts_SubmittedAfterStarted", "\"SubmittedAtUtc\" IS NULL OR \"SubmittedAtUtc\" >= \"StartedAtUtc\"");
            t.HasCheckConstraint("CK_AssessmentAttempts_PassedAfterStarted", "\"PassedAtUtc\" IS NULL OR \"PassedAtUtc\" >= \"StartedAtUtc\"");
        });
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ScorePercentage).HasPrecision(5, 2);
        builder.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();
        builder.Property(x => x.StartedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.SubmittedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.PassedAtUtc).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => x.UserId); builder.HasIndex(x => x.CareerEnrollmentId); builder.HasIndex(x => x.CourseProgressId); builder.HasIndex(x => x.AssessmentId);
        builder.HasIndex(x => new { x.CareerEnrollmentId, x.AssessmentId, x.AttemptNumber }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.AssessmentId });
        builder.HasIndex(x => new { x.CareerEnrollmentId, x.AssessmentId }).IsUnique().HasFilter("\"Status\" = 'InProgress'").HasDatabaseName("UX_AssessmentAttempts_Enrollment_Assessment_InProgress");
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CareerEnrollment>().WithMany().HasForeignKey(x => x.CareerEnrollmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CourseProgress>().WithMany().HasForeignKey(x => x.CourseProgressId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Assessment>().WithMany().HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Responses).WithOne().HasForeignKey(x => x.AssessmentAttemptId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Responses).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
