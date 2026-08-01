using Careersity.Domain.Assessments;
using Careersity.Domain.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.ConfigureAuditableEntity("Assessments");
        builder.ToTable("Assessments", table =>
        {
            table.HasCheckConstraint("CK_Assessments_PassingScorePercentage_Range", "\"PassingScorePercentage\" BETWEEN 1 AND 100");
            table.HasCheckConstraint("CK_Assessments_MaximumAttempts_Positive", "\"MaximumAttempts\" IS NULL OR \"MaximumAttempts\" > 0");
        });
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2_000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.CourseId);
        builder.HasIndex(x => x.Status);
        builder.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Questions).WithOne().HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Questions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
