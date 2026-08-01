using Careersity.Domain.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ConfigureAuditableEntity("Courses");
        builder.ToTable("Courses", table => table.HasCheckConstraint("CK_Courses_EstimatedDurationMinutes_Positive", "\"EstimatedDurationMinutes\" > 0"));
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(220).IsRequired();
        builder.Property(x => x.ShortDescription).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DetailedDescription).HasMaxLength(5_000);
        builder.Property(x => x.Difficulty).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.Difficulty);
        builder.HasIndex(x => x.Status);
        builder.HasMany(x => x.Lessons).WithOne().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Prerequisites).WithOne().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.CourseSkills).WithOne().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Lessons).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Prerequisites).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.CourseSkills).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
