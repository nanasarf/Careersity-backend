using Careersity.Application.Abstractions.Authentication;

namespace Careersity.Infrastructure.Authentication;

internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
