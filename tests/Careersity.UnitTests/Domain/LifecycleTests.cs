using Careersity.Domain.Careers;
using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Careersity.UnitTests.Domain;

public sealed class LifecycleTests
{
    [Fact] public void DraftContent_CanBePublished()
    {
        var category = CreateCategory(); category.Publish();
        category.Status.Should().Be(ContentStatus.Published);
    }

    [Fact] public void DraftContent_CanBeArchived()
    {
        var category = CreateCategory(); category.Archive();
        category.Status.Should().Be(ContentStatus.Archived);
    }

    [Fact] public void PublishedContent_CanBeArchived()
    {
        var category = CreateCategory(); category.Publish(); category.Archive();
        category.Status.Should().Be(ContentStatus.Archived);
    }

    [Fact] public void ArchivedContent_CannotBePublished()
    {
        var category = CreateCategory(); category.Archive();
        category.Invoking(x => x.Publish()).Should().Throw<DomainException>();
    }

    [Fact] public void RepeatedPublish_IsIdempotent()
    {
        var category = CreateCategory(); category.Publish(); var updated = category.UpdatedAtUtc; category.Publish();
        category.Status.Should().Be(ContentStatus.Published); category.UpdatedAtUtc.Should().Be(updated);
    }

    [Fact] public void RepeatedArchive_IsIdempotent()
    {
        var category = CreateCategory(); category.Archive(); var updated = category.UpdatedAtUtc; category.Archive();
        category.Status.Should().Be(ContentStatus.Archived); category.UpdatedAtUtc.Should().Be(updated);
    }

    private static CareerCategory CreateCategory() => new("Technology", "TECHNOLOGY");
}
