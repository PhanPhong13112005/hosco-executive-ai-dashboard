using System.Text.Json;
using Hosco.Application.Abstractions;
using Hosco.Domain.Entities;

namespace Hosco.Infrastructure.Persistence;

public interface ICorrelationContext { string CorrelationId { get; } }

public sealed class AuditWriter(HoscoDbContext db, ICurrentUser currentUser, ICorrelationContext correlation) : IAuditWriter
{
    public async Task WriteAsync(string action, string resourceType, string? resourceId, string? queryId,
        Guid? branchId, object? metadata, CancellationToken cancellationToken)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(), TenantId = currentUser.TenantId, UserId = currentUser.UserId, BranchId = branchId,
            Action = action, ResourceType = resourceType, ResourceId = resourceId, QueryId = queryId,
            CorrelationId = correlation.CorrelationId, MetadataJson = JsonSerializer.Serialize(metadata ?? new { }),
            OccurredAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
