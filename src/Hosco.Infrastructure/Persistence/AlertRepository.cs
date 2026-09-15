using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hosco.Infrastructure.Persistence;

public sealed class AlertRepository(HoscoDbContext db) : IAlertRepository
{
    public async Task<IReadOnlyList<AlertRule>> GetEnabledRulesAsync(CancellationToken cancellationToken) =>
        await db.AlertRules.AsNoTracking().Where(x => x.IsEnabled).OrderBy(x => x.TenantId).ThenBy(x => x.Code).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AlertRule>> GetRulesAsync(ReportingScope scope, CancellationToken cancellationToken)
    {
        var query = db.AlertRules.AsNoTracking().Where(x => x.TenantId == scope.TenantId);
        if (scope.RestrictedBranchIds is not null)
        {
            var ids = scope.RestrictedBranchIds.ToArray();
            query = query.Where(x => x.BranchId == null || ids.Contains(x.BranchId.Value));
        }
        return await query.OrderBy(x => x.Code).ToListAsync(cancellationToken);
    }

    public async Task<AlertRule?> GetRuleAsync(Guid id, ReportingScope scope, CancellationToken cancellationToken)
    {
        var query = db.AlertRules.Where(x => x.Id == id && x.TenantId == scope.TenantId);
        if (scope.RestrictedBranchIds is not null)
        {
            var ids = scope.RestrictedBranchIds.ToArray();
            query = query.Where(x => x.BranchId == null || ids.Contains(x.BranchId.Value));
        }
        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<Alert>> GetAlertsAsync(ReportingScope scope, AlertFilter filter, CancellationToken cancellationToken)
    {
        var query = ScopedAlerts(scope);
        if (filter.BranchId.HasValue) query = query.Where(x => x.BranchId == filter.BranchId);
        if (filter.Severity.HasValue) query = query.Where(x => x.Severity == filter.Severity);
        if (filter.Status.HasValue) query = query.Where(x => x.Status == filter.Status);
        if (db.Database.IsSqlite())
        {
            var materialized = await query.AsNoTracking().ToListAsync(cancellationToken);
            var filtered = materialized.Where(x => (!filter.From.HasValue || x.DetectedAt >= filter.From) &&
                                                     (!filter.To.HasValue || x.DetectedAt <= filter.To))
                .OrderByDescending(x => x.DetectedAt).ToList();
            return new PagedResult<Alert>(filtered.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList(), filtered.Count);
        }
        if (filter.From.HasValue) query = query.Where(x => x.DetectedAt >= filter.From);
        if (filter.To.HasValue) query = query.Where(x => x.DetectedAt <= filter.To);
        var total = await query.CountAsync(cancellationToken);
        var rows = await query.AsNoTracking().OrderByDescending(x => x.DetectedAt)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<Alert>(rows, total);
    }

    public Task<Alert?> GetAlertAsync(Guid id, ReportingScope scope, CancellationToken cancellationToken) =>
        ScopedAlerts(scope).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<AlertSummary> GetSummaryAsync(ReportingScope scope, CancellationToken cancellationToken)
    {
        var rows = await ScopedAlerts(scope).AsNoTracking().Select(x => new { x.Status, x.Severity }).ToListAsync(cancellationToken);
        return new AlertSummary(
            rows.Count(x => x.Status == AlertStatus.Open),
            rows.Count(x => x.Status != AlertStatus.Resolved && x.Severity == AlertSeverity.Critical),
            rows.Count(x => x.Status != AlertStatus.Open),
            rows.Count(x => x.Status == AlertStatus.Resolved));
    }

    public async Task<bool> ExistsWithinCooldownAsync(Guid tenantId, string dedupKey, DateTimeOffset since, CancellationToken cancellationToken)
    {
        var query = db.Alerts.AsNoTracking().Where(x => x.TenantId == tenantId && x.DedupKey == dedupKey);
        if (db.Database.IsSqlite())
            return (await query.Select(x => x.DetectedAt).ToListAsync(cancellationToken)).Any(x => x >= since);
        return await query.AnyAsync(x => x.DetectedAt >= since, cancellationToken);
    }

    public Task AddAsync(Alert alert, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        db.Alerts.Add(alert);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    private IQueryable<Alert> ScopedAlerts(ReportingScope scope)
    {
        var query = db.Alerts.Where(x => x.TenantId == scope.TenantId);
        if (scope.RestrictedBranchIds is not null)
        {
            var ids = scope.RestrictedBranchIds.ToArray();
            query = query.Where(x => x.BranchId == null || ids.Contains(x.BranchId.Value));
        }
        return query;
    }
}
