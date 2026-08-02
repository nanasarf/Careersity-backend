namespace Careersity.Application.Identity.Requests;

public sealed record RegisterRequest(string Email, string FirstName, string LastName, string Password, string ConfirmPassword);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshAccessTokenRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);
public sealed record UpdateMyProfileRequest(string FirstName, string LastName);
public sealed record ChangeMyPasswordRequest(string CurrentPassword, string NewPassword, string ConfirmNewPassword);
