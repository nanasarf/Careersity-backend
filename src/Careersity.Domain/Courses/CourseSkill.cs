using Careersity.Domain.Common;
using Careersity.Domain.Enums;

namespace Careersity.Domain.Courses;

/// <summary>Associates a course with a skill and target proficiency.</summary>
public sealed class CourseSkill : AuditableEntity
{
    private CourseSkill() { }

    public CourseSkill(Guid courseId, Guid skillId, SkillProficiencyLevel proficiencyLevel, bool isPrimary = false)
    {
        CourseId = Guard.NotEmpty(courseId, nameof(courseId));
        SkillId = Guard.NotEmpty(skillId, nameof(skillId));
        ProficiencyLevel = proficiencyLevel;
        IsPrimary = isPrimary;
    }

    public Guid CourseId { get; private set; }
    public Guid SkillId { get; private set; }
    public SkillProficiencyLevel ProficiencyLevel { get; private set; }
    public bool IsPrimary { get; private set; }

    public void Update(SkillProficiencyLevel proficiencyLevel, bool isPrimary)
    {
        ProficiencyLevel = proficiencyLevel;
        IsPrimary = isPrimary;
        MarkUpdated();
    }
}
