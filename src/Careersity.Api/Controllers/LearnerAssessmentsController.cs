using Careersity.Application.AssessmentAttempts.Requests;
using Careersity.Application.AssessmentAttempts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Careersity.Api.Controllers;

/// <summary>Current-user assessment attempts for enrolled and started courses. Multiple-choice grading requires an exact match; answer keys are never returned.</summary>
[ApiController, Authorize, Route("api/me/career-enrollments/{enrollmentId:guid}/courses/{courseId:guid}/assessments"), Tags("Learner Assessments")]
public sealed class LearnerAssessmentsController(IAssessmentAttemptService attempts) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid enrollmentId, Guid courseId, CancellationToken token) => Ok(await attempts.ListAsync(enrollmentId, courseId, token));

    [HttpPost("{assessmentId:guid}/attempts")]
    public async Task<IActionResult> Start(Guid enrollmentId, Guid courseId, Guid assessmentId, CancellationToken token)
    {
        var result = await attempts.StartAsync(enrollmentId, courseId, assessmentId, token);
        return result.Created ? CreatedAtAction(nameof(Get), new { enrollmentId, courseId, assessmentId, attemptId = result.Attempt.AttemptId }, result.Attempt) : Ok(result.Attempt);
    }

    [HttpGet("{assessmentId:guid}/attempts")]
    public async Task<IActionResult> History(Guid enrollmentId, Guid courseId, Guid assessmentId, CancellationToken token) => Ok(await attempts.HistoryAsync(enrollmentId, courseId, assessmentId, token));

    [HttpGet("{assessmentId:guid}/attempts/{attemptId:guid}")]
    public async Task<IActionResult> Get(Guid enrollmentId, Guid courseId, Guid assessmentId, Guid attemptId, CancellationToken token) => Ok(await attempts.GetAsync(enrollmentId, courseId, assessmentId, attemptId, token));

    [HttpPut("{assessmentId:guid}/attempts/{attemptId:guid}/responses")]
    public async Task<IActionResult> Save(Guid enrollmentId, Guid courseId, Guid assessmentId, Guid attemptId, SaveAssessmentResponsesRequest request, CancellationToken token) => Ok(await attempts.SaveAsync(enrollmentId, courseId, assessmentId, attemptId, request, token));

    [HttpPost("{assessmentId:guid}/attempts/{attemptId:guid}/submit")]
    public async Task<IActionResult> Submit(Guid enrollmentId, Guid courseId, Guid assessmentId, Guid attemptId, SubmitAssessmentAttemptRequest? request, CancellationToken token) => Ok(await attempts.SubmitAsync(enrollmentId, courseId, assessmentId, attemptId, request ?? new(), token));
}
