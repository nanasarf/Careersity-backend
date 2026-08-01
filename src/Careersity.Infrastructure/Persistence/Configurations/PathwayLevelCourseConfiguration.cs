using Careersity.Domain.Careers;
using Careersity.Domain.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class PathwayLevelCourseConfiguration : IEntityTypeConfiguration<PathwayLevelCourse>
{
    public void Configure(EntityTypeBuilder<PathwayLevelCourse> builder)
    {
        builder.ConfigureAuditableEntity("PathwayLevelCourses");
        builder.ToTable("PathwayLevelCourses", table => table.HasCheckConstraint("CK_PathwayLevelCourses_Order_Nonnegative", "\"Order\" >= 0"));
        builder.HasIndex(x => new { x.PathwayLevelId, x.CourseId }).IsUnique();
        builder.HasIndex(x => new { x.PathwayLevelId, x.Order }).IsUnique();
        builder.HasIndex(x => x.CourseId);
        builder.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
    }
}
