using Careersity.Domain.Careers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class CareerConfiguration : IEntityTypeConfiguration<Career>
{
    public void Configure(EntityTypeBuilder<Career> builder)
    {
        builder.ConfigureAuditableEntity("Careers");
        builder.ToTable("Careers", table => table.HasCheckConstraint("CK_Careers_EstimatedDurationWeeks_Positive", "\"EstimatedDurationWeeks\" IS NULL OR \"EstimatedDurationWeeks\" > 0"));
        builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(170).IsRequired();
        builder.Property(x => x.ShortDescription).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DetailedDescription).HasMaxLength(5_000);
        builder.Property(x => x.Responsibilities).HasMaxLength(5_000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.CareerCategoryId);
        builder.HasIndex(x => x.Status);
        builder.HasOne<CareerCategory>().WithMany().HasForeignKey(x => x.CareerCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
