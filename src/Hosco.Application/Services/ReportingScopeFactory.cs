using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Domain.Enums;

namespace Hosco.Application.Services;

public interface IReportingScopeFactory
{
    Task<ReportingScope> CreateAsync(Guid? requestedBranchId, CancellationToken cancellationToken);
}

public sealed class ReportingScopeFactory(ICurrentUser currentUser, IBranchScopeValidator validator) : IReportingScopeFactory
{
    public async Task<ReportingScope> CreateAsync(Guid? requestedBranchId, CancellationToken cancellationToken)
    {
        await validator.EnsureCanAccessAsync(requestedBranchId, cancellationToken);
        if (requestedBranchId.HasValue)
            return new ReportingScope(currentUser.TenantId, new HashSet<Guid> { requestedBranchId.Value });

        var tenantWide = currentUser.Roles.Contains(SystemRole.ChainManager);
        return new ReportingScope(currentUser.TenantId, tenantWide ? null : currentUser.BranchIds);
    }
}
