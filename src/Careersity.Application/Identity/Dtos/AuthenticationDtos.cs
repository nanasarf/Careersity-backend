using Careersity.Domain.Enums;

namespace Careersity.Application.Identity.Dtos;

public sealed record AuthenticatedUserDto(Guid Id, string Email, string FirstName, string LastName, string FullName, UserRole Role);
public sealed record AuthenticationResultDto(AuthenticatedUserDto User, string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken, DateTimeOffset RefreshTokenExpiresAtUtc);
public sealed record UserProfileDto(Guid Id, string Email, string FirstName, string LastName, string FullName,
    UserRole Role, bool IsActive, DateTimeOffset? LastLoginAtUtc, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
