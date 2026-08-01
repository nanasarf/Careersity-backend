using Careersity.Domain.Courses;
using Careersity.Domain.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ConfigureAuditableEntity("Projects");
        builder.ToTable("Projects", table => table.HasCheckConstraint("CK_Projects_EstimatedDurationMinutes_Positive", "\"EstimatedDurationMinutes\" > 0"));
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2_000).IsRequired();
        builder.Property(x => x.Instructions).HasMaxLength(10_000).HasColumnType("text").IsRequired();
        builder.Property(x => x.ExpectedOutput).HasMaxLength(3_000);
        builder.Property(x => x.EvaluationCriteria).HasMaxLength(5_000).HasColumnType("text");
        builder.Property(x => x.SubmissionType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.CourseId);
        builder.HasIndex(x => x.Status);
        builder.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
    }
}
