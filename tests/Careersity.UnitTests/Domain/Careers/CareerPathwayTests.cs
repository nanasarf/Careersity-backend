using Careersity.Domain.Careers;
using Careersity.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Careersity.UnitTests.Domain.Careers;

public sealed class CareerPathwayTests
{
    [Fact] public void AddLevel_AddsLevel()
    {
        var pathway = TestData.Pathway(); var level = new PathwayLevel(pathway.Id, "Foundation", 0);
        pathway.AddLevel(level);
        pathway.Levels.Should().ContainSingle().Which.Should().BeSameAs(level);
    }

    [Fact] public void AddLevel_RejectsDuplicateEntity()
    {
        var pathway = TestData.Pathway(); var level = new PathwayLevel(pathway.Id, "Foundation", 0); pathway.AddLevel(level);
        pathway.Invoking(x => x.AddLevel(level)).Should().Throw<DomainException>();
    }

    [Fact] public void AddLevel_RejectsDuplicateOrder()
    {
        var pathway = TestData.Pathway(); pathway.AddLevel(new PathwayLevel(pathway.Id, "One", 0));
        pathway.Invoking(x => x.AddLevel(new PathwayLevel(pathway.Id, "Two", 0))).Should().Throw<DomainException>();
    }

    [Fact] public void ReorderLevel_ChangesOrder()
    {
        var pathway = TestData.Pathway(); var level = new PathwayLevel(pathway.Id, "One", 0); pathway.AddLevel(level);
        pathway.ReorderLevel(level.Id, 2);
        level.Order.Should().Be(2);
    }

    [Fact] public void ReorderLevel_RejectsNegativeOrder()
    {
        var pathway = TestData.Pathway(); var level = new PathwayLevel(pathway.Id, "One", 0); pathway.AddLevel(level);
        pathway.Invoking(x => x.ReorderLevel(level.Id, -1)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] public void RemoveLevel_RejectsUnknownLevel() =>
        TestData.Pathway().Invoking(x => x.RemoveLevel(Guid.NewGuid())).Should().Throw<DomainException>();
}
