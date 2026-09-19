using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Application.Services;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;

namespace Hosco.UnitTests;

public sealed class AlertServiceTests
{
    [Fact]
    public async Task Acknowledge_and_resolve_capture_actor_and_time()
    {
        var tenant = Guid.NewGuid(); var userId = Guid.NewGuid();
        var alert = NewAlert(tenant);
        var repository = new Repository(alert);
        var clock = new Clock(new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero));
        var service = Service(repository, new User(tenant, userId, new HashSet<SystemRole> { SystemRole.Owner }), clock);

        var acknowledged = await service.AcknowledgeAsync(alert.Id, default);
        Assert.Equal("Acknowledged", acknowledged.Status);
        Assert.Equal(userId, acknowledged.AcknowledgedBy);
        Assert.Equal(clock.GetUtcNow(), acknowledged.AcknowledgedAt);

        clock.Advance(TimeSpan.FromMinutes(5));
        var resolved = await service.ResolveAsync(alert.Id, new ResolveAlertRequest("completed"), default);
        Assert.Equal("Resolved", resolved.Status);
        Assert.Equal(userId, resolved.ResolvedBy);
        Assert.Equal(clock.GetUtcNow(), resolved.ResolvedAt);
    }

    [Fact]
    public async Task Resolved_alert_cannot_transition_back_to_acknowledged()
    {
        var tenant = Guid.NewGuid(); var alert = NewAlert(tenant); alert.Status = AlertStatus.Resolved;
        var service = Service(new Repository(alert), new User(tenant, Guid.NewGuid(), new HashSet<SystemRole> { SystemRole.Owner }), new Clock(DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<ValidationException>(() => service.AcknowledgeAsync(alert.Id, default));
    }

    [Theory]
    [InlineData("AL-04")]
    [InlineData("AL-05")]
    public async Task Resolve_requires_note_for_employee_and_price_rules(string ruleCode)
    {
        var tenant = Guid.NewGuid(); var alert = NewAlert(tenant); alert.RuleCode = ruleCode;
        var role = ruleCode == "AL-04" ? SystemRole.BranchManager : SystemRole.Owner;
        var service = Service(new Repository(alert), new User(tenant, Guid.NewGuid(), new HashSet<SystemRole> { role }), new Clock(DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<ValidationException>(() => service.ResolveAsync(alert.Id, new ResolveAlertRequest(), default));
        var resolved = await service.ResolveAsync(alert.Id, new ResolveAlertRequest("Đã kiểm tra và xử lý."), default);
        Assert.Equal("Đã kiểm tra và xử lý.", resolved.ResolutionNote);
    }

    [Theory]
    [InlineData(SystemRole.Owner)]
    [InlineData(SystemRole.ChainManager)]
    [InlineData(SystemRole.SystemAdmin)]
    public async Task Al04_actions_are_restricted_to_branch_manager(SystemRole role)
    {
        var tenant = Guid.NewGuid(); var alert = NewAlert(tenant); alert.RuleCode = "AL-04";
        var service = Service(new Repository(alert), new User(tenant, Guid.NewGuid(), new HashSet<SystemRole> { role }), new Clock(DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.AcknowledgeAsync(alert.Id, default));
    }

    [Fact]
    public async Task Branch_manager_cannot_update_rule_configuration()
    {
        var tenant = Guid.NewGuid(); var repository = new Repository(NewAlert(tenant));
        var service = Service(repository, new User(tenant, Guid.NewGuid(), new HashSet<SystemRole> { SystemRole.BranchManager }), new Clock(DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateRuleAsync(Guid.NewGuid(), new UpdateAlertRule(IsEnabled: false), default));
    }

    [Fact]
    public void Alert_filter_rejects_invalid_range_and_page()
    {
        Assert.Throws<ValidationException>(() => new AlertFilter(Page: 0).Validate());
        Assert.Throws<ValidationException>(() => new AlertFilter(From: DateTimeOffset.UtcNow, To: DateTimeOffset.UtcNow.AddDays(-1)).Validate());
    }

    private static AlertService Service(Repository repository, User user, Clock clock) =>
        new(repository, new ScopeFactory(user), user, new Audit(), clock);

    private static Alert NewAlert(Guid tenant) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = tenant,
        RuleCode = "AL-01",
        Type = "AL-01",
        Severity = AlertSeverity.High,
        Status = AlertStatus.Open,
        Title = "title",
        Message = "message",
        PayloadJson = "{}",
        DedupKey = "key",
        DetectedAt = DateTimeOffset.UtcNow,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    private sealed record User(Guid TenantId, Guid UserId, IReadOnlySet<SystemRole> Roles) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public IReadOnlySet<Guid> BranchIds { get; } = new HashSet<Guid>();
    }

    private sealed class ScopeFactory(User user) : IReportingScopeFactory
    {
        public Task<ReportingScope> CreateAsync(Guid? branchId, CancellationToken ct) => Task.FromResult(new ReportingScope(user.TenantId, null));
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan value) => _now += value;
    }

    private sealed class Audit : IAuditWriter
    {
        public Task WriteAsync(string action, string resourceType, string? resourceId, string? queryId, Guid? branchId, object? metadata, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class Repository(Alert alert) : IAlertRepository
    {
        private readonly List<Alert> _alerts = [alert];
        public Task<Alert?> GetAlertAsync(Guid id, ReportingScope scope, CancellationToken ct) => Task.FromResult(_alerts.FirstOrDefault(x => x.Id == id && x.TenantId == scope.TenantId));
        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<AlertRule>> GetEnabledRulesAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<AlertRule>>([]);
        public Task<IReadOnlyList<AlertRule>> GetRulesAsync(ReportingScope scope, CancellationToken ct) => Task.FromResult<IReadOnlyList<AlertRule>>([]);
        public Task<AlertRule?> GetRuleAsync(Guid id, ReportingScope scope, CancellationToken ct) => Task.FromResult<AlertRule?>(null);
        public Task<PagedResult<Alert>> GetAlertsAsync(ReportingScope scope, AlertFilter filter, CancellationToken ct) => Task.FromResult(new PagedResult<Alert>(_alerts, _alerts.Count));
        public Task<AlertSummary> GetSummaryAsync(ReportingScope scope, CancellationToken ct) => Task.FromResult(new AlertSummary(1, 0, 0, 0));
        public Task<IReadOnlyList<Alert>> GetUnacknowledgedForEscalationAsync(DateTimeOffset now, CancellationToken ct) => Task.FromResult<IReadOnlyList<Alert>>([]);
        public Task<bool> ExistsWithinCooldownAsync(Guid tenantId, string dedupKey, DateTimeOffset since, CancellationToken ct) => Task.FromResult(false);
        public Task AddAsync(Alert item, CancellationToken ct) { _alerts.Add(item); return Task.CompletedTask; }
    }
}
