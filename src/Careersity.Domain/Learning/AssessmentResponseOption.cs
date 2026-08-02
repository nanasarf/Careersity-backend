using Careersity.Domain.Common;

namespace Careersity.Domain.Learning;

public sealed class AssessmentResponseOption : Entity
{
    private AssessmentResponseOption() { }
    internal AssessmentResponseOption(Guid assessmentResponseId, Guid answerOptionId)
    {
        AssessmentResponseId = Guard.NotEmpty(assessmentResponseId, nameof(assessmentResponseId));
        AnswerOptionId = Guard.NotEmpty(answerOptionId, nameof(answerOptionId));
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }
    public Guid AssessmentResponseId { get; private set; }
    public Guid AnswerOptionId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
