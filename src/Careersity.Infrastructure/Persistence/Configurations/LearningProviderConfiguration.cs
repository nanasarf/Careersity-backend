using Careersity.Domain.LearningResources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class LearningProviderConfiguration : IEntityTypeConfiguration<LearningProvider>
{
    public void Configure(EntityTypeBuilder<LearningProvider> b)
    {
        b.ConfigureAuditableEntity("LearningProviders"); b.Property(x => x.Name).HasMaxLength(200).IsRequired(); b.Property(x => x.Slug).HasMaxLength(220).IsRequired();
        b.Property(x => x.Description).HasMaxLength(3000); b.Property(x => x.WebsiteUrl).HasMaxLength(2000); b.Property(x => x.LogoUrl).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired(); b.HasIndex(x => x.Slug).IsUnique(); b.HasIndex(x => x.Status); b.HasIndex(x => x.Name);
    }
}
