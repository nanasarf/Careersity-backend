using Careersity.Domain.Common;
using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;

namespace Careersity.Domain.Courses;

/// <summary>A publishable learning unit composed of ordered lessons.</summary>
public sealed class Course : PublishableEntity
{
    private readonly List<Lesson> _lessons = [];
    private readonly List<CoursePrerequisite> _prerequisites = [];
    private readonly List<CourseSkill> _courseSkills = [];

    private Course() { }

    public Course(string title, string slug, string shortDescription, CourseDifficulty difficulty,
        int estimatedDurationMinutes, string? detailedDescription = null)
    {
        SetDetails(title, slug, shortDescription, detailedDescription);
        Difficulty = difficulty;
        EstimatedDurationMinutes = Guard.Positive(estimatedDurationMinutes, nameof(estimatedDurationMinutes));
    }

    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string ShortDescription { get; private set; } = string.Empty;
    public string? DetailedDescription { get; private set; }
    public CourseDifficulty Difficulty { get; private set; }
    public int EstimatedDurationMinutes { get; private set; }
    public IReadOnlyCollection<Lesson> Lessons => _lessons.AsReadOnly();
    public IReadOnlyCollection<CoursePrerequisite> Prerequisites => _prerequisites.AsReadOnly();
    public IReadOnlyCollection<CourseSkill> CourseSkills => _courseSkills.AsReadOnly();

    public void UpdateDetails(string title, string slug, string shortDescription, string? detailedDescription = null)
    {
        SetDetails(title, slug, shortDescription, detailedDescription);
        MarkUpdated();
    }

    public void ChangeDifficulty(CourseDifficulty difficulty) { Difficulty = difficulty; MarkUpdated(); }
    public void ChangeDuration(int minutes) { EstimatedDurationMinutes = Guard.Positive(minutes, nameof(minutes)); MarkUpdated(); }

    public void AddLesson(Lesson lesson)
    {
        ArgumentNullException.ThrowIfNull(lesson);
        if (lesson.CourseId != Id) throw new DomainException("The lesson belongs to a different course.");
        if (_lessons.Any(x => x.Id == lesson.Id)) throw new DomainException("The lesson is already in this course.");
        if (_lessons.Any(x => x.Order == lesson.Order)) throw new DomainException("Lesson order must be unique within a course.");
        _lessons.Add(lesson);
        MarkUpdated();
    }

    public void RemoveLesson(Guid lessonId)
    {
        var lesson = _lessons.SingleOrDefault(x => x.Id == lessonId) ?? throw new DomainException("The lesson does not belong to this course.");
        _lessons.Remove(lesson); MarkUpdated();
    }

    public void ReorderLesson(Guid lessonId, int newOrder)
    {
        Guard.NonNegative(newOrder, nameof(newOrder));
        var lesson = _lessons.SingleOrDefault(x => x.Id == lessonId) ?? throw new DomainException("The lesson does not belong to this course.");
        if (_lessons.Any(x => x.Id != lessonId && x.Order == newOrder)) throw new DomainException("Lesson order must be unique within a course.");
        lesson.ChangeOrder(newOrder); MarkUpdated();
    }

    public CoursePrerequisite AddPrerequisite(Guid prerequisiteCourseId, bool isRequired = true)
    {
        if (_prerequisites.Any(x => x.PrerequisiteCourseId == prerequisiteCourseId)) throw new DomainException("The prerequisite is already associated with this course.");
        var prerequisite = new CoursePrerequisite(Id, prerequisiteCourseId, isRequired);
        _prerequisites.Add(prerequisite); MarkUpdated(); return prerequisite;
    }

    public void RemovePrerequisite(Guid prerequisiteCourseId)
    {
        var prerequisite = _prerequisites.SingleOrDefault(x => x.PrerequisiteCourseId == prerequisiteCourseId)
            ?? throw new DomainException("The prerequisite is not associated with this course.");
        _prerequisites.Remove(prerequisite); MarkUpdated();
    }

    public CourseSkill AssociateSkill(Guid skillId, SkillProficiencyLevel proficiencyLevel, bool isPrimary = false)
    {
        if (_courseSkills.Any(x => x.SkillId == skillId)) throw new DomainException("The skill is already associated with this course.");
        var courseSkill = new CourseSkill(Id, skillId, proficiencyLevel, isPrimary);
        _courseSkills.Add(courseSkill); MarkUpdated(); return courseSkill;
    }

    public void RemoveSkillAssociation(Guid skillId)
    {
        var courseSkill = _courseSkills.SingleOrDefault(x => x.SkillId == skillId)
            ?? throw new DomainException("The skill is not associated with this course.");
        _courseSkills.Remove(courseSkill); MarkUpdated();
    }

    public override void Publish()
    {
        if (Status == ContentStatus.Draft && _lessons.Count == 0) throw new DomainException("A course must contain at least one lesson before publication.");
        base.Publish();
    }

    private void SetDetails(string title, string slug, string shortDescription, string? detailedDescription)
    {
        Title = Guard.Required(title, 200, nameof(title));
        Slug = Guard.Slug(slug, 220, nameof(slug));
        ShortDescription = Guard.Required(shortDescription, 500, nameof(shortDescription));
        DetailedDescription = Guard.Optional(detailedDescription, 5_000, nameof(detailedDescription));
    }
}
