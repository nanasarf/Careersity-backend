using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.Common.Models;
using Careersity.Application.CurriculumActivities.Dtos;
using Careersity.Application.CurriculumActivities.Requests;
using Careersity.Domain.Assessments;
using Careersity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.CurriculumActivities.Services;

public sealed class AssessmentService(ICareersityDbContext db) : IAssessmentService
{
    public async Task<AssessmentAdminDetailDto> CreateAsync(CreateAssessmentRequest request, CancellationToken cancellationToken)
    {
        var course = await db.Courses.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.CourseId, cancellationToken)
            ?? throw new NotFoundException("Course was not found.");
        var assessment = new Assessment(course.Id, request.Title, request.PassingScorePercentage, request.MaximumAttempts, request.Description);
        db.Assessments.Add(assessment); await db.SaveChangesAsync(cancellationToken); return ToDetail(assessment, course.Title, []);
    }
    public async Task<AssessmentAdminDetailDto> UpdateAsync(Guid id, UpdateAssessmentRequest request, CancellationToken cancellationToken)
    {
        var assessment = await FindAsync(id, false, cancellationToken); RequireDraft(assessment.Status);
        assessment.UpdateDetails(request.Title, request.Description); assessment.ChangePassingScore(request.PassingScorePercentage);
        assessment.ChangeMaximumAttempts(request.MaximumAttempts); await db.SaveChangesAsync(cancellationToken);
        return await GetAdminAsync(id, cancellationToken);
    }
    public async Task PublishAsync(Guid id, CancellationToken cancellationToken)
    {
        var assessment = await FindAsync(id, true, cancellationToken); RequireDraft(assessment.Status);
        var course = await db.Courses.AsNoTracking().SingleOrDefaultAsync(x => x.Id == assessment.CourseId, cancellationToken)
            ?? throw new NotFoundException("Course was not found.");
        if (course.Status != ContentStatus.Published) throw new ConflictException("The parent course must be published before the assessment.");
        var questions = assessment.Questions.OrderBy(x => x.Order).ToList();
        if (!questions.Select(x => x.Order).SequenceEqual(Enumerable.Range(0, questions.Count)))
            throw new ConflictException("Question orders must be contiguous from zero.");
        foreach (var question in questions)
        {
            var options = question.AnswerOptions.OrderBy(x => x.Order).ToList();
            if (!options.Select(x => x.Order).SequenceEqual(Enumerable.Range(0, options.Count)))
                throw new ConflictException($"Answer option orders must be contiguous for question {question.Order}.");
            if (!question.IsValid()) throw new ConflictException($"Question {question.Order} is not valid for its type.");
        }
        if (questions.Sum(x => x.Points) <= 0) throw new ConflictException("Assessment total points must be positive.");
        assessment.Publish(); await db.SaveChangesAsync(cancellationToken);
    }
    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
    { var assessment = await FindAsync(id, false, cancellationToken); assessment.Archive(); await db.SaveChangesAsync(cancellationToken); }
    public async Task<AssessmentAdminDetailDto> GetAdminAsync(Guid id, CancellationToken cancellationToken)
    {
        var assessment = await db.Assessments.AsNoTracking().Include(x => x.Questions).ThenInclude(x => x.AnswerOptions)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Assessment was not found.");
        var courseTitle = await db.Courses.AsNoTracking().Where(x => x.Id == assessment.CourseId).Select(x => x.Title).SingleAsync(cancellationToken);
        return ToDetail(assessment, courseTitle, assessment.Questions);
    }
    public async Task<PagedResult<AssessmentListItemDto>> ListAdminAsync(AssessmentQuery request, CancellationToken cancellationToken)
    {
        var query = db.Assessments.AsNoTracking(); if (request.CourseId.HasValue) query = query.Where(x => x.CourseId == request.CourseId);
        if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.Search)) { var search = request.Search.Trim().ToLowerInvariant(); query = query.Where(x => x.Title.ToLower().Contains(search) || (x.Description != null && x.Description.ToLower().Contains(search))); }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Title).Skip((request.ValidatedPage - 1) * request.ValidatedPageSize).Take(request.ValidatedPageSize)
            .Select(x => new AssessmentListItemDto(x.Id, x.CourseId, x.Title, x.Description, x.PassingScorePercentage,
                x.MaximumAttempts, x.Status, x.Questions.Count, x.Questions.Sum(q => q.Points))).ToListAsync(cancellationToken);
        return new(items, request.ValidatedPage, request.ValidatedPageSize, total);
    }
    public async Task<IReadOnlyCollection<PublicAssessmentSummaryDto>> ListPublishedAsync(string courseSlug, CancellationToken cancellationToken)
    {
        var slug = courseSlug.Trim().ToLowerInvariant();
        var course = await db.Courses.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == slug && x.Status == ContentStatus.Published, cancellationToken)
            ?? throw new NotFoundException("Published course was not found.");
        return await db.Assessments.AsNoTracking().Where(x => x.CourseId == course.Id && x.Status == ContentStatus.Published)
            .OrderBy(x => x.Title).Select(x => new PublicAssessmentSummaryDto(x.Id, x.Title, x.Description,
                x.PassingScorePercentage, x.MaximumAttempts, x.Questions.Count, x.Questions.Sum(q => q.Points))).ToListAsync(cancellationToken);
    }
    internal static void RequireDraft(ContentStatus status) { if (status != ContentStatus.Draft) throw new ConflictException("The assessment is immutable after publication or archiving."); }
    internal async Task<Assessment> FindAsync(Guid id, bool includeChildren, CancellationToken token)
    { IQueryable<Assessment> query = db.Assessments; if (includeChildren) query = query.Include(x => x.Questions).ThenInclude(x => x.AnswerOptions); return await query.SingleOrDefaultAsync(x => x.Id == id, token) ?? throw new NotFoundException("Assessment was not found."); }
    internal static AssessmentAdminDetailDto ToDetail(Assessment x, string courseTitle, IEnumerable<Question> questions) =>
        new(x.Id, x.CourseId, courseTitle, x.Title, x.Description, x.PassingScorePercentage, x.MaximumAttempts, x.Status,
            questions.OrderBy(q => q.Order).Select(QuestionService.ToDto).ToList(), x.CreatedAtUtc, x.UpdatedAtUtc);
}
