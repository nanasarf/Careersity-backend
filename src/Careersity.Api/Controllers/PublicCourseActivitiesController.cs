using Careersity.Application.CurriculumActivities.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Careersity.Api.Controllers;

/// <summary>Published course activity summaries and project instructions. Assessment answer keys are never exposed.</summary>
[ApiController, AllowAnonymous, Route("api/courses/{courseSlug}"), Tags("Public Course Activities")]
public sealed class PublicCourseActivitiesController(IAssessmentService assessments, IProjectService projects) : ControllerBase
{
    [HttpGet("assessments")] public async Task<IActionResult> Assessments(string courseSlug, CancellationToken token) => Ok(await assessments.ListPublishedAsync(courseSlug, token));
    [HttpGet("projects")] public async Task<IActionResult> Projects(string courseSlug, CancellationToken token) => Ok(await projects.ListPublishedAsync(courseSlug, token));
    [HttpGet("projects/{projectId:guid}")] public async Task<IActionResult> Project(string courseSlug, Guid projectId, CancellationToken token) => Ok(await projects.GetPublishedAsync(courseSlug, projectId, token));
}
