using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;

namespace Hosco.Application.Services;

public sealed class AlertEngine(
    IAlertRepository repository,
    IEnumerable<IAlertRuleEvaluator> evaluators,
    INotificationSender notifications,
    TimeProvider timeProvider,
    IAlertEngineDiagnostics? diagnostics = null) : IAlertEngine
{
    private readonly IReadOnlyDictionary<string, IAlertRuleEvaluator> _evaluators = evaluators
        .ToDictionary(x => x.RuleCode, StringComparer.OrdinalIgnoreCase);

    public async Task<AlertEngineResult> EvaluateAllAsync(CancellationToken cancellationToken)
    {
        var rules = await repository.GetEnabledRulesAsync(cancellationToken);
        var created = 0;
        var suppressed = 0;
        var failed = 0;
        var now = timeProvider.GetUtcNow();

        foreach (var rule in rules)
        {
            try
            {
                if (!_evaluators.TryGetValue(rule.Code, out var evaluator))
                {
                    failed++;
                    continue;
                }

                var candidates = await evaluator.EvaluateAsync(rule, now, cancellationToken);
                foreach (var candidate in candidates)
                {
                    var cooldownStart = now.AddMinutes(-Math.Max(0, rule.CooldownMinutes));
                    if (await repository.ExistsWithinCooldownAsync(rule.TenantId, candidate.DedupKey, cooldownStart, cancellationToken))
                    {
                        suppressed++;
                        continue;
                    }

                    var alert = ToEntity(candidate, now);
                    await repository.AddAsync(alert, cancellationToken);
                    await repository.SaveChangesAsync(cancellationToken);
                    created++;
                    await notifications.SendAsync(alert, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failed++;
                diagnostics?.RuleFailed(rule, exception);
            }
        }

        return new AlertEngineResult(rules.Count, created, suppressed, failed);
    }

    private static Alert ToEntity(AlertCandidate candidate, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        RuleId = candidate.RuleId,
        RuleCode = candidate.RuleCode,
        Type = candidate.RuleCode,
        TenantId = candidate.TenantId,
        BranchId = candidate.BranchId,
        Severity = candidate.Severity,
        Status = AlertStatus.Open,
        Title = candidate.Title,
        Message = candidate.Message,
        DetectedValue = candidate.DetectedValue,
        ThresholdValue = candidate.ThresholdValue,
        PayloadJson = candidate.ContextJson,
        DedupKey = candidate.DedupKey,
        DetectedAt = candidate.DetectedAt,
        CreatedAt = now,
        UpdatedAt = now
    };
}
