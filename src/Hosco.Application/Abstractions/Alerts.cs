using Hosco.Application.Models;
using Hosco.Domain.Entities;

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
}

public interface IAlertEngine
{
    Task<AlertEngineResult> EvaluateAllAsync(CancellationToken cancellationToken);
}

public interface IAlertEngineDiagnostics
{
    void RuleFailed(AlertRule rule, Exception exception);
}

public interface IAlertService
{
    Task<AlertPage> ListAsync(AlertFilter filter, CancellationToken cancellationToken);
    Task<AlertDetail> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<AlertDetail> AcknowledgeAsync(Guid id, CancellationToken cancellationToken);
    Task<AlertDetail> ResolveAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertRuleView>> ListRulesAsync(Guid? branchId, CancellationToken cancellationToken);
    Task<AlertRuleView> UpdateRuleAsync(Guid id, UpdateAlertRule update, CancellationToken cancellationToken);
}
