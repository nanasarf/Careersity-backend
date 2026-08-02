using Careersity.Domain.LearningResources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class ExternalLearningResourceConfiguration : IEntityTypeConfiguration<ExternalLearningResource>
{
    public void Configure(EntityTypeBuilder<ExternalLearningResource> b)
    {
        b.ConfigureAuditableEntity("ExternalLearningResources");
        b.ToTable("ExternalLearningResources", t => t.HasCheckConstraint("CK_ExternalLearningResources_Duration_Positive", "\"EstimatedDurationMinutes\" IS NULL OR \"EstimatedDurationMinutes\" > 0"));
        b.Property(x => x.Title).HasMaxLength(300).IsRequired(); b.Property(x => x.Description).HasMaxLength(5000); b.Property(x => x.Url).HasMaxLength(2000).IsRequired();
        b.Property(x => x.SourceLabel).HasMaxLength(300); b.Property(x => x.ResourceType).HasConversion<string>().HasMaxLength(40).IsRequired(); b.Property(x => x.AccessType).HasConversion<string>().HasMaxLength(30).IsRequired(); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.LastReviewedAtUtc).HasColumnType("timestamp with time zone");
        b.HasIndex(x => x.LearningProviderId); b.HasIndex(x => x.InstructorId); b.HasIndex(x => x.ResourceType); b.HasIndex(x => x.AccessType); b.HasIndex(x => x.Status);
        b.HasOne<LearningProvider>().WithMany().HasForeignKey(x => x.LearningProviderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Instructor>().WithMany().HasForeignKey(x => x.InstructorId).OnDelete(DeleteBehavior.Restrict);
    }
}
