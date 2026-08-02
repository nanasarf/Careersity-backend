using Careersity.Application.Abstractions.Authentication;
using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.Common.Models;
using Careersity.Application.CurriculumActivities.Dtos;
using Careersity.Application.LearningContent.Dtos;
using Careersity.Application.LearningProgress.Dtos;
using Careersity.Application.LearningProgress.Requests;
using Careersity.Domain.Careers;
using Careersity.Domain.Courses;
using Careersity.Domain.Enums;
using Careersity.Domain.Learning;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.LearningProgress.Services;

public sealed class LearningProgressService(ICareersityDbContext db, ICurrentUser currentUser, ICourseCompletionEvaluator completionEvaluator) : ILearningProgressService
{
    public async Task<CareerEnrollmentDetailDto> EnrollAsync(EnrollInCareerRequest request, CancellationToken token)
    {
        var userId = UserId();
        var career = await db.Careers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.CareerId && x.Status == ContentStatus.Published, token)
            ?? throw new NotFoundException("Published career was not found.");
        var categoryPublished = await db.CareerCategories.AsNoTracking().AnyAsync(x => x.Id == career.CareerCategoryId && x.Status == ContentStatus.Published, token);
        if (!categoryPublished) throw new ConflictException("The career category must be published before enrollment.");
        var pathway = await db.CareerPathways.AsNoTracking().Include(x => x.Levels).ThenInclude(x => x.Courses)
            .SingleOrDefaultAsync(x => x.CareerId == career.Id && x.IsPrimary && x.Status == ContentStatus.Published, token)
            ?? throw new ConflictException("The career has no primary published pathway.");
        if (pathway.Levels.Count == 0 || !pathway.Levels.SelectMany(x => x.Courses).Any(x => x.IsRequired))
            throw new ConflictException("The pathway must contain at least one required course.");
        var courseIds = pathway.Levels.SelectMany(x => x.Courses).Select(x => x.CourseId).Distinct().ToList();
        if (await db.Courses.AsNoTracking().CountAsync(x => courseIds.Contains(x.Id) && x.Status == ContentStatus.Published, token) != courseIds.Count)
            throw new ConflictException("All pathway courses must be published before enrollment.");
        if (await db.CareerEnrollments.AnyAsync(x => x.UserId == userId && x.CareerPathwayId == pathway.Id && x.Status != EnrollmentStatus.Withdrawn, token))
            throw new ConflictException("You already have an enrollment for this pathway.");
        var enrollment = new CareerEnrollment(userId, career.Id, pathway.Id); db.CareerEnrollments.Add(enrollment);
        await db.SaveChangesAsync(token); return await GetAsync(enrollment.Id, token);
    }

    public async Task<PagedResult<CareerEnrollmentListItemDto>> ListAsync(EnrollmentListQuery request, CancellationToken token)
    {
        var userId = UserId(); var query = db.CareerEnrollments.AsNoTracking().Where(x => x.UserId == userId);
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status);
        else if (!request.IncludeHistory) query = query.Where(x => x.Status == EnrollmentStatus.Active || x.Status == EnrollmentStatus.Paused);
        var total = await query.CountAsync(token);
        var ids = await query.OrderByDescending(x => x.UpdatedAtUtc ?? x.EnrolledAtUtc)
            .Skip((request.ValidatedPage - 1) * request.ValidatedPageSize).Take(request.ValidatedPageSize).Select(x => x.Id).ToListAsync(token);
        var items = new List<CareerEnrollmentListItemDto>();
        foreach (var id in ids)
        {
            var detail = await GetAsync(id, token);
            var current = detail.Levels.OrderBy(x => x.Order).SelectMany(level => level.Courses.OrderBy(x => x.Order)
                .Where(course => course.AvailabilityStatus is CourseAvailabilityStatus.Available or CourseAvailabilityStatus.InProgress)
                .Select(course => new CurrentCourseDto(course.CourseId, course.Title, course.Slug, level.Name, level.Order, course.Order, course.ProgressPercentage))).FirstOrDefault();
            items.Add(new(detail.Id, detail.Career.Id, detail.Career.Title, detail.Career.Slug, detail.Pathway.Id,
                detail.Pathway.Name, detail.Pathway.Version, detail.Status, detail.EnrolledAtUtc, detail.StartedAtUtc,
                detail.CompletedAtUtc, detail.OverallProgressPercentage, detail.CompletedRequiredCourseCount,
                detail.TotalRequiredCourseCount, current));
        }
        return new(items, request.ValidatedPage, request.ValidatedPageSize, total);
    }

    public async Task<CareerEnrollmentDetailDto> GetAsync(Guid enrollmentId, CancellationToken token)
    {
        var enrollment = await FindEnrollmentAsync(enrollmentId, true, token);
        var graph = await LoadGraphAsync(enrollment, token);
        return await BuildDetailAsync(enrollment, graph, token);
    }

    public async Task PauseAsync(Guid id, CancellationToken token) { var x = await FindEnrollmentAsync(id, false, token); x.Pause(); await db.SaveChangesAsync(token); }
    public async Task ResumeAsync(Guid id, CancellationToken token) { var x = await FindEnrollmentAsync(id, false, token); x.Resume(); await db.SaveChangesAsync(token); }
    public async Task WithdrawAsync(Guid id, CancellationToken token) { var x = await FindEnrollmentAsync(id, false, token); x.Withdraw(); await db.SaveChangesAsync(token); }

    public async Task<LearnerCourseDetailDto> GetCourseAsync(Guid enrollmentId, Guid courseId, CancellationToken token)
    {
        var enrollment = await FindEnrollmentAsync(enrollmentId, true, token); var graph = await LoadGraphAsync(enrollment, token);
        return await BuildCourseDetailAsync(enrollment, graph, courseId, token);
    }

    public async Task<LearnerCourseDetailDto> StartCourseAsync(Guid enrollmentId, Guid courseId, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var enrollment = await FindEnrollmentAsync(enrollmentId, true, token); enrollment.EnsureActivityAllowed();
        var graph = await LoadGraphAsync(enrollment, token); EnsurePublished(graph, courseId); EnsureAvailable(enrollment, graph, courseId);
        var existing = enrollment.CourseProgressRecords.SingleOrDefault(x => x.CourseId == courseId);
        if (existing is null) db.CourseProgressRecords.Add(enrollment.AddCourseProgress(courseId)); else existing.RecordAccess();
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return await GetCourseAsync(enrollmentId, courseId, token);
    }

    public async Task<LearnerCourseDetailDto> CompleteCourseAsync(Guid enrollmentId, Guid courseId, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var enrollment = await FindEnrollmentAsync(enrollmentId, true, token); enrollment.EnsureActivityAllowed();
        var graph = await LoadGraphAsync(enrollment, token); EnsurePublished(graph, courseId); EnsureAvailable(enrollment, graph, courseId);
        var progress = enrollment.CourseProgressRecords.SingleOrDefault(x => x.CourseId == courseId);
        if (progress is null) { progress = enrollment.AddCourseProgress(courseId); db.CourseProgressRecords.Add(progress); }
        await completionEvaluator.CompleteIfEligibleAsync(progress, true, token);
        CompleteEnrollmentIfEligible(enrollment, graph);
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token); return await GetCourseAsync(enrollmentId, courseId, token);
    }

    public async Task<LearnerLessonDetailDto> GetLessonAsync(Guid enrollmentId, Guid courseId, Guid lessonId, CancellationToken token)
    {
        var enrollment = await FindEnrollmentAsync(enrollmentId, true, token); var graph = await LoadGraphAsync(enrollment, token);
        EnsureAvailable(enrollment, graph, courseId);
        var lesson = graph.Courses[courseId].Lessons.SingleOrDefault(x => x.Id == lessonId) ?? throw new NotFoundException("Lesson was not found in this course.");
        return LessonDetail(lesson, enrollment.CourseProgressRecords.SingleOrDefault(x => x.CourseId == courseId)?.LessonProgressRecords.SingleOrDefault(x => x.LessonId == lessonId));
    }

    public async Task<LearnerLessonDetailDto> StartLessonAsync(Guid enrollmentId, Guid courseId, Guid lessonId, CancellationToken token) =>
        await MutateLessonAsync(enrollmentId, courseId, lessonId, false, token);

    public async Task<LearnerLessonDetailDto> CompleteLessonAsync(Guid enrollmentId, Guid courseId, Guid lessonId, CancellationToken token) =>
        await MutateLessonAsync(enrollmentId, courseId, lessonId, true, token);

    private async Task<LearnerLessonDetailDto> MutateLessonAsync(Guid enrollmentId, Guid courseId, Guid lessonId, bool complete, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var enrollment = await FindEnrollmentAsync(enrollmentId, true, token); enrollment.EnsureActivityAllowed(); var graph = await LoadGraphAsync(enrollment, token);
        EnsurePublished(graph, courseId); EnsureAvailable(enrollment, graph, courseId);
        var course = graph.Courses[courseId]; var lesson = course.Lessons.SingleOrDefault(x => x.Id == lessonId) ?? throw new NotFoundException("Lesson was not found in this course.");
        var courseProgress = enrollment.CourseProgressRecords.SingleOrDefault(x => x.CourseId == courseId);
        if (courseProgress is null) { courseProgress = enrollment.AddCourseProgress(courseId); db.CourseProgressRecords.Add(courseProgress); }
        var lessonProgress = courseProgress.LessonProgressRecords.SingleOrDefault(x => x.LessonId == lessonId);
        if (lessonProgress is null) { lessonProgress = courseProgress.AddLessonProgress(lessonId); db.LessonProgressRecords.Add(lessonProgress); }
        else lessonProgress.RecordAccess();
        if (complete) { lessonProgress.Complete(); await completionEvaluator.CompleteIfEligibleAsync(courseProgress, false, token); CompleteEnrollmentIfEligible(enrollment, graph); }
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token); return LessonDetail(lesson, lessonProgress);
    }

    private async Task<CareerEnrollment> FindEnrollmentAsync(Guid id, bool children, CancellationToken token)
    {
        var userId = UserId(); IQueryable<CareerEnrollment> query = db.CareerEnrollments;
        if (children) query = query.Include(x => x.CourseProgressRecords).ThenInclude(x => x.LessonProgressRecords)
            .Include(x => x.CourseProgressRecords).ThenInclude(x => x.ExternalResourceProgressRecords);
        return await query.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, token) ?? throw new NotFoundException("Enrollment was not found.");
    }

    private async Task<PathwayGraph> LoadGraphAsync(CareerEnrollment enrollment, CancellationToken token)
    {
        var career = await db.Careers.AsNoTracking().SingleAsync(x => x.Id == enrollment.CareerId, token);
        var pathway = await db.CareerPathways.AsNoTracking().Include(x => x.Levels).ThenInclude(x => x.Courses)
            .SingleAsync(x => x.Id == enrollment.CareerPathwayId, token);
        var ids = pathway.Levels.SelectMany(x => x.Courses).Select(x => x.CourseId).Distinct().ToList();
        var courses = await db.Courses.AsNoTracking().Include(x => x.Lessons).Include(x => x.Prerequisites)
            .Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, token);
        return new(career, pathway, courses);
    }

    private static void EnsureAvailable(CareerEnrollment enrollment, PathwayGraph graph, Guid courseId)
    {
        var assignment = graph.Pathway.Levels.SelectMany(level => level.Courses.Select(course => (Level: level, Assignment: course)))
            .SingleOrDefault(x => x.Assignment.CourseId == courseId);
        if (assignment.Assignment is null || !graph.Courses.TryGetValue(courseId, out var course)) throw new NotFoundException("Course was not found in this enrollment.");
        var progress = enrollment.CourseProgressRecords.SingleOrDefault(x => x.CourseId == courseId);
        if (course.Status != ContentStatus.Published && progress is null) throw new ConflictException("Archived content cannot receive new progress activity.");
        if (Availability(enrollment, graph, assignment.Level, course) == CourseAvailabilityStatus.Locked)
            throw new ConflictException("This course is locked because required earlier courses or prerequisites are incomplete.");
    }

    private static void EnsurePublished(PathwayGraph graph, Guid courseId)
    {
        if (!graph.Courses.TryGetValue(courseId, out var course)) throw new NotFoundException("Course was not found in this enrollment.");
        if (course.Status != ContentStatus.Published) throw new ConflictException("Archived content cannot receive new progress activity.");
    }

    private static CourseAvailabilityStatus Availability(CareerEnrollment enrollment, PathwayGraph graph, PathwayLevel level, Course course)
    {
        var progress = enrollment.CourseProgressRecords.SingleOrDefault(x => x.CourseId == course.Id);
        if (progress?.CompletedAtUtc is not null) return CourseAvailabilityStatus.Completed;
        if (progress is not null) return CourseAvailabilityStatus.InProgress;
        if (course.Status != ContentStatus.Published) return CourseAvailabilityStatus.Locked;
        var completed = enrollment.CourseProgressRecords.Where(x => x.CompletedAtUtc is not null).Select(x => x.CourseId).ToHashSet();
        var earlierRequired = graph.Pathway.Levels.Where(x => x.Order < level.Order).SelectMany(x => x.Courses).Where(x => x.IsRequired).Select(x => x.CourseId);
        if (earlierRequired.Any(x => !completed.Contains(x))) return CourseAvailabilityStatus.Locked;
        if (course.Prerequisites.Where(x => x.IsRequired).Any(x => !completed.Contains(x.PrerequisiteCourseId))) return CourseAvailabilityStatus.Locked;
        return CourseAvailabilityStatus.Available;
    }

    private static void CompleteEnrollmentIfEligible(CareerEnrollment enrollment, PathwayGraph graph)
    {
        var required = graph.Pathway.Levels.SelectMany(x => x.Courses).Where(x => x.IsRequired).Select(x => x.CourseId).Distinct().ToList();
        var completed = enrollment.CourseProgressRecords.Where(x => x.CompletedAtUtc is not null).Select(x => x.CourseId).ToHashSet();
        if (required.Count > 0 && required.All(completed.Contains)) enrollment.Complete();
    }

    private async Task<CareerEnrollmentDetailDto> BuildDetailAsync(CareerEnrollment enrollment, PathwayGraph graph, CancellationToken token)
    {
        var completed = enrollment.CourseProgressRecords.Where(x => x.CompletedAtUtc is not null).Select(x => x.CourseId).ToHashSet();
        var requiredIds = graph.Pathway.Levels.SelectMany(x => x.Courses).Where(x => x.IsRequired).Select(x => x.CourseId).Distinct().ToList();
        var levels = new List<LearnerPathwayLevelDto>();
        foreach (var level in graph.Pathway.Levels.OrderBy(x => x.Order))
        {
            var assignments = level.Courses.OrderBy(x => x.Order).Where(x => graph.Courses.ContainsKey(x.CourseId)).ToList();
            var courses = new List<LearnerCourseProgressDto>();
            foreach (var assignment in assignments) courses.Add(await CourseSummaryAsync(enrollment, graph, level, assignment, token));
            var required = assignments.Where(x => x.IsRequired).Select(x => x.CourseId).ToList();
            var relevant = required.Count > 0 ? required : assignments.Select(x => x.CourseId).ToList();
            var done = relevant.Count(x => completed.Contains(x));
            levels.Add(new LearnerPathwayLevelDto(level.Id, level.Name, level.Description, level.Order,
                relevant.Count > 0 && done == relevant.Count, Percentage(done, relevant.Count), courses));
        }
        var completedRequired = requiredIds.Count(completed.Contains);
        return new(enrollment.Id, new(graph.Career.Id, graph.Career.Title, graph.Career.Slug),
            new(graph.Pathway.Id, graph.Pathway.Name, graph.Pathway.Version), enrollment.Status, enrollment.EnrolledAtUtc,
            enrollment.StartedAtUtc, enrollment.PausedAtUtc, enrollment.CompletedAtUtc, enrollment.WithdrawnAtUtc,
            Percentage(completedRequired, requiredIds.Count), completedRequired, requiredIds.Count, levels);
    }

    private async Task<LearnerCourseProgressDto> CourseSummaryAsync(CareerEnrollment enrollment, PathwayGraph graph, PathwayLevel level, PathwayLevelCourse assignment, CancellationToken token)
    {
        var course = graph.Courses[assignment.CourseId]; var progress = enrollment.CourseProgressRecords.SingleOrDefault(x => x.CourseId == course.Id);
        var required = course.Lessons.Where(x => x.IsRequired).Select(x => x.Id).ToList();
        var completed = progress?.LessonProgressRecords.Where(x => x.CompletedAtUtc is not null).Select(x => x.LessonId).ToHashSet() ?? [];
        var metrics = await RequirementMetricsAsync(course.Id, progress, required.Count, required.Count(completed.Contains), token);
        return new(course.Id, course.Title, course.Slug, course.Difficulty, course.EstimatedDurationMinutes, assignment.Order,
            assignment.IsRequired, Availability(enrollment, graph, level, course), progress is not null, progress?.CompletedAtUtc is not null,
            metrics.Percentage, completed.Count(x => required.Contains(x)), required.Count,
            progress?.StartedAtUtc, progress?.CompletedAtUtc, metrics.CompletedResources, metrics.RequiredResources);
    }

    private async Task<LearnerCourseDetailDto> BuildCourseDetailAsync(CareerEnrollment enrollment, PathwayGraph graph, Guid courseId, CancellationToken token)
    {
        var pair = graph.Pathway.Levels.SelectMany(level => level.Courses.Select(a => (Level: level, Assignment: a))).SingleOrDefault(x => x.Assignment.CourseId == courseId);
        if (pair.Assignment is null || !graph.Courses.TryGetValue(courseId, out var course)) throw new NotFoundException("Course was not found in this enrollment.");
        var summary = await CourseSummaryAsync(enrollment, graph, pair.Level, pair.Assignment, token); var progress = enrollment.CourseProgressRecords.SingleOrDefault(x => x.CourseId == courseId);
        var prerequisites = await (from relation in db.CoursePrerequisites.AsNoTracking() join prerequisite in db.Courses.AsNoTracking() on relation.PrerequisiteCourseId equals prerequisite.Id
            where relation.CourseId == courseId select new CoursePrerequisiteDto(relation.Id, prerequisite.Id, prerequisite.Title, prerequisite.Slug, relation.IsRequired)).ToListAsync(token);
        var lessons = course.Lessons.OrderBy(x => x.Order).Select(x => LessonSummary(x, progress?.LessonProgressRecords.SingleOrDefault(p => p.LessonId == x.Id))).ToList();
        var projects = await db.Projects.AsNoTracking().Where(x => x.CourseId == courseId && x.Status == ContentStatus.Published)
            .OrderBy(x => x.Title).Select(x => new PublicProjectSummaryDto(x.Id, x.Title, x.Description, x.SubmissionType, x.EstimatedDurationMinutes)).ToListAsync(token);
        var assessments = await db.Assessments.AsNoTracking().Where(x => x.CourseId == courseId && x.Status == ContentStatus.Published)
            .OrderBy(x => x.Title).Select(x => new PublicAssessmentSummaryDto(x.Id, x.Title, x.Description, x.PassingScorePercentage, x.MaximumAttempts, x.Questions.Count, x.Questions.Sum(q => q.Points))).ToListAsync(token);
        var externalResources = await (from a in db.CourseExternalResources.AsNoTracking()
            join r in db.ExternalLearningResources.AsNoTracking() on a.ExternalLearningResourceId equals r.Id
            join provider in db.LearningProviders.AsNoTracking() on r.LearningProviderId equals provider.Id
            join i0 in db.Instructors.AsNoTracking() on r.InstructorId equals i0.Id into instructors from instructor in instructors.DefaultIfEmpty()
            join ep0 in db.ExternalResourceProgressRecords.AsNoTracking().Where(x => progress != null && x.CourseProgressId == progress.Id) on a.Id equals ep0.CourseExternalResourceId into progresses from ep in progresses.DefaultIfEmpty()
            where a.CourseId == courseId && r.Status == ContentStatus.Published orderby a.Order
            select new Careersity.Application.ExternalLearning.Dtos.LearnerExternalResourceProgressDto(a.Id,r.Id,r.Title,r.Description,r.ResourceType,r.AccessType,r.Url,provider.Name,instructor==null?null:instructor.Name,a.Order,a.IsRequired,ep!=null,ep!=null&&ep.CompletedAtUtc!=null,ep==null?null:ep.StartedAtUtc,ep==null?null:ep.CompletedAtUtc,r.EstimatedDurationMinutes)).ToListAsync(token);
        var requiredLessons = lessons.Count(x => x.IsRequired); var completedLessons = lessons.Count(x => x.IsRequired && x.IsCompleted);
        var passedAssessments = progress is null ? 0 : await db.AssessmentAttempts.AsNoTracking().Where(x => x.CourseProgressId == progress.Id && x.Status == AssessmentAttemptStatus.Passed && db.Assessments.Any(a => a.Id == x.AssessmentId && a.Status == ContentStatus.Published)).Select(x => x.AssessmentId).Distinct().CountAsync(token);
        var requiredResources = externalResources.Count(x => x.IsRequired); var completedResources = externalResources.Count(x => x.IsRequired && x.IsCompleted);
        var totalRequirements = requiredLessons + assessments.Count + requiredResources; var completedRequirements = completedLessons + passedAssessments + completedResources;
        var requirementPercentage = Percentage(completedRequirements, totalRequirements);
        return new(course.Id, course.Title, course.Slug, course.ShortDescription, course.DetailedDescription, course.Difficulty,
            course.EstimatedDurationMinutes, summary.AvailabilityStatus, summary.IsStarted, summary.IsCompleted,
            requirementPercentage, prerequisites, lessons, projects, assessments, externalResources, completedResources, requiredResources);
    }

    private static LearnerLessonProgressDto LessonSummary(Lesson x, LessonProgress? p) => new(x.Id, x.Title, x.Slug, x.Summary,
        x.ContentType, x.EstimatedDurationMinutes, x.Order, x.IsRequired, p is not null, p?.CompletedAtUtc is not null, p?.StartedAtUtc, p?.CompletedAtUtc);
    private static LearnerLessonDetailDto LessonDetail(Lesson x, LessonProgress? p) => new(x.Id, x.CourseId, x.Title, x.Slug, x.Summary,
        x.Content, x.ContentType, x.ExternalResourceUrl, x.EstimatedDurationMinutes, x.Order, x.IsRequired, p is not null,
        p?.CompletedAtUtc is not null, p?.StartedAtUtc, p?.CompletedAtUtc);
    private static decimal Percentage(int completed, int total) => total == 0 ? 0 : Math.Clamp(Math.Round(completed * 100m / total, 2), 0, 100);
    private async Task<(decimal Percentage, int CompletedResources, int RequiredResources)> RequirementMetricsAsync(Guid courseId, CourseProgress? progress, int requiredLessons, int completedLessons, CancellationToken token)
    {
        var assessmentIds = await db.Assessments.AsNoTracking().Where(x => x.CourseId == courseId && x.Status == ContentStatus.Published).Select(x => x.Id).ToListAsync(token);
        var passed = progress is null ? 0 : await db.AssessmentAttempts.AsNoTracking().Where(x => x.CourseProgressId == progress.Id && x.Status == AssessmentAttemptStatus.Passed && assessmentIds.Contains(x.AssessmentId)).Select(x => x.AssessmentId).Distinct().CountAsync(token);
        var resourceIds = await (from a in db.CourseExternalResources.AsNoTracking() join r in db.ExternalLearningResources.AsNoTracking() on a.ExternalLearningResourceId equals r.Id where a.CourseId == courseId && a.IsRequired && r.Status == ContentStatus.Published select a.Id).ToListAsync(token);
        var completedResources = progress is null ? 0 : await db.ExternalResourceProgressRecords.AsNoTracking().CountAsync(x => x.CourseProgressId == progress.Id && x.CompletedAtUtc != null && resourceIds.Contains(x.CourseExternalResourceId), token);
        return (Percentage(completedLessons + passed + completedResources, requiredLessons + assessmentIds.Count + resourceIds.Count), completedResources, resourceIds.Count);
    }
    private Guid UserId() => currentUser.IsAuthenticated && currentUser.UserId.HasValue ? currentUser.UserId.Value : throw new UnauthorizedException();
    private sealed record PathwayGraph(Career Career, CareerPathway Pathway, IReadOnlyDictionary<Guid, Course> Courses);
}
