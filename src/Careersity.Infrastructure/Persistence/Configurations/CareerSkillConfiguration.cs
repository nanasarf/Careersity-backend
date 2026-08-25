using Careersity.Domain.Careers;
using Careersity.Domain.Skills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class CareerSkillConfiguration : IEntityTypeConfiguration<CareerSkill>
{
    public void Configure(EntityTypeBuilder<CareerSkill> builder)
    {
        builder.ConfigureAuditableEntity("CareerSkills");
        builder.ToTable("CareerSkills", table => table.HasCheckConstraint("CK_CareerSkills_DisplayOrder_Nonnegative", "\"DisplayOrder\" >= 0"));
        builder.Property(x => x.RequiredProficiencyLevel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.CareerId, x.SkillId }).IsUnique();
        builder.HasIndex(x => new { x.CareerId, x.DisplayOrder }).IsUnique();
        builder.HasIndex(x => x.CareerId);
        builder.HasIndex(x => x.SkillId);
        builder.HasOne<Career>().WithMany().HasForeignKey(x => x.CareerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Skill>().WithMany().HasForeignKey(x => x.SkillId).OnDelete(DeleteBehavior.Restrict);
    }
}
