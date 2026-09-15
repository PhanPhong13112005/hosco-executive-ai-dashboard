using Hosco.Domain.Enums;

namespace Hosco.Application.Models;

public sealed record AlertFilter(
    Guid? BranchId = null,
    AlertSeverity? Severity = null,
    AlertStatus? Status = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = 50)
{
    public AlertFilter Validate()
    {
        if (Page < 1) throw new Abstractions.ValidationException("page must be at least 1.");
        if (PageSize is < 1 or > ReportingFilter.MaxPageSize)
            throw new Abstractions.ValidationException($"pageSize must be between 1 and {ReportingFilter.MaxPageSize}.");
        if (From.HasValue && To.HasValue && From > To)
            throw new Abstractions.ValidationException("from must be earlier than or equal to to.");
        return this;
    }
}

public sealed record AlertListItem(
    Guid Id, string RuleCode, Guid? BranchId, string Severity, string Status,
    string Title, DateTimeOffset DetectedAt, decimal? DetectedValue, decimal? ThresholdValue);

public sealed record AlertDetail(
    Guid Id, Guid? RuleId, string RuleCode, Guid TenantId, Guid? BranchId, string Severity, string Status,
    string Title, string Message, decimal? DetectedValue, decimal? ThresholdValue, string ContextJson,
    DateTimeOffset DetectedAt, DateTimeOffset? AcknowledgedAt, Guid? AcknowledgedBy,
    DateTimeOffset? ResolvedAt, Guid? ResolvedBy, string DedupKey);

public sealed record AlertSummary(int Open, int Urgent, int Handled, int Resolved);

public sealed record AlertPage(IReadOnlyList<AlertListItem> Items, int TotalCount, AlertSummary Summary);

public sealed record AlertRuleView(
    Guid Id, string Code, string Name, string Description, string Severity, bool IsEnabled,
    decimal? Threshold, decimal? Baseline, int WindowMinutes, int CooldownMinutes,
    Guid? BranchId, string ConfigJson, string BaStatus);

public sealed record UpdateAlertRule(
    bool? IsEnabled = null,
    AlertSeverity? Severity = null,
    decimal? Threshold = null,
    decimal? Baseline = null,
    int? WindowMinutes = null,
    int? CooldownMinutes = null,
    string? ConfigJson = null);

public sealed record AlertSignal(
    Guid? BranchId, string EntityKey, string Title, string Message,
    decimal DetectedValue, decimal ThresholdValue, string ContextJson);

public sealed record AlertCandidate(
    Guid RuleId, string RuleCode, Guid TenantId, Guid? BranchId, AlertSeverity Severity,
    string Title, string Message, decimal DetectedValue, decimal ThresholdValue,
    string DedupKey, string ContextJson, DateTimeOffset DetectedAt);

public sealed record AlertEngineResult(int EvaluatedRules, int CreatedAlerts, int SuppressedDuplicates, int FailedRules);

