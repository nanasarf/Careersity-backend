using Careersity.Domain.Courses;
using Careersity.Domain.Skills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class CourseSkillConfiguration : IEntityTypeConfiguration<CourseSkill>
{
    public void Configure(EntityTypeBuilder<CourseSkill> builder)
    {
        builder.ConfigureAuditableEntity("CourseSkills");
        builder.Property(x => x.ProficiencyLevel).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.CourseId, x.SkillId }).IsUnique();
        builder.HasIndex(x => x.SkillId);
        builder.HasOne<Skill>().WithMany().HasForeignKey(x => x.SkillId).OnDelete(DeleteBehavior.Restrict);
    }
}
