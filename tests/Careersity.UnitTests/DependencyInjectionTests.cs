using Careersity.Application;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Careersity.UnitTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddCareersityApplication_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddCareersityApplication();

        result.Should().BeSameAs(services);
    }
}
