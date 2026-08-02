using Careersity.Domain.Assessments;

namespace Careersity.Application.AssessmentAttempts.Services;

internal static class AssessmentGrader
{
    internal sealed record Result(decimal Score, int Earned, int Total, IReadOnlyDictionary<Guid, (bool IsCorrect, int PointsAwarded)> Grades);
    internal static Result Grade(IEnumerable<Question> questions, IReadOnlyDictionary<Guid, IReadOnlyCollection<Guid>> answers)
    {
        var list = questions.ToList(); var grades = new Dictionary<Guid, (bool, int)>(); var earned = 0; var total = list.Sum(x => x.Points);
        foreach (var question in list)
        {
            var selected = answers[question.Id].ToHashSet();
            var correct = question.AnswerOptions.Where(x => x.IsCorrect).Select(x => x.Id).ToHashSet();
            var isCorrect = selected.SetEquals(correct);
            var awarded = isCorrect ? question.Points : 0; earned += awarded; grades[question.Id] = (isCorrect, awarded);
        }
        return new(Math.Round(earned * 100m / total, 2, MidpointRounding.AwayFromZero), earned, total, grades);
    }
}
