using Careersity.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ConfigureAuditableEntity("Users");
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.NormalizedEmail).HasMaxLength(320).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(1_000).IsRequired();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.LastLoginAtUtc).HasColumnType("timestamp with time zone");
        builder.HasIndex(x => x.NormalizedEmail).IsUnique();
        builder.HasIndex(x => x.Role);
        builder.HasIndex(x => x.IsActive);
        builder.HasMany(x => x.RefreshTokens).WithOne().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.RefreshTokens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
