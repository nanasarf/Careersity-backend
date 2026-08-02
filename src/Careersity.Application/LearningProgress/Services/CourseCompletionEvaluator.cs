using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.Common.Exceptions;
using Careersity.Domain.Enums;
using Careersity.Domain.Learning;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.LearningProgress.Services;

public interface ICourseCompletionEvaluator
{
    Task<bool> CompleteIfEligibleAsync(CourseProgress progress, bool throwIfIncomplete, CancellationToken token);
}

public sealed class CourseCompletionEvaluator(ICareersityDbContext db) : ICourseCompletionEvaluator
{
    public async Task<bool> CompleteIfEligibleAsync(CourseProgress progress, bool throwIfIncomplete, CancellationToken token)
    {
        if (progress.CompletedAtUtc is not null) return true;
        var requiredLessonIds = await db.Lessons.AsNoTracking().Where(x => x.CourseId == progress.CourseId && x.IsRequired).Select(x => x.Id).ToListAsync(token);
        var completedLessonIds = progress.LessonProgressRecords.Where(x => x.CompletedAtUtc is not null).Select(x => x.LessonId).ToHashSet();
        var lessonsComplete = requiredLessonIds.All(completedLessonIds.Contains);
        var publishedAssessmentIds = await db.Assessments.AsNoTracking().Where(x => x.CourseId == progress.CourseId && x.Status == ContentStatus.Published).Select(x => x.Id).ToListAsync(token);
        var passedAssessmentIds = await db.AssessmentAttempts.AsNoTracking().Where(x => x.CourseProgressId == progress.Id && x.Status == AssessmentAttemptStatus.Passed).Select(x => x.AssessmentId).ToListAsync(token);
        var assessmentsComplete = publishedAssessmentIds.All(passedAssessmentIds.Contains);
        if (!lessonsComplete || !assessmentsComplete)
        {
            if (throwIfIncomplete) throw new ConflictException(!lessonsComplete ? "All required lessons must be completed first." : "All published assessments must be passed first.");
            return false;
        }
        progress.Complete(); return true;
    }
}
