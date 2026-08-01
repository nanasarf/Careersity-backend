using Careersity.Domain.Careers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class CareerPathwayConfiguration : IEntityTypeConfiguration<CareerPathway>
{
    public void Configure(EntityTypeBuilder<CareerPathway> builder)
    {
        builder.ConfigureAuditableEntity("CareerPathways");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2_000);
        builder.Property(x => x.Version).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.CareerId);
        builder.HasIndex(x => new { x.CareerId, x.Version }).IsUnique();
        builder.HasIndex(x => x.CareerId).IsUnique().HasFilter("\"IsPrimary\" = TRUE").HasDatabaseName("IX_CareerPathways_OnePrimaryPerCareer");
        builder.HasIndex(x => x.Status);
        builder.HasOne<Career>().WithMany().HasForeignKey(x => x.CareerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Levels).WithOne().HasForeignKey(x => x.CareerPathwayId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Levels).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
