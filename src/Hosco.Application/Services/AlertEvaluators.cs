using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Domain.Entities;

namespace Hosco.Application.Services;

public abstract class AlertRuleEvaluatorBase(IAlertSignalDataStore data) : IAlertRuleEvaluator
{
    protected IAlertSignalDataStore Data { get; } = data;
    public abstract string RuleCode { get; }

    public async Task<IReadOnlyList<AlertCandidate>> EvaluateAsync(AlertRule rule, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!rule.Code.Equals(RuleCode, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException($"Evaluator {RuleCode} cannot evaluate rule {rule.Code}.");
        if (!rule.IsEnabled) return [];

        var signals = await LoadSignalsAsync(rule, now, cancellationToken);
        return signals.Select(signal => new AlertCandidate(
            rule.Id, rule.Code, rule.TenantId, signal.BranchId, signal.Severity,
            signal.Title, signal.Message, signal.DetectedValue, signal.ThresholdValue,
            signal.BaselineValue, BuildDedupKey(rule, signal), signal.ContextJson, now)).ToList();
    }

    protected abstract Task<IReadOnlyList<AlertSignal>> LoadSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken cancellationToken);

    private static string BuildDedupKey(AlertRule rule, AlertSignal signal) =>
        $"{rule.TenantId:N}:{signal.BranchId?.ToString("N") ?? "all"}:{rule.Code.ToUpperInvariant()}:{signal.EntityKey.ToLowerInvariant()}";
}

public sealed class CancellationRateAlertEvaluator(IAlertSignalDataStore data) : AlertRuleEvaluatorBase(data)
{
    public override string RuleCode => "AL-01";
    protected override Task<IReadOnlyList<AlertSignal>> LoadSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct) =>
        Data.GetCancellationRateSignalsAsync(rule, now, ct);
}

public sealed class RevenueDropAlertEvaluator(IAlertSignalDataStore data) : AlertRuleEvaluatorBase(data)
{
    public override string RuleCode => "AL-02";
    protected override Task<IReadOnlyList<AlertSignal>> LoadSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct) =>
        Data.GetRevenueDropSignalsAsync(rule, now, ct);
}

public sealed class DangerousStockAlertEvaluator(IAlertSignalDataStore data) : AlertRuleEvaluatorBase(data)
{
    public override string RuleCode => "AL-03";
    protected override Task<IReadOnlyList<AlertSignal>> LoadSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct) =>
        Data.GetDangerousStockSignalsAsync(rule, now, ct);
}

public sealed class EmployeeCancellationAlertEvaluator(IAlertSignalDataStore data) : AlertRuleEvaluatorBase(data)
{
    public override string RuleCode => "AL-04";
    protected override Task<IReadOnlyList<AlertSignal>> LoadSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct) =>
        Data.GetEmployeeCancellationSignalsAsync(rule, now, ct);
}

public sealed class PriceDiscountAlertEvaluator(IAlertSignalDataStore data) : AlertRuleEvaluatorBase(data)
{
    public override string RuleCode => "AL-05";
    protected override Task<IReadOnlyList<AlertSignal>> LoadSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct) =>
        Data.GetPriceDiscountSignalsAsync(rule, now, ct);
}
