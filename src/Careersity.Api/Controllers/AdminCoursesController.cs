using Careersity.Api.Infrastructure;
using Careersity.Application.LearningContent.Requests;
using Careersity.Application.LearningContent.Services;
using Careersity.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Careersity.Api.Controllers;

[ApiController, Authorize(Policy = SecurityPolicies.AdministratorOnly), Route("api/admin/courses"), Tags("Admin Courses")]
public sealed class AdminCoursesController(ICourseService courses, ILessonService lessons,
    ICoursePrerequisiteService prerequisites, ICourseSkillService skills) : ControllerBase
{
    [HttpPost] public async Task<IActionResult> Create(CreateCourseRequest request, CancellationToken token)
    { var result = await courses.CreateAsync(request, token); return CreatedAtAction(nameof(Get), new { id = result.Id }, result); }
    [HttpGet] public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] CourseDifficulty? difficulty = null,
        [FromQuery] ContentStatus? status = null, [FromQuery] Guid? skillId = null, CancellationToken token = default) =>
        Ok(await courses.ListAdminAsync(new(page, pageSize, search, difficulty, status, skillId), token));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken token) => Ok(await courses.GetAdminAsync(id, token));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, UpdateCourseRequest request, CancellationToken token) => Ok(await courses.UpdateAsync(id, request, token));
    [HttpPost("{id:guid}/publish")] public async Task<IActionResult> Publish(Guid id, CancellationToken token) { await courses.PublishAsync(id, token); return NoContent(); }
    [HttpPost("{id:guid}/archive")] public async Task<IActionResult> Archive(Guid id, CancellationToken token) { await courses.ArchiveAsync(id, token); return NoContent(); }

    [HttpPost("{courseId:guid}/lessons")] public async Task<IActionResult> AddLesson(Guid courseId, AddLessonRequest request, CancellationToken token)
    { var result = await lessons.AddAsync(courseId, request, token); return CreatedAtAction(nameof(GetLesson), new { courseId, lessonId = result.Id }, result); }
    [HttpGet("{courseId:guid}/lessons/{lessonId:guid}")] public async Task<IActionResult> GetLesson(Guid courseId, Guid lessonId, CancellationToken token) => Ok(await lessons.GetAdminAsync(courseId, lessonId, token));
    [HttpPut("{courseId:guid}/lessons/{lessonId:guid}")] public async Task<IActionResult> UpdateLesson(Guid courseId, Guid lessonId, UpdateLessonRequest request, CancellationToken token) => Ok(await lessons.UpdateAsync(courseId, lessonId, request, token));
    [HttpDelete("{courseId:guid}/lessons/{lessonId:guid}")] public async Task<IActionResult> RemoveLesson(Guid courseId, Guid lessonId, CancellationToken token) { await lessons.RemoveAsync(courseId, lessonId, token); return NoContent(); }
    [HttpPut("{courseId:guid}/lessons/reorder")] public async Task<IActionResult> ReorderLessons(Guid courseId, ReorderLessonsRequest request, CancellationToken token) { await lessons.ReorderAsync(courseId, request, token); return NoContent(); }

    [HttpPost("{courseId:guid}/prerequisites")] public async Task<IActionResult> AddPrerequisite(Guid courseId, AddCoursePrerequisiteRequest request, CancellationToken token) => StatusCode(201, await prerequisites.AddAsync(courseId, request, token));
    [HttpPut("{courseId:guid}/prerequisites/{prerequisiteId:guid}")] public async Task<IActionResult> UpdatePrerequisite(Guid courseId, Guid prerequisiteId, UpdateCoursePrerequisiteRequest request, CancellationToken token) => Ok(await prerequisites.UpdateAsync(courseId, prerequisiteId, request, token));
    [HttpDelete("{courseId:guid}/prerequisites/{prerequisiteId:guid}")] public async Task<IActionResult> RemovePrerequisite(Guid courseId, Guid prerequisiteId, CancellationToken token) { await prerequisites.RemoveAsync(courseId, prerequisiteId, token); return NoContent(); }

    [HttpPost("{courseId:guid}/skills")] public async Task<IActionResult> AddSkill(Guid courseId, AddCourseSkillRequest request, CancellationToken token) => StatusCode(201, await skills.AddAsync(courseId, request, token));
    [HttpPut("{courseId:guid}/skills/{courseSkillId:guid}")] public async Task<IActionResult> UpdateSkill(Guid courseId, Guid courseSkillId, UpdateCourseSkillRequest request, CancellationToken token) => Ok(await skills.UpdateAsync(courseId, courseSkillId, request, token));
    [HttpDelete("{courseId:guid}/skills/{courseSkillId:guid}")] public async Task<IActionResult> RemoveSkill(Guid courseId, Guid courseSkillId, CancellationToken token) { await skills.RemoveAsync(courseId, courseSkillId, token); return NoContent(); }
}
