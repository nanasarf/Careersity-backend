using Careersity.Domain.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class CoursePrerequisiteConfiguration : IEntityTypeConfiguration<CoursePrerequisite>
{
    public void Configure(EntityTypeBuilder<CoursePrerequisite> builder)
    {
        builder.ConfigureAuditableEntity("CoursePrerequisites");
        builder.ToTable("CoursePrerequisites", table => table.HasCheckConstraint(
            "CK_CoursePrerequisites_NoSelfReference", "\"CourseId\" <> \"PrerequisiteCourseId\""));
        builder.HasIndex(x => new { x.CourseId, x.PrerequisiteCourseId }).IsUnique();
        builder.HasIndex(x => x.PrerequisiteCourseId);
        builder.HasOne<Course>().WithMany().HasForeignKey(x => x.PrerequisiteCourseId).OnDelete(DeleteBehavior.Restrict);
    }
}
