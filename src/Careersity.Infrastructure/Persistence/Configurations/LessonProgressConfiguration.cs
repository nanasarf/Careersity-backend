using Careersity.Domain.Courses;
using Careersity.Domain.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class LessonProgressConfiguration : IEntityTypeConfiguration<LessonProgress>
{
    public void Configure(EntityTypeBuilder<LessonProgress> builder)
    {
        builder.ConfigureAuditableEntity("LessonProgressRecords");
        builder.ToTable("LessonProgressRecords", table => table.HasCheckConstraint("CK_LessonProgress_CompletedAfterStarted", "\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"StartedAtUtc\""));
        builder.Property(x => x.StartedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.CompletedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.LastAccessedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(x => new { x.CourseProgressId, x.LessonId }).IsUnique(); builder.HasIndex(x => x.LessonId);
        builder.HasOne<CourseProgress>().WithMany(x => x.LessonProgressRecords).HasForeignKey(x => x.CourseProgressId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Lesson>().WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Restrict);
    }
}
