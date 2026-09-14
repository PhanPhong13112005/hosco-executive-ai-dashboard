using Hosco.Application.Abstractions;
using Hosco.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hosco.Infrastructure.Security;

public sealed class IdentityStore(HoscoDbContext db) : IIdentityStore
{
    public async Task<IdentityRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking()
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .Include(x => x.UserBranches)
            .SingleOrDefaultAsync(x => x.Email == email.ToLowerInvariant(), cancellationToken);
        return user is null ? null : new IdentityRecord(user.Id, user.TenantId, user.Email, user.DisplayName,
            user.PasswordHash, user.IsActive, user.UserRoles.Select(x => x.Role.Name).ToHashSet(),
            user.UserBranches.Select(x => x.BranchId).ToHashSet());
    }
}
