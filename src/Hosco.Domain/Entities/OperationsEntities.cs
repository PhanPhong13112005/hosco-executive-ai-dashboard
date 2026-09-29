using Hosco.Domain.Common;
using Hosco.Domain.Enums;

namespace Hosco.Domain.Entities;

public sealed class Alert : TenantEntity
{
    public Guid? RuleId { get; set; }
    public Guid? BranchId { get; set; }
    public required string Type { get; set; }
    public required string RuleCode { get; set; }
    public AlertSeverity Severity { get; set; }
    public AlertStatus Status { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public required string PayloadJson { get; set; }
    public decimal? DetectedValue { get; set; }
    public decimal? ThresholdValue { get; set; }
    public decimal? BaselineValue { get; set; }
    public DateTimeOffset DetectedAt { get; set; }
    public DateTimeOffset? AcknowledgedAt { get; set; }
    public Guid? AcknowledgedBy { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public Guid? ResolvedBy { get; set; }
    public string? ResolutionNote { get; set; }
    public DateTimeOffset? EscalatedAt { get; set; }
    public required string DedupKey { get; set; }
    public AlertRule? Rule { get; set; }
}

public sealed class AlertRule : TenantEntity
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public AlertSeverity Severity { get; set; }
    public bool IsEnabled { get; set; } = true;
    public decimal? Threshold { get; set; }
    public decimal? Baseline { get; set; }
    public int WindowMinutes { get; set; }
    public int CooldownMinutes { get; set; }
    public Guid? BranchId { get; set; }
    public required string ConfigJson { get; set; }
    public ICollection<Alert> Alerts { get; set; } = [];
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
