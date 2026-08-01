using Careersity.Domain.Careers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class PathwayLevelConfiguration : IEntityTypeConfiguration<PathwayLevel>
{
    public void Configure(EntityTypeBuilder<PathwayLevel> builder)
    {
        builder.ConfigureAuditableEntity("PathwayLevels");
        builder.ToTable("PathwayLevels", table => table.HasCheckConstraint("CK_PathwayLevels_Order_Nonnegative", "\"Order\" >= 0"));
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1_500);
        builder.HasIndex(x => new { x.CareerPathwayId, x.Order }).IsUnique();
        builder.HasIndex(x => x.CareerPathwayId);
        builder.HasMany(x => x.Courses).WithOne().HasForeignKey(x => x.PathwayLevelId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Courses).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
