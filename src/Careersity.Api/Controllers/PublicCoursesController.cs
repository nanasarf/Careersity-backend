using Careersity.Application.LearningContent.Requests;
using Careersity.Application.LearningContent.Services;
using Careersity.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Careersity.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/courses"), Tags("Public Learning Content")]
public sealed class PublicCoursesController(ICourseService courses, ILessonService lessons) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] CourseDifficulty? difficulty = null,
        [FromQuery] Guid? skillId = null, CancellationToken cancellationToken = default) =>
        Ok(await courses.ListPublishedAsync(new(page, pageSize, search, difficulty, null, skillId), cancellationToken));
    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug, CancellationToken cancellationToken) => Ok(await courses.GetPublishedAsync(slug, cancellationToken));
    [HttpGet("{courseSlug}/lessons/{lessonSlug}")]
    public async Task<IActionResult> GetLesson(string courseSlug, string lessonSlug, CancellationToken cancellationToken) =>
        Ok(await lessons.GetPublishedAsync(courseSlug, lessonSlug, cancellationToken));
}
