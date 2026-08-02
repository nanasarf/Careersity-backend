using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.CurriculumActivities.Dtos;
using Careersity.Application.CurriculumActivities.Requests;
using Careersity.Domain.Assessments;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.CurriculumActivities.Services;

public sealed class QuestionService(ICareersityDbContext db) : IQuestionService
{
    public async Task<QuestionAdminDto> AddAsync(Guid assessmentId, AddQuestionRequest request, CancellationToken token)
    {
        var assessment = await FindAssessmentAsync(assessmentId, token); AssessmentService.RequireDraft(assessment.Status);
        if (assessment.Questions.Any(x => x.Order == request.Order)) throw new ConflictException("Question order must be unique within the assessment.");
        var question = new Question(assessment.Id, request.Prompt, request.QuestionType, request.Order, request.Points);
        assessment.AddQuestion(question); db.Questions.Add(question); await db.SaveChangesAsync(token); return ToDto(question);
    }
    public async Task<QuestionAdminDto> UpdateAsync(Guid assessmentId, Guid questionId, UpdateQuestionRequest request, CancellationToken token)
    {
        var assessment = await FindAssessmentAsync(assessmentId, token); AssessmentService.RequireDraft(assessment.Status);
        var question = assessment.Questions.SingleOrDefault(x => x.Id == questionId) ?? throw new NotFoundException("Question was not found.");
        if (assessment.Questions.Any(x => x.Id != questionId && x.Order == request.Order)) throw new ConflictException("Question order must be unique within the assessment.");
        question.UpdatePrompt(request.Prompt); question.ChangeType(request.QuestionType); question.ChangePoints(request.Points);
        if (question.Order != request.Order) assessment.ReorderQuestion(question.Id, request.Order);
        await db.SaveChangesAsync(token); return ToDto(question);
    }
    public async Task RemoveAsync(Guid assessmentId, Guid questionId, CancellationToken token)
    {
        var assessment = await FindAssessmentAsync(assessmentId, token); AssessmentService.RequireDraft(assessment.Status);
        if (assessment.Questions.All(x => x.Id != questionId)) throw new NotFoundException("Question was not found.");
        assessment.RemoveQuestion(questionId); await db.SaveChangesAsync(token);
    }
    public async Task ReorderAsync(Guid assessmentId, ReorderQuestionsRequest request, CancellationToken token)
    {
        var assessment = await FindAssessmentAsync(assessmentId, token); AssessmentService.RequireDraft(assessment.Status);
        var items = request.Questions.ToList(); ValidateComplete(items.Select(x => x.QuestionId), items.Select(x => x.Order), assessment.Questions.Select(x => x.Id), "question");
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var temporary = (assessment.Questions.Count == 0 ? 0 : assessment.Questions.Max(x => x.Order)) + assessment.Questions.Count + 1;
        var index = 0; foreach (var question in assessment.Questions.ToList()) assessment.ReorderQuestion(question.Id, temporary + index++);
        await db.SaveChangesAsync(token); foreach (var item in items) assessment.ReorderQuestion(item.QuestionId, item.Order);
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
    }
    public async Task<QuestionAdminDto> GetAdminAsync(Guid assessmentId, Guid questionId, CancellationToken token)
    {
        if (!await db.Assessments.AsNoTracking().AnyAsync(x => x.Id == assessmentId, token)) throw new NotFoundException("Assessment was not found.");
        var question = await db.Questions.AsNoTracking().Include(x => x.AnswerOptions).SingleOrDefaultAsync(x => x.Id == questionId && x.AssessmentId == assessmentId, token)
            ?? throw new NotFoundException("Question was not found."); return ToDto(question);
    }
    internal static void ValidateComplete(IEnumerable<Guid> ids, IEnumerable<int> orders, IEnumerable<Guid> currentIds, string child)
    {
        var idList = ids.ToList(); var orderList = orders.ToList(); var current = currentIds.ToHashSet();
        if (idList.Count != current.Count || idList.Distinct().Count() != idList.Count || idList.Any(x => !current.Contains(x))) throw new ConflictException($"The reorder request must contain every {child} exactly once.");
        if (orderList.Distinct().Count() != orderList.Count || !orderList.OrderBy(x => x).SequenceEqual(Enumerable.Range(0, orderList.Count))) throw new ConflictException($"{char.ToUpperInvariant(child[0]) + child[1..]} orders must be unique and contiguous from zero.");
    }
    private async Task<Assessment> FindAssessmentAsync(Guid id, CancellationToken token) => await db.Assessments.Include(x => x.Questions).ThenInclude(x => x.AnswerOptions).SingleOrDefaultAsync(x => x.Id == id, token) ?? throw new NotFoundException("Assessment was not found.");
    internal static QuestionAdminDto ToDto(Question x) => new(x.Id, x.AssessmentId, x.Prompt, x.QuestionType, x.Order, x.Points,
        x.AnswerOptions.OrderBy(o => o.Order).Select(AnswerOptionService.ToDto).ToList(), x.CreatedAtUtc, x.UpdatedAtUtc);
}
