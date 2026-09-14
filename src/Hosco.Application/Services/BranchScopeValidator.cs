using Hosco.Application.Abstractions;
using Hosco.Domain.Enums;

namespace Hosco.Application.Services;

public interface IBranchDirectory
{
    Task<bool> BelongsToTenantAsync(Guid branchId, Guid tenantId, CancellationToken cancellationToken);
}

public sealed class BranchScopeValidator(ICurrentUser currentUser, IBranchDirectory branches) : IBranchScopeValidator
{
    public async Task EnsureCanAccessAsync(Guid? branchId, CancellationToken cancellationToken)
    {
        if (!branchId.HasValue) return;
        if (!await branches.BelongsToTenantAsync(branchId.Value, currentUser.TenantId, cancellationToken))
            throw new ForbiddenException("The requested branch is outside the current tenant.");

        var tenantWide = currentUser.Roles.Contains(SystemRole.Owner) ||
                         currentUser.Roles.Contains(SystemRole.ChainManager) ||
                         currentUser.Roles.Contains(SystemRole.SystemAdmin);
        if (!tenantWide && !currentUser.BranchIds.Contains(branchId.Value))
            throw new ForbiddenException("The requested branch is outside the current user's branch scope.");
    }
}
