using Careersity.Domain.Assessments;
using Careersity.Domain.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Careersity.Infrastructure.Persistence.Configurations;

public sealed class AssessmentResponseConfiguration : IEntityTypeConfiguration<AssessmentResponse>
{
    public void Configure(EntityTypeBuilder<AssessmentResponse> builder)
    {
        builder.ConfigureAuditableEntity("AssessmentResponses");
        builder.Property(x => x.PointsAwarded);
        builder.Property(x => x.UpdatedAtUtc).IsConcurrencyToken();
        builder.HasIndex(x => new { x.AssessmentAttemptId, x.QuestionId }).IsUnique();
        builder.HasOne<AssessmentAttempt>().WithMany(x => x.Responses).HasForeignKey(x => x.AssessmentAttemptId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Question>().WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.SelectedOptions).WithOne().HasForeignKey(x => x.AssessmentResponseId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.SelectedOptions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
