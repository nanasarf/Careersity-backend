using Careersity.Domain.Assessments;
using Careersity.Domain.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class AssessmentResponseOptionConfiguration : IEntityTypeConfiguration<AssessmentResponseOption>
{
    public void Configure(EntityTypeBuilder<AssessmentResponseOption> builder)
    {
        builder.ToTable("AssessmentResponseOptions");
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(x => new { x.AssessmentResponseId, x.AnswerOptionId }).IsUnique();
        builder.HasOne<AssessmentResponse>().WithMany(x => x.SelectedOptions).HasForeignKey(x => x.AssessmentResponseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AnswerOption>().WithMany().HasForeignKey(x => x.AnswerOptionId).OnDelete(DeleteBehavior.Restrict);
    }
}
