using Careersity.Domain.Assessments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class AnswerOptionConfiguration : IEntityTypeConfiguration<AnswerOption>
{
    public void Configure(EntityTypeBuilder<AnswerOption> builder)
    {
        builder.ConfigureAuditableEntity("AnswerOptions");
        builder.ToTable("AnswerOptions", table => table.HasCheckConstraint("CK_AnswerOptions_Order_Nonnegative", "\"Order\" >= 0"));
        builder.Property(x => x.Text).HasMaxLength(1_000).IsRequired();
        builder.HasIndex(x => new { x.QuestionId, x.Order }).IsUnique();
        builder.HasIndex(x => x.QuestionId);
    }
}
