using Careersity.Domain.Courses;
using Careersity.Domain.LearningResources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class CourseExternalResourceConfiguration : IEntityTypeConfiguration<CourseExternalResource>
{
    public void Configure(EntityTypeBuilder<CourseExternalResource> b)
    {
        b.ConfigureAuditableEntity("CourseExternalResources"); b.ToTable("CourseExternalResources", t => t.HasCheckConstraint("CK_CourseExternalResources_Order_Nonnegative", "\"Order\" >= 0"));
        b.Property(x => x.Notes).HasMaxLength(2000); b.HasIndex(x => new { x.CourseId, x.ExternalLearningResourceId }).IsUnique(); b.HasIndex(x => new { x.CourseId, x.Order }).IsUnique(); b.HasIndex(x => x.ExternalLearningResourceId);
        b.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade); b.HasOne<ExternalLearningResource>().WithMany().HasForeignKey(x => x.ExternalLearningResourceId).OnDelete(DeleteBehavior.Restrict);
    }
}
