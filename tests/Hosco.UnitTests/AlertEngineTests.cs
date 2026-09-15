using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Application.Services;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;

namespace Hosco.UnitTests;

public sealed class AlertEngineTests
{
    [Fact]
    public async Task Same_alert_is_suppressed_within_cooldown_and_allowed_afterwards()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 6, 30, 12, 0, 0, TimeSpan.Zero));
        var rule = Rule(Guid.NewGuid(), Guid.NewGuid(), 60);
        var repository = new MemoryRepository([rule]);
        var notifications = new NotificationSpy();
        var engine = new AlertEngine(repository, [new CandidateEvaluator()], notifications, clock);

        var first = await engine.EvaluateAllAsync(default);
        clock.Advance(TimeSpan.FromMinutes(10));
        var withinCooldown = await engine.EvaluateAllAsync(default);
        clock.Advance(TimeSpan.FromMinutes(51));
        var afterCooldown = await engine.EvaluateAllAsync(default);

        Assert.Equal(2, repository.Alerts.Count);
        Assert.Equal(1, first.CreatedAlerts);
        Assert.Equal(1, withinCooldown.SuppressedDuplicates);
        Assert.Equal(1, afterCooldown.CreatedAlerts);
        Assert.Equal(2, notifications.Count);
    }

    [Fact]
    public async Task Different_tenant_and_branch_candidates_do_not_deduplicate_each_other()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 6, 30, 12, 0, 0, TimeSpan.Zero));
        var tenantA = Guid.NewGuid(); var tenantB = Guid.NewGuid();
        var repository = new MemoryRepository([Rule(tenantA, Guid.NewGuid(), 60), Rule(tenantB, Guid.NewGuid(), 60)]);
        var result = await new AlertEngine(repository, [new CandidateEvaluator()], new NotificationSpy(), clock).EvaluateAllAsync(default);
        Assert.Equal(2, result.CreatedAlerts);
        Assert.Equal(2, repository.Alerts.Select(x => x.TenantId).Distinct().Count());
        Assert.Equal(2, repository.Alerts.Select(x => x.BranchId).Distinct().Count());
    }

    [Fact]
    public async Task One_rule_failure_does_not_stop_remaining_rules()
    {
        var clock = new FakeClock(DateTimeOffset.UtcNow);
        var good = Rule(Guid.NewGuid(), Guid.NewGuid(), 10);
        var bad = Rule(Guid.NewGuid(), Guid.NewGuid(), 10); bad.Code = "AL-X";
        var result = await new AlertEngine(new MemoryRepository([bad, good]), [new CandidateEvaluator()], new NotificationSpy(), clock)
            .EvaluateAllAsync(default);
        Assert.Equal(1, result.FailedRules);
        Assert.Equal(1, result.CreatedAlerts);
    }

    private static AlertRule Rule(Guid tenantId, Guid branchId, int cooldown) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenantId,
        BranchId = branchId,
        Code = "AL-01",
        Name = "test",
        Description = "test",
        Severity = AlertSeverity.Warning,
        IsEnabled = true,
        Threshold = 10,
        WindowMinutes = 60,
        CooldownMinutes = cooldown,
        ConfigJson = "{}",
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private sealed class CandidateEvaluator : IAlertRuleEvaluator
    {
        public string RuleCode => "AL-01";
        public Task<IReadOnlyList<AlertCandidate>> EvaluateAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<AlertCandidate>>([new(rule.Id, rule.Code, rule.TenantId, rule.BranchId,
                rule.Severity, "title", "message", 20, 10,
                $"{rule.TenantId:N}:{rule.BranchId:N}:{rule.Code}:entity", "{}", now)]);
    }

    private sealed class FakeClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan value) => _now += value;
    }

    private sealed class NotificationSpy : INotificationSender
    {
        public int Count { get; private set; }
        public Task SendAsync(Alert alert, CancellationToken ct) { Count++; return Task.CompletedTask; }
    }

    private sealed class MemoryRepository(IReadOnlyList<AlertRule> rules) : IAlertRepository
    {
        public List<Alert> Alerts { get; } = [];
        public Task<IReadOnlyList<AlertRule>> GetEnabledRulesAsync(CancellationToken ct) => Task.FromResult(rules);
        public Task<bool> ExistsWithinCooldownAsync(Guid tenantId, string dedupKey, DateTimeOffset since, CancellationToken ct) =>
            Task.FromResult(Alerts.Any(x => x.TenantId == tenantId && x.DedupKey == dedupKey && x.DetectedAt >= since));
        public Task AddAsync(Alert alert, CancellationToken ct) { Alerts.Add(alert); return Task.CompletedTask; }
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<AlertRule>> GetRulesAsync(ReportingScope scope, CancellationToken ct) => Task.FromResult(rules);
        public Task<AlertRule?> GetRuleAsync(Guid id, ReportingScope scope, CancellationToken ct) => Task.FromResult(rules.FirstOrDefault(x => x.Id == id));
        public Task<PagedResult<Alert>> GetAlertsAsync(ReportingScope scope, AlertFilter filter, CancellationToken ct) => Task.FromResult(new PagedResult<Alert>(Alerts, Alerts.Count));
        public Task<Alert?> GetAlertAsync(Guid id, ReportingScope scope, CancellationToken ct) => Task.FromResult(Alerts.FirstOrDefault(x => x.Id == id));
        public Task<AlertSummary> GetSummaryAsync(ReportingScope scope, CancellationToken ct) => Task.FromResult(new AlertSummary(0, 0, 0, 0));
    }
}
