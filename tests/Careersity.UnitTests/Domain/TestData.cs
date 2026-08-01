using Careersity.Domain.Assessments;
using Careersity.Domain.Careers;
using Careersity.Domain.Courses;
using Careersity.Domain.Enums;

namespace Careersity.UnitTests.Domain;

internal static class TestData
{
    internal static Career Career(int? weeks = 12) =>
        new(Guid.NewGuid(), "Software Engineer", "SOFTWARE-ENGINEER", "Builds software.", estimatedDurationWeeks: weeks);

    internal static CareerPathway Pathway() => new(Guid.NewGuid(), "Core pathway", "1.0");

    internal static Course Course() =>
        new("Programming", "PROGRAMMING", "Learn programming.", CourseDifficulty.Foundation, 60);

    internal static Lesson Lesson(Course course, int order = 0, LessonContentType type = LessonContentType.Article,
        string? url = null) => new(course.Id, "Introduction", "INTRODUCTION", type, 10, order, externalResourceUrl: url);

    internal static Assessment Assessment() => new(Guid.NewGuid(), "Knowledge check", 70);

    internal static Question ValidSingleChoice(Assessment assessment, int order = 0)
    {
        var question = new Question(assessment.Id, "Choose one", QuestionType.SingleChoice, order, 1);
        question.AddAnswerOption(new AnswerOption(question.Id, "Correct", true, 0));
        question.AddAnswerOption(new AnswerOption(question.Id, "Incorrect", false, 1));
        return question;
    }
}
