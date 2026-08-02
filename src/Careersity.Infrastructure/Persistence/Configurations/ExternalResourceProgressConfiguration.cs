using Careersity.Domain.Learning;
using Careersity.Domain.LearningResources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class ExternalResourceProgressConfiguration : IEntityTypeConfiguration<ExternalResourceProgress>
{
    public void Configure(EntityTypeBuilder<ExternalResourceProgress> b)
    {
        b.ConfigureAuditableEntity("ExternalResourceProgressRecords");
        b.ToTable("ExternalResourceProgressRecords", t => t.HasCheckConstraint("CK_ExternalResourceProgress_CompletedAfterStarted", "\"CompletedAtUtc\" IS NULL OR \"CompletedAtUtc\" >= \"StartedAtUtc\""));
        b.Property(x => x.StartedAtUtc).HasColumnType("timestamp with time zone").IsRequired(); b.Property(x => x.CompletedAtUtc).HasColumnType("timestamp with time zone"); b.Property(x => x.LastAccessedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        b.HasIndex(x => new { x.CourseProgressId, x.CourseExternalResourceId }).IsUnique(); b.HasIndex(x => x.CourseExternalResourceId);
        b.HasOne<CourseProgress>().WithMany(x => x.ExternalResourceProgressRecords).HasForeignKey(x => x.CourseProgressId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<CourseExternalResource>().WithMany().HasForeignKey(x => x.CourseExternalResourceId).OnDelete(DeleteBehavior.Restrict);
    }
}
