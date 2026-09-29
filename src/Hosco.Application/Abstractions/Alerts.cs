using Hosco.Application.Models;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;

namespace Hosco.Application.Abstractions;

public interface IAlertSignalDataStore
{
    Task<IReadOnlyList<AlertSignal>> GetCancellationRateSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertSignal>> GetRevenueDropSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertSignal>> GetDangerousStockSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertSignal>> GetEmployeeCancellationSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertSignal>> GetPriceDiscountSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken cancellationToken);
}

public interface IAlertRepository
{
    Task<IReadOnlyList<AlertRule>> GetEnabledRulesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertRule>> GetRulesAsync(ReportingScope scope, CancellationToken cancellationToken);
    Task<AlertRule?> GetRuleAsync(Guid id, ReportingScope scope, CancellationToken cancellationToken);
    Task<PagedResult<Alert>> GetAlertsAsync(ReportingScope scope, AlertFilter filter, CancellationToken cancellationToken);
    Task<Alert?> GetAlertAsync(Guid id, ReportingScope scope, CancellationToken cancellationToken);
    Task<AlertSummary> GetSummaryAsync(ReportingScope scope, CancellationToken cancellationToken);
    Task<IReadOnlyList<Alert>> GetUnacknowledgedForEscalationAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task<bool> ExistsWithinCooldownAsync(Guid tenantId, string dedupKey, DateTimeOffset since, CancellationToken cancellationToken);
    Task AddAsync(Alert alert, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IAlertRuleEvaluator
{
    string RuleCode { get; }
    Task<IReadOnlyList<AlertCandidate>> EvaluateAsync(AlertRule rule, DateTimeOffset now, CancellationToken cancellationToken);
}

public interface INotificationSender
{
    Task SendAsync(Alert alert, CancellationToken cancellationToken);
    Task SendEscalationAsync(Alert alert, CancellationToken cancellationToken);
    Task RetryPendingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public interface INotificationChannel
{
    string Name { get; }
    int MaxAttempts { get; }
    TimeSpan RetryDelay(int completedAttempts);
    NotificationTarget? ResolveTarget(Alert alert, NotificationPurpose purpose);
    Task<NotificationAttemptResult> SendAsync(Alert alert, NotificationPurpose purpose, NotificationTarget target, CancellationToken cancellationToken);
}

public interface INotificationDeliveryStore
{
    Task<NotificationDelivery> GetOrCreateAsync(Alert alert, string channel, NotificationPurpose purpose,
        NotificationTarget target, string idempotencyKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<NotificationDelivery>> GetRetryableAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken);
    Task<Alert?> GetAlertAsync(Guid alertId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed record NotificationTarget(string RecipientKey, string Address);
public sealed record NotificationAttemptResult(bool Sent, bool Retryable, bool Skipped = false, string? Error = null);

public interface IAlertEngine
{
    Task<AlertEngineResult> EvaluateAllAsync(CancellationToken cancellationToken);
}

public interface IAlertEngineDiagnostics
{
    void RuleFailed(AlertRule rule, Exception exception);
    void NotificationRetryFailed(Exception exception) { }
}

public interface IAlertService
{
    Task<AlertPage> ListAsync(AlertFilter filter, CancellationToken cancellationToken);
    Task<AlertDetail> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<AlertDetail> AcknowledgeAsync(Guid id, CancellationToken cancellationToken);
    Task<AlertDetail> ResolveAsync(Guid id, ResolveAlertRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertRuleView>> ListRulesAsync(Guid? branchId, CancellationToken cancellationToken);
    Task<AlertRuleView> UpdateRuleAsync(Guid id, UpdateAlertRule update, CancellationToken cancellationToken);
}
