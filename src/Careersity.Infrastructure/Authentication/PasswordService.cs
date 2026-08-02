using Careersity.Application.Abstractions.Authentication;
using Careersity.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace Careersity.Infrastructure.Authentication;

internal sealed class PasswordService(IPasswordHasher<User> hasher) : IPasswordService
{
    public string Hash(string password) => hasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        hasher.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
}
