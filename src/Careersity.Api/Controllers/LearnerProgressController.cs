using Careersity.Application.LearningProgress.Requests;
using Careersity.Application.LearningProgress.Services;
using Careersity.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Careersity.Api.Controllers;

/// <summary>Current-user career enrollment and lesson-based learning progress.</summary>
[ApiController, Authorize, Route("api/me/career-enrollments"), Tags("Learner Progress")]
public sealed class LearnerProgressController(ILearningProgressService progress) : ControllerBase
{
    [HttpPost] public async Task<IActionResult> Enroll(EnrollInCareerRequest request, CancellationToken token) { var result = await progress.EnrollAsync(request, token); return CreatedAtAction(nameof(Get), new { enrollmentId = result.Id }, result); }
    [HttpGet] public async Task<IActionResult> List([FromQuery] EnrollmentStatus? status = null, [FromQuery] bool includeHistory = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken token = default) => Ok(await progress.ListAsync(new(status, includeHistory, page, pageSize), token));
    [HttpGet("{enrollmentId:guid}")] public async Task<IActionResult> Get(Guid enrollmentId, CancellationToken token) => Ok(await progress.GetAsync(enrollmentId, token));
    [HttpPost("{enrollmentId:guid}/pause")] public async Task<IActionResult> Pause(Guid enrollmentId, CancellationToken token) { await progress.PauseAsync(enrollmentId, token); return NoContent(); }
    [HttpPost("{enrollmentId:guid}/resume")] public async Task<IActionResult> Resume(Guid enrollmentId, CancellationToken token) { await progress.ResumeAsync(enrollmentId, token); return NoContent(); }
    [HttpPost("{enrollmentId:guid}/withdraw")] public async Task<IActionResult> Withdraw(Guid enrollmentId, CancellationToken token) { await progress.WithdrawAsync(enrollmentId, token); return NoContent(); }
    [HttpGet("{enrollmentId:guid}/courses/{courseId:guid}")] public async Task<IActionResult> Course(Guid enrollmentId, Guid courseId, CancellationToken token) => Ok(await progress.GetCourseAsync(enrollmentId, courseId, token));
    [HttpPost("{enrollmentId:guid}/courses/{courseId:guid}/start")] public async Task<IActionResult> StartCourse(Guid enrollmentId, Guid courseId, CancellationToken token) => Ok(await progress.StartCourseAsync(enrollmentId, courseId, token));
    [HttpPost("{enrollmentId:guid}/courses/{courseId:guid}/complete")] public async Task<IActionResult> CompleteCourse(Guid enrollmentId, Guid courseId, CancellationToken token) => Ok(await progress.CompleteCourseAsync(enrollmentId, courseId, token));
    [HttpGet("{enrollmentId:guid}/courses/{courseId:guid}/lessons/{lessonId:guid}")] public async Task<IActionResult> Lesson(Guid enrollmentId, Guid courseId, Guid lessonId, CancellationToken token) => Ok(await progress.GetLessonAsync(enrollmentId, courseId, lessonId, token));
    [HttpPost("{enrollmentId:guid}/courses/{courseId:guid}/lessons/{lessonId:guid}/start")] public async Task<IActionResult> StartLesson(Guid enrollmentId, Guid courseId, Guid lessonId, CancellationToken token) => Ok(await progress.StartLessonAsync(enrollmentId, courseId, lessonId, token));
    [HttpPost("{enrollmentId:guid}/courses/{courseId:guid}/lessons/{lessonId:guid}/complete")] public async Task<IActionResult> CompleteLesson(Guid enrollmentId, Guid courseId, Guid lessonId, CancellationToken token) => Ok(await progress.CompleteLessonAsync(enrollmentId, courseId, lessonId, token));
}
