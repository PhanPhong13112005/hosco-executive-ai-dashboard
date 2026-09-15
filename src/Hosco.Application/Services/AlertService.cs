using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;

namespace Hosco.Application.Services;

public sealed class AlertService(
    IAlertRepository repository,
    IReportingScopeFactory scopes,
    ICurrentUser currentUser,
    IAuditWriter audit,
    TimeProvider timeProvider) : IAlertService
{
    public async Task<AlertPage> ListAsync(AlertFilter filter, CancellationToken cancellationToken)
    {
        filter.Validate();
        var scope = await scopes.CreateAsync(filter.BranchId, cancellationToken);
        var result = await repository.GetAlertsAsync(scope, filter, cancellationToken);
        var summary = await repository.GetSummaryAsync(scope, cancellationToken);
        return new AlertPage(result.Items.Select(ToListItem).ToList(), result.TotalCount, summary);
    }

    public async Task<AlertDetail> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var alert = await repository.GetAlertAsync(id, await scopes.CreateAsync(null, cancellationToken), cancellationToken)
            ?? throw new KeyNotFoundException($"Alert '{id}' was not found.");
        return ToDetail(alert);
    }

    public async Task<AlertDetail> AcknowledgeAsync(Guid id, CancellationToken cancellationToken)
    {
        var alert = await GetEntityAsync(id, cancellationToken);
        if (alert.Status == AlertStatus.Resolved)
            throw new ValidationException("A resolved alert cannot be acknowledged.");
        if (alert.Status == AlertStatus.Open)
        {
            var now = timeProvider.GetUtcNow();
            alert.Status = AlertStatus.Acknowledged;
            alert.AcknowledgedAt = now;
            alert.AcknowledgedBy = currentUser.UserId;
            alert.UpdatedAt = now;
            await repository.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("alert.acknowledge", "Alert", alert.Id.ToString(), null, alert.BranchId, new { alert.RuleCode }, cancellationToken);
        }
        return ToDetail(alert);
    }

    public async Task<AlertDetail> ResolveAsync(Guid id, CancellationToken cancellationToken)
    {
        var alert = await GetEntityAsync(id, cancellationToken);
        if (alert.Status != AlertStatus.Resolved)
        {
            var now = timeProvider.GetUtcNow();
            alert.Status = AlertStatus.Resolved;
            alert.ResolvedAt = now;
            alert.ResolvedBy = currentUser.UserId;
            alert.UpdatedAt = now;
            await repository.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("alert.resolve", "Alert", alert.Id.ToString(), null, alert.BranchId, new { alert.RuleCode }, cancellationToken);
        }
        return ToDetail(alert);
    }

    public async Task<IReadOnlyList<AlertRuleView>> ListRulesAsync(Guid? branchId, CancellationToken cancellationToken)
    {
        var rules = await repository.GetRulesAsync(await scopes.CreateAsync(branchId, cancellationToken), cancellationToken);
        return rules.Select(ToRuleView).ToList();
    }

    public async Task<AlertRuleView> UpdateRuleAsync(Guid id, UpdateAlertRule update, CancellationToken cancellationToken)
    {
        EnsureCanConfigure();
        Validate(update);
        var scope = await scopes.CreateAsync(null, cancellationToken);
        var rule = await repository.GetRuleAsync(id, scope, cancellationToken)
            ?? throw new KeyNotFoundException($"Alert rule '{id}' was not found.");
        if (update.IsEnabled.HasValue) rule.IsEnabled = update.IsEnabled.Value;
        if (update.Severity.HasValue) rule.Severity = update.Severity.Value;
        if (update.Threshold.HasValue) rule.Threshold = update.Threshold.Value;
        if (update.Baseline.HasValue) rule.Baseline = update.Baseline.Value;
        if (update.WindowMinutes.HasValue) rule.WindowMinutes = update.WindowMinutes.Value;
        if (update.CooldownMinutes.HasValue) rule.CooldownMinutes = update.CooldownMinutes.Value;
        if (update.ConfigJson is not null) rule.ConfigJson = update.ConfigJson;
        rule.UpdatedAt = timeProvider.GetUtcNow();
        await repository.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("alert-rule.update", "AlertRule", rule.Id.ToString(), null, rule.BranchId,
            new { rule.Code, rule.IsEnabled, rule.Threshold, rule.CooldownMinutes }, cancellationToken);
        return ToRuleView(rule);
    }

    private async Task<Alert> GetEntityAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetAlertAsync(id, await scopes.CreateAsync(null, cancellationToken), cancellationToken)
        ?? throw new KeyNotFoundException($"Alert '{id}' was not found.");

    private void EnsureCanConfigure()
    {
        if (!currentUser.Roles.Any(x => x is SystemRole.Owner or SystemRole.ChainManager or SystemRole.SystemAdmin))
            throw new ForbiddenException("The current role cannot change alert configuration.");
    }

    private static void Validate(UpdateAlertRule update)
    {
        if (update.Threshold is < 0 || update.Baseline is < 0)
            throw new ValidationException("threshold and baseline cannot be negative.");
        if (update.WindowMinutes is <= 0 or > 43_200)
            throw new ValidationException("windowMinutes must be between 1 and 43200.");
        if (update.CooldownMinutes is < 0 or > 43_200)
            throw new ValidationException("cooldownMinutes must be between 0 and 43200.");
        if (update.ConfigJson is { Length: > 4000 })
            throw new ValidationException("configJson cannot exceed 4000 characters.");
    }

    private static AlertListItem ToListItem(Alert x) => new(x.Id, x.RuleCode, x.BranchId, x.Severity.ToString(),
        x.Status.ToString(), x.Title, x.DetectedAt, x.DetectedValue, x.ThresholdValue);

    private static AlertDetail ToDetail(Alert x) => new(x.Id, x.RuleId, x.RuleCode, x.TenantId, x.BranchId,
        x.Severity.ToString(), x.Status.ToString(), x.Title, x.Message, x.DetectedValue, x.ThresholdValue,
        x.PayloadJson, x.DetectedAt, x.AcknowledgedAt, x.AcknowledgedBy, x.ResolvedAt, x.ResolvedBy, x.DedupKey);

    private static AlertRuleView ToRuleView(AlertRule x) => new(x.Id, x.Code, x.Name, x.Description, x.Severity.ToString(),
        x.IsEnabled, x.Threshold, x.Baseline, x.WindowMinutes, x.CooldownMinutes, x.BranchId, x.ConfigJson, "PENDING");
}
