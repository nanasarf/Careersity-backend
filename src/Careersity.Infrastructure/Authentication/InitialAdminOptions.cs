namespace Careersity.Infrastructure.Authentication;

public sealed class InitialAdminOptions
{
    public const string SectionName = "InitialAdmin";
    public bool Enabled { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
