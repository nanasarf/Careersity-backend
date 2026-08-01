using Careersity.Domain.Assessments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ConfigureAuditableEntity("Questions");
        builder.ToTable("Questions", table =>
        {
            table.HasCheckConstraint("CK_Questions_Order_Nonnegative", "\"Order\" >= 0");
            table.HasCheckConstraint("CK_Questions_Points_Positive", "\"Points\" > 0");
        });
        builder.Property(x => x.Prompt).HasMaxLength(2_000).IsRequired();
        builder.Property(x => x.QuestionType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(x => new { x.AssessmentId, x.Order }).IsUnique();
        builder.HasMany(x => x.AnswerOptions).WithOne().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.AnswerOptions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
