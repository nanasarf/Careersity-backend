using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.LearningContent.Dtos;
using Careersity.Application.LearningContent.Requests;
using Careersity.Domain.Courses;
using Careersity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.LearningContent.Services;

public sealed class LessonService(ICareersityDbContext db) : ILessonService
{
    public async Task<LessonDto> AddAsync(Guid courseId, AddLessonRequest request, CancellationToken cancellationToken)
    {
        var course = await FindCourseAsync(courseId, cancellationToken); SkillService.RequireDraft(course.Status, "course");
        var slug = SkillService.Normalize(request.Slug);
        if (course.Lessons.Any(x => x.Slug == slug)) throw new ConflictException("Lesson slug must be unique within the course.");
        if (course.Lessons.Any(x => x.Order == request.Order)) throw new ConflictException("Lesson order must be unique within the course.");
        var lesson = new Lesson(course.Id, request.Title, slug, request.ContentType, request.EstimatedDurationMinutes,
            request.Order, request.Summary, request.Content, request.ExternalResourceUrl, request.IsRequired);
        course.AddLesson(lesson); db.Lessons.Add(lesson); await db.SaveChangesAsync(cancellationToken); return ToDto(lesson);
    }
    public async Task<LessonDto> UpdateAsync(Guid courseId, Guid lessonId, UpdateLessonRequest request, CancellationToken cancellationToken)
    {
        var course = await FindCourseAsync(courseId, cancellationToken); SkillService.RequireDraft(course.Status, "course");
        var lesson = course.Lessons.SingleOrDefault(x => x.Id == lessonId) ?? throw new NotFoundException("Lesson was not found.");
        var slug = SkillService.Normalize(request.Slug);
        if (course.Lessons.Any(x => x.Id != lessonId && x.Slug == slug)) throw new ConflictException("Lesson slug must be unique within the course.");
        if (course.Lessons.Any(x => x.Id != lessonId && x.Order == request.Order)) throw new ConflictException("Lesson order must be unique within the course.");
        lesson.UpdateContentAndMetadata(request.Title, slug, request.ContentType, request.EstimatedDurationMinutes,
            request.Summary, request.Content, request.ExternalResourceUrl); lesson.SetRequired(request.IsRequired);
        if (lesson.Order != request.Order) course.ReorderLesson(lesson.Id, request.Order);
        await db.SaveChangesAsync(cancellationToken); return ToDto(lesson);
    }
    public async Task RemoveAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken)
    {
        var course = await FindCourseAsync(courseId, cancellationToken); SkillService.RequireDraft(course.Status, "course");
        if (course.Lessons.All(x => x.Id != lessonId)) throw new NotFoundException("Lesson was not found.");
        course.RemoveLesson(lessonId); await db.SaveChangesAsync(cancellationToken);
    }
    public async Task ReorderAsync(Guid courseId, ReorderLessonsRequest request, CancellationToken cancellationToken)
    {
        var course = await FindCourseAsync(courseId, cancellationToken); SkillService.RequireDraft(course.Status, "course");
        var items = request.Lessons.ToList();
        if (items.Count != course.Lessons.Count || items.Select(x => x.LessonId).Distinct().Count() != items.Count ||
            items.Any(x => course.Lessons.All(y => y.Id != x.LessonId)))
            throw new ConflictException("The reorder request must contain every lesson exactly once.");
        if (items.Select(x => x.Order).Distinct().Count() != items.Count ||
            !items.Select(x => x.Order).OrderBy(x => x).SequenceEqual(Enumerable.Range(0, items.Count)))
            throw new ConflictException("Lesson orders must be unique and contiguous from zero.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var temporaryStart = (course.Lessons.Count == 0 ? 0 : course.Lessons.Max(x => x.Order)) + course.Lessons.Count + 1;
        var index = 0; foreach (var lesson in course.Lessons.ToList()) course.ReorderLesson(lesson.Id, temporaryStart + index++);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var item in items) course.ReorderLesson(item.LessonId, item.Order);
        await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
    }
    public async Task<LessonDto> GetAdminAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken)
    {
        if (!await db.Courses.AsNoTracking().AnyAsync(x => x.Id == courseId, cancellationToken)) throw new NotFoundException("Course was not found.");
        return ToDto(await db.Lessons.AsNoTracking().SingleOrDefaultAsync(x => x.Id == lessonId && x.CourseId == courseId, cancellationToken)
            ?? throw new NotFoundException("Lesson was not found."));
    }
    public async Task<LessonDto> GetPublishedAsync(string courseSlug, string lessonSlug, CancellationToken cancellationToken)
    {
        var normalizedCourse = SkillService.Normalize(courseSlug); var normalizedLesson = SkillService.Normalize(lessonSlug);
        var lesson = await (from item in db.Lessons.AsNoTracking() join course in db.Courses.AsNoTracking() on item.CourseId equals course.Id
            where course.Slug == normalizedCourse && course.Status == ContentStatus.Published && item.Slug == normalizedLesson select item)
            .SingleOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("Published lesson was not found.");
        return ToDto(lesson);
    }
    private async Task<Course> FindCourseAsync(Guid id, CancellationToken token) =>
        await db.Courses.Include(x => x.Lessons).SingleOrDefaultAsync(x => x.Id == id, token) ?? throw new NotFoundException("Course was not found.");
    internal static LessonDto ToDto(Lesson x) => new(x.Id, x.CourseId, x.Title, x.Slug, x.Summary, x.Content,
        x.ContentType, x.ExternalResourceUrl, x.EstimatedDurationMinutes, x.Order, x.IsRequired, x.CreatedAtUtc, x.UpdatedAtUtc);
}
