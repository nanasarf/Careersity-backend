using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.CurriculumActivities.Dtos;
using Careersity.Application.CurriculumActivities.Requests;
using Careersity.Domain.Assessments;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.CurriculumActivities.Services;

public sealed class AnswerOptionService(ICareersityDbContext db) : IAnswerOptionService
{
    public async Task<AnswerOptionAdminDto> AddAsync(Guid assessmentId, Guid questionId, AddAnswerOptionRequest request, CancellationToken token)
    {
        var (assessment, question) = await FindAsync(assessmentId, questionId, token); AssessmentService.RequireDraft(assessment.Status);
        if (question.AnswerOptions.Any(x => x.Order == request.Order)) throw new ConflictException("Answer option order must be unique within the question.");
        if (question.AnswerOptions.Any(x => string.Equals(x.Text, request.Text.Trim(), StringComparison.OrdinalIgnoreCase))) throw new ConflictException("Answer option text must be unique within the question.");
        var option = new AnswerOption(question.Id, request.Text, request.IsCorrect, request.Order);
        question.AddAnswerOption(option); db.AnswerOptions.Add(option); await db.SaveChangesAsync(token); return ToDto(option);
    }
    public async Task<AnswerOptionAdminDto> UpdateAsync(Guid assessmentId, Guid questionId, Guid optionId, UpdateAnswerOptionRequest request, CancellationToken token)
    {
        var (assessment, question) = await FindAsync(assessmentId, questionId, token); AssessmentService.RequireDraft(assessment.Status);
        var option = question.AnswerOptions.SingleOrDefault(x => x.Id == optionId) ?? throw new NotFoundException("Answer option was not found.");
        if (question.AnswerOptions.Any(x => x.Id != optionId && x.Order == request.Order)) throw new ConflictException("Answer option order must be unique within the question.");
        if (question.AnswerOptions.Any(x => x.Id != optionId && string.Equals(x.Text, request.Text.Trim(), StringComparison.OrdinalIgnoreCase))) throw new ConflictException("Answer option text must be unique within the question.");
        question.UpdateAnswerOptionText(option.Id, request.Text); option.SetCorrect(request.IsCorrect);
        if (option.Order != request.Order) question.ReorderAnswerOption(option.Id, request.Order);
        await db.SaveChangesAsync(token); return ToDto(option);
    }
    public async Task RemoveAsync(Guid assessmentId, Guid questionId, Guid optionId, CancellationToken token)
    {
        var (assessment, question) = await FindAsync(assessmentId, questionId, token); AssessmentService.RequireDraft(assessment.Status);
        if (question.AnswerOptions.All(x => x.Id != optionId)) throw new NotFoundException("Answer option was not found.");
        question.RemoveAnswerOption(optionId); await db.SaveChangesAsync(token);
    }
    public async Task ReorderAsync(Guid assessmentId, Guid questionId, ReorderAnswerOptionsRequest request, CancellationToken token)
    {
        var (assessment, question) = await FindAsync(assessmentId, questionId, token); AssessmentService.RequireDraft(assessment.Status);
        var items = request.AnswerOptions.ToList(); QuestionService.ValidateComplete(items.Select(x => x.AnswerOptionId), items.Select(x => x.Order), question.AnswerOptions.Select(x => x.Id), "answer option");
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        var temporary = (question.AnswerOptions.Count == 0 ? 0 : question.AnswerOptions.Max(x => x.Order)) + question.AnswerOptions.Count + 1;
        var index = 0; foreach (var option in question.AnswerOptions.ToList()) question.ReorderAnswerOption(option.Id, temporary + index++);
        await db.SaveChangesAsync(token); foreach (var item in items) question.ReorderAnswerOption(item.AnswerOptionId, item.Order);
        await db.SaveChangesAsync(token); await transaction.CommitAsync(token);
    }
    private async Task<(Assessment Assessment, Question Question)> FindAsync(Guid assessmentId, Guid questionId, CancellationToken token)
    {
        var assessment = await db.Assessments.Include(x => x.Questions).ThenInclude(x => x.AnswerOptions).SingleOrDefaultAsync(x => x.Id == assessmentId, token)
            ?? throw new NotFoundException("Assessment was not found.");
        var question = assessment.Questions.SingleOrDefault(x => x.Id == questionId) ?? throw new NotFoundException("Question was not found."); return (assessment, question);
    }
    internal static AnswerOptionAdminDto ToDto(AnswerOption x) => new(x.Id, x.QuestionId, x.Text, x.IsCorrect, x.Order, x.CreatedAtUtc, x.UpdatedAtUtc);
}
