using Careersity.Domain.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ConfigureAuditableEntity("Lessons");
        builder.ToTable("Lessons", table =>
        {
            table.HasCheckConstraint("CK_Lessons_EstimatedDurationMinutes_Positive", "\"EstimatedDurationMinutes\" > 0");
            table.HasCheckConstraint("CK_Lessons_Order_Nonnegative", "\"Order\" >= 0");
        });
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(220).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(1_000);
        builder.Property(x => x.Content).HasMaxLength(50_000).HasColumnType("text");
        builder.Property(x => x.ContentType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ExternalResourceUrl).HasMaxLength(2_000);
        builder.HasIndex(x => new { x.CourseId, x.Order }).IsUnique();
        builder.HasIndex(x => new { x.CourseId, x.Slug }).IsUnique();
        builder.HasIndex(x => x.CourseId);
    }
}
