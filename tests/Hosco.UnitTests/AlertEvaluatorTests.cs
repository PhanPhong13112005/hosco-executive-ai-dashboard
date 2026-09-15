using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Application.Services;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;

namespace Hosco.UnitTests;

public sealed class AlertEvaluatorTests
{
    [Fact]
    public async Task Cancellation_rate_triggers_at_threshold_boundary()
    {
        var store = new SignalStore([Signal(20, 20)]);
        var result = await new CancellationRateAlertEvaluator(store).EvaluateAsync(Rule("AL-01"), Now, default);
        Assert.Single(result);
        Assert.Equal(Now, store.CapturedNow);
    }

    [Fact]
    public async Task Revenue_drop_requires_strictly_more_than_threshold()
    {
        var store = new SignalStore([Signal(30, 30)]);
        var evaluator = new RevenueDropAlertEvaluator(store);
        Assert.Empty(await evaluator.EvaluateAsync(Rule("AL-02"), Now, default));
        store.Signals = [Signal(30.01m, 30)];
        Assert.Single(await evaluator.EvaluateAsync(Rule("AL-02"), Now, default));
    }

    [Fact]
    public async Task Dangerous_stock_triggers_at_or_below_threshold()
    {
        var store = new SignalStore([Signal(8, 8), Signal(9, 8)]);
        var result = await new DangerousStockAlertEvaluator(store).EvaluateAsync(Rule("AL-03"), Now, default);
        Assert.Single(result);
        Assert.Contains(":AL-03:", result[0].DedupKey);
    }

    [Fact]
    public async Task Employee_anomaly_triggers_at_count_boundary()
    {
        var result = await new EmployeeCancellationAlertEvaluator(new SignalStore([Signal(5, 5)]))
            .EvaluateAsync(Rule("AL-04"), Now, default);
        Assert.Single(result);
    }

    [Fact]
    public async Task Discount_anomaly_does_not_trigger_below_threshold()
    {
        var result = await new PriceDiscountAlertEvaluator(new SignalStore([Signal(24.99m, 25)]))
            .EvaluateAsync(Rule("AL-05"), Now, default);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Disabled_rule_is_not_evaluated()
    {
        var store = new SignalStore([Signal(100, 1)]);
        var rule = Rule("AL-01");
        rule.IsEnabled = false;
        Assert.Empty(await new CancellationRateAlertEvaluator(store).EvaluateAsync(rule, Now, default));
        Assert.Null(store.CapturedNow);
    }

    private static readonly DateTimeOffset Now = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Branch = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static AlertSignal Signal(decimal detected, decimal threshold) =>
        new(Branch, "entity-1", "title", "message", detected, threshold, "{}");

    private static AlertRule Rule(string code) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Tenant,
        Code = code,
        Name = code,
        Description = "test",
        Severity = AlertSeverity.Warning,
        IsEnabled = true,
        Threshold = 10,
        WindowMinutes = 60,
        CooldownMinutes = 30,
        ConfigJson = "{}",
        CreatedAt = Now,
        UpdatedAt = Now
    };

    private sealed class SignalStore(IReadOnlyList<AlertSignal> signals) : IAlertSignalDataStore
    {
        public IReadOnlyList<AlertSignal> Signals { get; set; } = signals;
        public DateTimeOffset? CapturedNow { get; private set; }
        private Task<IReadOnlyList<AlertSignal>> Result(DateTimeOffset now) { CapturedNow = now; return Task.FromResult(Signals); }
        public Task<IReadOnlyList<AlertSignal>> GetCancellationRateSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct) => Result(now);
        public Task<IReadOnlyList<AlertSignal>> GetRevenueDropSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct) => Result(now);
        public Task<IReadOnlyList<AlertSignal>> GetDangerousStockSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct) => Result(now);
        public Task<IReadOnlyList<AlertSignal>> GetEmployeeCancellationSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct) => Result(now);
        public Task<IReadOnlyList<AlertSignal>> GetPriceDiscountSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct) => Result(now);
    }
}
