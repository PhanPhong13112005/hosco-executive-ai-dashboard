using Hosco.Domain.Enums;

namespace Hosco.Application.Abstractions;

public sealed record IdentityRecord(
    Guid UserId,
    Guid TenantId,
    string Email,
    string DisplayName,
    string PasswordHash,
    bool IsActive,
    IReadOnlySet<SystemRole> Roles,
    IReadOnlySet<Guid> BranchIds);

public interface IIdentityStore
{
    Task<IdentityRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken);
}

public interface IPasswordVerifier
{
    bool Verify(string password, string encodedHash);
}

public interface IAuditWriter
{
    Task WriteAsync(string action, string resourceType, string? resourceId, string? queryId,
        Guid? branchId, object? metadata, CancellationToken cancellationToken);
}
