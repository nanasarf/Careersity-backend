using Careersity.Application.Identity.Dtos;
using Careersity.Application.Identity.Requests;

namespace Careersity.Application.Identity.Services;

public interface IAuthenticationService
{
    Task<AuthenticationResultDto> RegisterAsync(RegisterRequest request, string? ip, CancellationToken cancellationToken);
    Task<AuthenticationResultDto> LoginAsync(LoginRequest request, string? ip, CancellationToken cancellationToken);
    Task<AuthenticationResultDto> RefreshAsync(RefreshAccessTokenRequest request, string? ip, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequest request, string? ip, CancellationToken cancellationToken);
    Task RevokeAllAsync(string? ip, CancellationToken cancellationToken);
    Task<AuthenticationResultDto> ChangePasswordAsync(ChangeMyPasswordRequest request, string? ip, CancellationToken cancellationToken);
}
public interface IUserProfileService
{
    Task<UserProfileDto> GetAsync(CancellationToken cancellationToken);
    Task<UserProfileDto> UpdateAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken);
}
