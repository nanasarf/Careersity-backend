using Careersity.Domain.Common;
using Careersity.Domain.Enums;

namespace Careersity.Domain.Careers;

/// <summary>Records the proficiency a career requires in a skill.</summary>
public sealed class CareerSkill : AuditableEntity
{
    private CareerSkill() { }

    public CareerSkill(Guid careerId, Guid skillId, SkillProficiencyLevel requiredProficiencyLevel,
        bool isRequired, int displayOrder)
    {
        CareerId = Guard.NotEmpty(careerId, nameof(careerId));
        SkillId = Guard.NotEmpty(skillId, nameof(skillId));
        RequiredProficiencyLevel = requiredProficiencyLevel;
        IsRequired = isRequired;
        DisplayOrder = Guard.NonNegative(displayOrder, nameof(displayOrder));
    }

    public Guid CareerId { get; private set; }
    public Guid SkillId { get; private set; }
    public SkillProficiencyLevel RequiredProficiencyLevel { get; private set; }
    public bool IsRequired { get; private set; }
    public int DisplayOrder { get; private set; }

    public void UpdateRequirement(SkillProficiencyLevel proficiencyLevel, bool isRequired, int displayOrder)
    {
        DisplayOrder = Guard.NonNegative(displayOrder, nameof(displayOrder));
        RequiredProficiencyLevel = proficiencyLevel;
        IsRequired = isRequired;
        MarkUpdated();
    }
}
