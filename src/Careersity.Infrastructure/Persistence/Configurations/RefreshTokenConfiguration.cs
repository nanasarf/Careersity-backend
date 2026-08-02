using Careersity.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", table =>
        {
            table.HasCheckConstraint("CK_RefreshTokens_ExpirationAfterCreation", "\"ExpiresAtUtc\" > \"CreatedAtUtc\"");
            table.HasCheckConstraint("CK_RefreshTokens_RevocationAfterCreation", "\"RevokedAtUtc\" IS NULL OR \"RevokedAtUtc\" >= \"CreatedAtUtc\"");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.ExpiresAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.RevokedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedByIp).HasMaxLength(64);
        builder.Property(x => x.RevokedByIp).HasMaxLength(64);
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.RevokedAtUtc, x.ExpiresAtUtc });
        builder.HasOne<User>().WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<RefreshToken>().WithMany().HasForeignKey(x => x.ReplacedByTokenId).OnDelete(DeleteBehavior.NoAction);
    }
}
