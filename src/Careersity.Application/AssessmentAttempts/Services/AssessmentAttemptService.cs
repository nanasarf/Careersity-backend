using Careersity.Application.Abstractions.Authentication;
using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.AssessmentAttempts.Dtos;
using Careersity.Application.AssessmentAttempts.Requests;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.LearningProgress.Services;
using Careersity.Domain.Assessments;
using Careersity.Domain.Enums;
using Careersity.Domain.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Careersity.Application.AssessmentAttempts.Services;

public sealed class AssessmentAttemptService(ICareersityDbContext db, ICurrentUser currentUser,
    ICourseCompletionEvaluator completion, ILogger<AssessmentAttemptService> logger) : IAssessmentAttemptService
{
    public async Task<IReadOnlyCollection<LearnerAssessmentSummaryDto>> ListAsync(Guid enrollmentId, Guid courseId, CancellationToken token)
    {
        var (_, progress) = await ContextAsync(enrollmentId, courseId, false, token);
        var assessments = await db.Assessments.AsNoTracking().Where(x => x.CourseId == courseId && x.Status == ContentStatus.Published)
            .OrderBy(x => x.Title).Select(x => new { x.Id, x.Title, x.Description, x.PassingScorePercentage, x.MaximumAttempts, Count = x.Questions.Count, Total = x.Questions.Sum(q => q.Points) }).ToListAsync(token);
        var stats = await db.AssessmentAttempts.AsNoTracking().Where(x => x.CareerEnrollmentId == enrollmentId && x.UserId == UserId())
            .GroupBy(x => x.AssessmentId).Select(g => new { Id = g.Key, Count = g.Count(), Passed = g.Any(x => x.Status == AssessmentAttemptStatus.Passed), Active = g.Where(x => x.Status == AssessmentAttemptStatus.InProgress).Select(x => (Guid?)x.Id).FirstOrDefault() }).ToDictionaryAsync(x => x.Id, token);
        return assessments.Select(x => { stats.TryGetValue(x.Id, out var s); var used = s?.Count ?? 0; return new LearnerAssessmentSummaryDto(x.Id, x.Title, x.Description, x.PassingScorePercentage, x.MaximumAttempts, used, x.MaximumAttempts.HasValue ? Math.Max(0, x.MaximumAttempts.Value - used) : null, s?.Passed ?? false, s?.Active, x.Count, x.Total); }).ToList();
    }

    public async Task<StartAssessmentAttemptResult> StartAsync(Guid enrollmentId, Guid courseId, Guid assessmentId, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var (enrollment, progress) = await ContextAsync(enrollmentId, courseId, true, token);
        var assessment = await AssessmentAsync(courseId, assessmentId, true, true, token);
        var attempts = await db.AssessmentAttempts.Where(x => x.CareerEnrollmentId == enrollmentId && x.AssessmentId == assessmentId).OrderBy(x => x.AttemptNumber).ToListAsync(token);
        var active = attempts.SingleOrDefault(x => x.Status == AssessmentAttemptStatus.InProgress);
        if (active is not null) { await transaction.CommitAsync(token); return new(ToStart(active, assessment), false); }
        if (attempts.Any(x => x.Status == AssessmentAttemptStatus.Passed)) throw new ConflictException("This assessment has already been passed.");
        if (assessment.MaximumAttempts.HasValue && attempts.Count >= assessment.MaximumAttempts.Value)
        { logger.LogInformation("Assessment attempt limit reached for assessment {AssessmentId}", assessmentId); throw new ConflictException("The maximum number of assessment attempts has been reached."); }
        var attempt = new AssessmentAttempt(UserId(), enrollment.Id, progress.Id, assessment.Id, attempts.Count + 1);
        db.AssessmentAttempts.Add(attempt); await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        logger.LogInformation("Assessment attempt {AttemptId} started for assessment {AssessmentId}", attempt.Id, assessmentId);
        return new(ToStart(attempt, assessment), true);
    }

    public async Task<IReadOnlyCollection<AssessmentAttemptHistoryItemDto>> HistoryAsync(Guid enrollmentId, Guid courseId, Guid assessmentId, CancellationToken token)
    {
        await ContextAsync(enrollmentId, courseId, false, token); await AssessmentAsync(courseId, assessmentId, false, false, token);
        return await db.AssessmentAttempts.AsNoTracking().Where(x => x.UserId == UserId() && x.CareerEnrollmentId == enrollmentId && x.AssessmentId == assessmentId)
            .OrderByDescending(x => x.AttemptNumber).Select(x => new AssessmentAttemptHistoryItemDto(x.Id, x.AttemptNumber, x.Status, x.StartedAtUtc, x.SubmittedAtUtc, x.ScorePercentage, x.Status == AssessmentAttemptStatus.Passed)).ToListAsync(token);
    }

    public async Task<AssessmentAttemptDetailDto> GetAsync(Guid enrollmentId, Guid courseId, Guid assessmentId, Guid attemptId, CancellationToken token)
    {
        await ContextAsync(enrollmentId, courseId, false, token); var assessment = await AssessmentAsync(courseId, assessmentId, false, false, token);
        var attempt = await AttemptAsync(enrollmentId, assessmentId, attemptId, false, token); return ToDetail(attempt, assessment.Title);
    }

    public async Task<AssessmentAttemptDetailDto> SaveAsync(Guid enrollmentId, Guid courseId, Guid assessmentId, Guid attemptId, SaveAssessmentResponsesRequest request, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        await ContextAsync(enrollmentId, courseId, true, token); var assessment = await AssessmentAsync(courseId, assessmentId, true, true, token);
        var attempt = await AttemptAsync(enrollmentId, assessmentId, attemptId, true, token);
        ApplyResponses(attempt, assessment, request.Responses, false); await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        return ToDetail(attempt, assessment.Title);
    }

    public async Task<AssessmentAttemptDetailDto> SubmitAsync(Guid enrollmentId, Guid courseId, Guid assessmentId, Guid attemptId, SubmitAssessmentAttemptRequest request, CancellationToken token)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var (enrollment, progress) = await ContextAsync(enrollmentId, courseId, true, token); var assessment = await AssessmentAsync(courseId, assessmentId, true, true, token);
        var attempt = await AttemptAsync(enrollmentId, assessmentId, attemptId, true, token);
        if (attempt.Status != AssessmentAttemptStatus.InProgress) { await transaction.CommitAsync(token); return ToDetail(attempt, assessment.Title); }
        if (request.Responses is not null) ApplyResponses(attempt, assessment, request.Responses, true);
        ValidateSubmission(attempt, assessment);
        var answers = attempt.Responses.ToDictionary(x => x.QuestionId, x => (IReadOnlyCollection<Guid>)x.SelectedOptions.Select(o => o.AnswerOptionId).ToList());
        var grade = AssessmentGrader.Grade(assessment.Questions, answers); var passed = grade.Score >= assessment.PassingScorePercentage;
        attempt.Submit(grade.Score, grade.Earned, grade.Total, passed, grade.Grades);
        await db.SaveChangesAsync(token);
        if (passed)
        {
            await completion.CompleteIfEligibleAsync(progress, false, token);
            await CompleteEnrollmentIfEligibleAsync(enrollment, token);
        }
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
        logger.LogInformation("Assessment attempt {AttemptId} submitted with result {Result}", attempt.Id, passed ? "Passed" : "Failed");
        return ToDetail(attempt, assessment.Title);
    }

    private async Task<(CareerEnrollment Enrollment, CourseProgress Progress)> ContextAsync(Guid enrollmentId, Guid courseId, bool requireActive, CancellationToken token)
    {
        var enrollment = await db.CareerEnrollments.Include(x => x.CourseProgressRecords).ThenInclude(x => x.LessonProgressRecords)
            .SingleOrDefaultAsync(x => x.Id == enrollmentId && x.UserId == UserId(), token) ?? throw new NotFoundException("Enrollment was not found.");
        var belongs = await db.PathwayLevelCourses.AsNoTracking().AnyAsync(x => x.CourseId == courseId && db.PathwayLevels.Any(l => l.Id == x.PathwayLevelId && l.CareerPathwayId == enrollment.CareerPathwayId), token);
        if (!belongs) throw new NotFoundException("Course was not found in this enrollment.");
        if (requireActive && enrollment.Status != EnrollmentStatus.Active) throw new ConflictException("Learning activity requires an active enrollment.");
        var coursePublished = await db.Courses.AsNoTracking().AnyAsync(x => x.Id == courseId && x.Status == ContentStatus.Published, token);
        if (!coursePublished) throw new ConflictException("The course is not available.");
        var progress = enrollment.CourseProgressRecords.SingleOrDefault(x => x.CourseId == courseId) ?? throw new ConflictException("The course must be started before an assessment attempt.");
        return (enrollment, progress);
    }

    private async Task<Assessment> AssessmentAsync(Guid courseId, Guid assessmentId, bool includeAnswers, bool requirePublished, CancellationToken token)
    {
        IQueryable<Assessment> query = db.Assessments;
        if (includeAnswers) query = query.Include(x => x.Questions).ThenInclude(x => x.AnswerOptions);
        return await query.SingleOrDefaultAsync(x => x.Id == assessmentId && x.CourseId == courseId && (!requirePublished || x.Status == ContentStatus.Published), token) ?? throw new NotFoundException("Assessment was not found in this course.");
    }

    private async Task<AssessmentAttempt> AttemptAsync(Guid enrollmentId, Guid assessmentId, Guid attemptId, bool tracking, CancellationToken token)
    {
        IQueryable<AssessmentAttempt> query = db.AssessmentAttempts.Include(x => x.Responses).ThenInclude(x => x.SelectedOptions);
        if (!tracking) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(x => x.Id == attemptId && x.UserId == UserId() && x.CareerEnrollmentId == enrollmentId && x.AssessmentId == assessmentId, token) ?? throw new NotFoundException("Assessment attempt was not found.");
    }

    private static void ApplyResponses(AssessmentAttempt attempt, Assessment assessment, IEnumerable<SaveAssessmentResponseRequest> responses, bool submitting)
    {
        var questions = assessment.Questions.ToDictionary(x => x.Id);
        foreach (var item in responses)
        {
            if (!questions.TryGetValue(item.QuestionId, out var question)) throw new ConflictException("A response question does not belong to this assessment.");
            var valid = question.AnswerOptions.Select(x => x.Id).ToHashSet();
            if (item.SelectedAnswerOptionIds.Any(x => !valid.Contains(x))) throw new RequestValidationException("A selected answer option does not belong to its question.");
            if (submitting && item.SelectedAnswerOptionIds.Count == 0) throw new ConflictException("Every submitted response must select an answer.");
            attempt.AddOrUpdateResponse(item.QuestionId, item.SelectedAnswerOptionIds);
        }
    }

    private static void ValidateSubmission(AssessmentAttempt attempt, Assessment assessment)
    {
        if (attempt.Responses.Count != assessment.Questions.Count) throw new ConflictException("Every assessment question must be answered before submission.");
        foreach (var question in assessment.Questions)
        {
            var count = attempt.Responses.Single(x => x.QuestionId == question.Id).SelectedOptions.Count;
            var valid = question.QuestionType == QuestionType.MultipleChoice ? count >= 1 : count == 1;
            if (!valid) throw new ConflictException($"The selected-answer count is invalid for {question.QuestionType}.");
        }
    }

    private async Task CompleteEnrollmentIfEligibleAsync(CareerEnrollment enrollment, CancellationToken token)
    {
        var required = await db.PathwayLevelCourses.AsNoTracking().Where(x => x.IsRequired && db.PathwayLevels.Any(l => l.Id == x.PathwayLevelId && l.CareerPathwayId == enrollment.CareerPathwayId)).Select(x => x.CourseId).Distinct().ToListAsync(token);
        var complete = enrollment.CourseProgressRecords.Where(x => x.CompletedAtUtc is not null).Select(x => x.CourseId).ToHashSet();
        if (required.Count > 0 && required.All(complete.Contains)) enrollment.Complete();
    }

    private static AssessmentAttemptStartDto ToStart(AssessmentAttempt x, Assessment assessment) => new(x.Id, x.AssessmentId, x.AttemptNumber, x.Status, x.StartedAtUtc,
        assessment.Questions.OrderBy(q => q.Order).Select(q => new LearnerAssessmentQuestionDto(q.Id, q.Prompt, q.QuestionType, q.Order, q.Points,
            q.AnswerOptions.OrderBy(o => o.Order).Select(o => new LearnerAnswerOptionDto(o.Id, o.Text, o.Order)).ToList())).ToList());
    private static AssessmentAttemptDetailDto ToDetail(AssessmentAttempt x, string title) => new(x.Id, x.AssessmentId, title, x.AttemptNumber, x.Status, x.StartedAtUtc, x.SubmittedAtUtc,
        x.ScorePercentage, x.PointsEarned, x.TotalPoints, x.Status == AssessmentAttemptStatus.Passed,
        x.Responses.OrderBy(r => r.CreatedAtUtc).Select(r => new LearnerAssessmentResponseDto(r.QuestionId, r.SelectedOptions.Select(o => o.AnswerOptionId).ToList(), r.IsCorrect, r.PointsAwarded)).ToList());
    private Guid UserId() => currentUser.IsAuthenticated && currentUser.UserId.HasValue ? currentUser.UserId.Value : throw new UnauthorizedException();
}
