using Careersity.Domain.Common;
using Careersity.Domain.Exceptions;

namespace Careersity.Domain.Careers;

/// <summary>An ordered, versioned route through the learning required for a career.</summary>
public sealed class CareerPathway : PublishableEntity
{
    private readonly List<PathwayLevel> _levels = [];

    private CareerPathway() { }

    public CareerPathway(Guid careerId, string name, string version, string? description = null, bool isPrimary = false)
    {
        CareerId = Guard.NotEmpty(careerId, nameof(careerId));
        SetDetails(name, description, version);
        IsPrimary = isPrimary;
    }

    public Guid CareerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string Version { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; }
    public IReadOnlyCollection<PathwayLevel> Levels => _levels.AsReadOnly();

    public void UpdateDetails(string name, string version, string? description = null)
    {
        SetDetails(name, description, version);
        MarkUpdated();
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
        MarkUpdated();
    }

    public void AddLevel(PathwayLevel level)
    {
        ArgumentNullException.ThrowIfNull(level);
        if (level.CareerPathwayId != Id) throw new DomainException("The level belongs to a different pathway.");
        if (_levels.Any(x => x.Id == level.Id)) throw new DomainException("The level is already in this pathway.");
        if (_levels.Any(x => x.Order == level.Order)) throw new DomainException("Level order must be unique within a pathway.");
        _levels.Add(level);
        MarkUpdated();
    }

    public void RemoveLevel(Guid levelId)
    {
        var level = _levels.SingleOrDefault(x => x.Id == levelId)
            ?? throw new DomainException("The level does not belong to this pathway.");
        _levels.Remove(level);
        MarkUpdated();
    }

    public void ReorderLevel(Guid levelId, int newOrder)
    {
        Guard.NonNegative(newOrder, nameof(newOrder));
        var level = _levels.SingleOrDefault(x => x.Id == levelId)
            ?? throw new DomainException("The level does not belong to this pathway.");
        if (_levels.Any(x => x.Id != levelId && x.Order == newOrder)) throw new DomainException("Level order must be unique within a pathway.");
        level.ChangeOrder(newOrder);
        MarkUpdated();
    }

    private void SetDetails(string name, string? description, string version)
    {
        Name = Guard.Required(name, 200, nameof(name));
        Description = Guard.Optional(description, 2_000, nameof(description));
        Version = Guard.Required(version, 30, nameof(version));
    }
}
