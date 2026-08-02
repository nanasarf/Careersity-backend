namespace Careersity.Api.Infrastructure;

public static class SecurityPolicies
{
    public const string AdministratorOnly = "AdministratorOnly";
    public const string LoginRateLimit = "LoginRateLimit";
    public const string RegistrationRateLimit = "RegistrationRateLimit";
    public const string RefreshRateLimit = "RefreshRateLimit";
}
