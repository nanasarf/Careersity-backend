using Careersity.Api.Infrastructure;
using Careersity.Application.CurriculumActivities.Requests;
using Careersity.Application.CurriculumActivities.Services;
using Careersity.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Careersity.Api.Controllers;

/// <summary>Administrator-only assessment authoring. Published and Archived assessments are immutable.</summary>
[ApiController, Authorize(Policy = SecurityPolicies.AdministratorOnly), Route("api/admin/assessments"), Tags("Admin Assessments")]
public sealed class AdminAssessmentsController(IAssessmentService assessments, IQuestionService questions, IAnswerOptionService options) : ControllerBase
{
    [HttpPost] public async Task<IActionResult> Create(CreateAssessmentRequest request, CancellationToken token) { var result = await assessments.CreateAsync(request, token); return CreatedAtAction(nameof(Get), new { assessmentId = result.Id }, result); }
    [HttpGet] public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] Guid? courseId = null, [FromQuery] ContentStatus? status = null, CancellationToken token = default) => Ok(await assessments.ListAdminAsync(new(page, pageSize, search, courseId, status), token));
    [HttpGet("{assessmentId:guid}")] public async Task<IActionResult> Get(Guid assessmentId, CancellationToken token) => Ok(await assessments.GetAdminAsync(assessmentId, token));
    [HttpPut("{assessmentId:guid}")] public async Task<IActionResult> Update(Guid assessmentId, UpdateAssessmentRequest request, CancellationToken token) => Ok(await assessments.UpdateAsync(assessmentId, request, token));
    [HttpPost("{assessmentId:guid}/publish")] public async Task<IActionResult> Publish(Guid assessmentId, CancellationToken token) { await assessments.PublishAsync(assessmentId, token); return NoContent(); }
    [HttpPost("{assessmentId:guid}/archive")] public async Task<IActionResult> Archive(Guid assessmentId, CancellationToken token) { await assessments.ArchiveAsync(assessmentId, token); return NoContent(); }
    [HttpPost("{assessmentId:guid}/questions")] public async Task<IActionResult> AddQuestion(Guid assessmentId, AddQuestionRequest request, CancellationToken token) { var result = await questions.AddAsync(assessmentId, request, token); return CreatedAtAction(nameof(GetQuestion), new { assessmentId, questionId = result.Id }, result); }
    [HttpGet("{assessmentId:guid}/questions/{questionId:guid}")] public async Task<IActionResult> GetQuestion(Guid assessmentId, Guid questionId, CancellationToken token) => Ok(await questions.GetAdminAsync(assessmentId, questionId, token));
    [HttpPut("{assessmentId:guid}/questions/{questionId:guid}")] public async Task<IActionResult> UpdateQuestion(Guid assessmentId, Guid questionId, UpdateQuestionRequest request, CancellationToken token) => Ok(await questions.UpdateAsync(assessmentId, questionId, request, token));
    [HttpDelete("{assessmentId:guid}/questions/{questionId:guid}")] public async Task<IActionResult> RemoveQuestion(Guid assessmentId, Guid questionId, CancellationToken token) { await questions.RemoveAsync(assessmentId, questionId, token); return NoContent(); }
    [HttpPut("{assessmentId:guid}/questions/reorder")] public async Task<IActionResult> ReorderQuestions(Guid assessmentId, ReorderQuestionsRequest request, CancellationToken token) { await questions.ReorderAsync(assessmentId, request, token); return NoContent(); }
    [HttpPost("{assessmentId:guid}/questions/{questionId:guid}/answer-options")] public async Task<IActionResult> AddOption(Guid assessmentId, Guid questionId, AddAnswerOptionRequest request, CancellationToken token) => StatusCode(201, await options.AddAsync(assessmentId, questionId, request, token));
    [HttpPut("{assessmentId:guid}/questions/{questionId:guid}/answer-options/{answerOptionId:guid}")] public async Task<IActionResult> UpdateOption(Guid assessmentId, Guid questionId, Guid answerOptionId, UpdateAnswerOptionRequest request, CancellationToken token) => Ok(await options.UpdateAsync(assessmentId, questionId, answerOptionId, request, token));
    [HttpDelete("{assessmentId:guid}/questions/{questionId:guid}/answer-options/{answerOptionId:guid}")] public async Task<IActionResult> RemoveOption(Guid assessmentId, Guid questionId, Guid answerOptionId, CancellationToken token) { await options.RemoveAsync(assessmentId, questionId, answerOptionId, token); return NoContent(); }
    [HttpPut("{assessmentId:guid}/questions/{questionId:guid}/answer-options/reorder")] public async Task<IActionResult> ReorderOptions(Guid assessmentId, Guid questionId, ReorderAnswerOptionsRequest request, CancellationToken token) { await options.ReorderAsync(assessmentId, questionId, request, token); return NoContent(); }
}
