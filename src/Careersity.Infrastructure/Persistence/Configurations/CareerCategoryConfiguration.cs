using Careersity.Domain.Careers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class CareerCategoryConfiguration : IEntityTypeConfiguration<CareerCategory>
{
    public void Configure(EntityTypeBuilder<CareerCategory> builder)
    {
        builder.ConfigureAuditableEntity("CareerCategories");
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1_000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Name);
    }
}
