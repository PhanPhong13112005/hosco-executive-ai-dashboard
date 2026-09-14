using Hosco.Domain.Enums;

namespace Hosco.Application.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
    Guid TenantId { get; }
    IReadOnlySet<SystemRole> Roles { get; }
    IReadOnlySet<Guid> BranchIds { get; }
}

public interface IBranchScopeValidator
{
    Task EnsureCanAccessAsync(Guid? branchId, CancellationToken cancellationToken);
}

public sealed class ForbiddenException(string message) : Exception(message);
public sealed class ValidationException(string message) : Exception(message);
public sealed class BusinessDefinitionPendingException(string metricCode)
    : Exception($"Business definition for metric '{metricCode}' is awaiting BA approval.");
