using Careersity.Domain.Skills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ConfigureAuditableEntity("Skills");
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(170).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2_000);
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.Status);
    }
}
