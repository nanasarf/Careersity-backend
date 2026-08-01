using Careersity.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    internal static void ConfigureAuditableEntity<TEntity>(this EntityTypeBuilder<TEntity> builder, string tableName)
        where TEntity : AuditableEntity
    {
        builder.ToTable(tableName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("timestamp with time zone");
    }
}
