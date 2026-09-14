using Hosco.Domain.Common;
using Hosco.Domain.Enums;

namespace Hosco.Domain.Entities;

public sealed class Alert : TenantEntity
{
    public Guid? BranchId { get; set; }
    public required string Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public AlertStatus Status { get; set; }
    public required string Title { get; set; }
    public required string PayloadJson { get; set; }
    public DateTimeOffset DetectedAt { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}

public sealed class AuditLog : TenantEntity
{
    public Guid? UserId { get; set; }
    public Guid? BranchId { get; set; }
    public required string Action { get; set; }
    public required string ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public string? QueryId { get; set; }
    public required string CorrelationId { get; set; }
    public required string MetadataJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
