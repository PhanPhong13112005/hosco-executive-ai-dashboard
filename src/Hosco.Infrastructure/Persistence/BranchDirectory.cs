using Hosco.Application.Services;
using Microsoft.EntityFrameworkCore;

namespace Hosco.Infrastructure.Persistence;

public sealed class BranchDirectory(HoscoDbContext db) : IBranchDirectory
{
    public Task<bool> BelongsToTenantAsync(Guid branchId, Guid tenantId, CancellationToken cancellationToken) =>
        db.Branches.AsNoTracking().AnyAsync(x => x.Id == branchId && x.TenantId == tenantId && x.IsActive, cancellationToken);
}
