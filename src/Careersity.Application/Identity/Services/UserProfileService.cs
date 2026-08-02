using Careersity.Application.Abstractions.Authentication;
using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.Identity.Dtos;
using Careersity.Application.Identity.Requests;
using Careersity.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.Identity.Services;

public sealed class UserProfileService(ICareersityDbContext db, ICurrentUser currentUser) : IUserProfileService
{
    public async Task<UserProfileDto> GetAsync(CancellationToken cancellationToken) => ToDto(await FindAsync(cancellationToken));
    public async Task<UserProfileDto> UpdateAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken)
    { var user = await FindAsync(cancellationToken); user.UpdateName(request.FirstName, request.LastName); await db.SaveChangesAsync(cancellationToken); return ToDto(user); }
    private async Task<User> FindAsync(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue) throw new UnauthorizedException();
        return await db.Users.SingleOrDefaultAsync(x => x.Id == currentUser.UserId.Value, cancellationToken)
            ?? throw new UnauthorizedException();
    }
    private static UserProfileDto ToDto(User user) => new(user.Id, user.Email, user.FirstName, user.LastName,
        $"{user.FirstName} {user.LastName}", user.Role, user.IsActive, user.LastLoginAtUtc, user.CreatedAtUtc, user.UpdatedAtUtc);
}
