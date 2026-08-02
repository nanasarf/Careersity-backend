using Careersity.Domain.Courses;
using Careersity.Domain.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class CourseProgressConfiguration : IEntityTypeConfiguration<CourseProgress>
{
    public void Configure(EntityTypeBuilder<CourseProgress> builder)
    {
        builder.ConfigureAuditableEntity("CourseProgressRecords");
        builder.ToTable("CourseProgressRecords", table => table.HasCheckConstraint("CK_CourseProgress_CompletedAfterStarted", "\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"StartedAtUtc\""));
        builder.Property(x => x.StartedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CompletedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.LastAccessedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(x => new { x.CareerEnrollmentId, x.CourseId }).IsUnique(); builder.HasIndex(x => x.CourseId);
        builder.HasOne<CareerEnrollment>().WithMany(x => x.CourseProgressRecords).HasForeignKey(x => x.CareerEnrollmentId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.LessonProgressRecords).WithOne().HasForeignKey(x => x.CourseProgressId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.LessonProgressRecords).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
