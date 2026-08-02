using Careersity.Domain.LearningResources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> b)
    {
        b.ConfigureAuditableEntity("Instructors"); b.Property(x => x.Name).HasMaxLength(200).IsRequired(); b.Property(x => x.Title).HasMaxLength(200);
        b.Property(x => x.Biography).HasMaxLength(3000); b.Property(x => x.ProfileUrl).HasMaxLength(2000); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.LearningProviderId); b.HasIndex(x => x.Status); b.HasOne<LearningProvider>().WithMany().HasForeignKey(x => x.LearningProviderId).OnDelete(DeleteBehavior.Restrict);
    }
}
