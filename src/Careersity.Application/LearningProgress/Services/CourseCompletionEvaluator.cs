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
        var requiredResourceIds = await (from assignment in db.CourseExternalResources.AsNoTracking()
            join resource in db.ExternalLearningResources.AsNoTracking() on assignment.ExternalLearningResourceId equals resource.Id
            where assignment.CourseId == progress.CourseId && assignment.IsRequired && resource.Status == ContentStatus.Published
            select assignment.Id).ToListAsync(token);
        var completedResourceIds = await db.ExternalResourceProgressRecords.AsNoTracking().Where(x => x.CourseProgressId == progress.Id && x.CompletedAtUtc != null).Select(x => x.CourseExternalResourceId).ToListAsync(token);
        var resourcesComplete = requiredResourceIds.All(completedResourceIds.Contains);
        if (!lessonsComplete || !assessmentsComplete || !resourcesComplete)
        {
            if (throwIfIncomplete) throw new ConflictException(!lessonsComplete ? "All required lessons must be completed first." : !assessmentsComplete ? "All published assessments must be passed first." : "All required published external resources must be completed first.");
            return false;
        }
        progress.Complete(); return true;
    }
}
