using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Application.Services;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;

namespace Hosco.UnitTests;

public sealed class AlertEvaluatorTests
{
    [Theory]
    [InlineData(29.99, null)]
    [InlineData(30, AlertSeverity.High)]
    [InlineData(50, AlertSeverity.Critical)]
    public void Al01_uses_high_and_critical_relative_increase_boundaries(decimal increase, AlertSeverity? expected) =>
        Assert.Equal(expected, AlertRuleConfiguration.CancellationSeverity(increase, new(30, 50, 7, 120)));

    [Theory]
    [InlineData(70, null)]
    [InlineData(69.99, AlertSeverity.High)]
    [InlineData(49.99, AlertSeverity.Critical)]
    public void Al02_uses_strict_revenue_drop_boundaries(decimal currentPercent, AlertSeverity? expected) =>
        Assert.Equal(expected, AlertRuleConfiguration.RevenueDropSeverity(currentPercent, 100, new(70, 50, 7, [new(11, 13)], 60)));

    [Theory]
    [InlineData(9, 10, AlertSeverity.Medium)]
    [InlineData(5, 10, AlertSeverity.High)]
    [InlineData(0, 10, AlertSeverity.Critical)]
    [InlineData(11, 10, null)]
    public void Al03_uses_available_stock_boundaries(int available, int safety, AlertSeverity? expected) =>
        Assert.Equal(expected, AlertRuleConfiguration.DangerousStockSeverity(available, safety, new(50)));

    [Theory]
    [InlineData(16, 10, 9, AlertSeverity.Medium)]
    [InlineData(16, 10, 10, AlertSeverity.High)]
    [InlineData(14, 10, 10, AlertSeverity.High)]
    [InlineData(9, 10, 10, null)]
    public void Al04_uses_branch_or_absolute_threshold_and_minimum_sample(decimal observed, decimal baseline, int sample, AlertSeverity? expected) =>
        Assert.Equal(expected, AlertRuleConfiguration.EmployeeSeverity(observed, baseline, sample, new(15, 7, 10)));

    [Theory]
    [InlineData(40, false, null)]
    [InlineData(40.01, false, AlertSeverity.High)]
    [InlineData(60.01, false, AlertSeverity.Critical)]
    [InlineData(0, true, AlertSeverity.High)]
    public void Al05_uses_discount_and_floor_price_boundaries(decimal discount, bool belowFloor, AlertSeverity? expected) =>
        Assert.Equal(expected, AlertRuleConfiguration.PriceSeverity(discount, belowFloor, new(40, 60)));

    [Fact]
    public async Task Evaluator_preserves_per_signal_severity_baseline_and_dedup_scope()
    {
        var store = new SignalStore([new AlertSignal(Branch, "employee-1", AlertSeverity.Critical,
            "title", "message", 50, 30, 12, "{}")]);
        var result = await new CancellationRateAlertEvaluator(store).EvaluateAsync(Rule("AL-01"), Now, default);

        var candidate = Assert.Single(result);
        Assert.Equal(AlertSeverity.Critical, candidate.Severity);
        Assert.Equal(12, candidate.BaselineValue);
        Assert.Contains($":{Branch:N}:AL-01:employee-1", candidate.DedupKey);
        Assert.Equal(Now, store.CapturedNow);
    }

    [Fact]
    public async Task Disabled_rule_is_not_evaluated()
    {
        var store = new SignalStore([]);
        var rule = Rule("AL-01");
        rule.IsEnabled = false;
        Assert.Empty(await new CancellationRateAlertEvaluator(store).EvaluateAsync(rule, Now, default));
        Assert.Null(store.CapturedNow);
    }

    private static readonly DateTimeOffset Now = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static AlertRule Rule(string code) => new()
    {
        Id = Guid.NewGuid(), TenantId = Tenant, BranchId = Branch, Code = code, Name = code,
        Description = "test", Severity = AlertSeverity.High, IsEnabled = true, Threshold = 10,
        WindowMinutes = 60, CooldownMinutes = 30, ConfigJson = "{}", CreatedAt = Now, UpdatedAt = Now
    };

    private sealed class SignalStore(IReadOnlyList<AlertSignal> signals) : IAlertSignalDataStore
    {
        public DateTimeOffset? CapturedNow { get; private set; }
        private Task<IReadOnlyList<AlertSignal>> Result(DateTimeOffset now) { CapturedNow = now; return Task.FromResult(signals); }
        public Task<IReadOnlyList<AlertSignal>> GetCancellationRateSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct) => Result(now);
        public Task<IReadOnlyList<AlertSignal>> GetRevenueDropSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct) => Result(now);
        public Task<IReadOnlyList<AlertSignal>> GetDangerousStockSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct) => Result(now);
        public Task<IReadOnlyList<AlertSignal>> GetEmployeeCancellationSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct) => Result(now);
        public Task<IReadOnlyList<AlertSignal>> GetPriceDiscountSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct) => Result(now);
    }
}
