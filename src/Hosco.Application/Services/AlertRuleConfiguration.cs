using System.Text.Json;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;

namespace Hosco.Application.Services;

public sealed record PeakWindow(int StartHour, int EndHour);
public sealed record CancellationRuleConfig(decimal HighIncreasePercent, decimal CriticalIncreasePercent, int BaselineDays, int EscalateUnackedMinutes);
public sealed record RevenueDropRuleConfig(decimal HighRevenuePercent, decimal CriticalRevenuePercent, int BaselineDays, IReadOnlyList<PeakWindow> PeakWindows, int EscalateUnackedMinutes);
public sealed record DangerousStockRuleConfig(decimal HighSafetyPercent);
public sealed record EmployeeAnomalyRuleConfig(decimal AbsoluteRatePercent, int BaselineDays, int MinimumSample);
public sealed record PriceDiscountRuleConfig(decimal HighDiscountPercent, decimal CriticalDiscountPercent);

public static class AlertRuleConfiguration
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static CancellationRuleConfig Cancellation(AlertRule rule) => Read(rule,
        new CancellationRuleConfig(rule.Threshold ?? 30m, rule.Baseline ?? 50m, 7, 120));

    public static RevenueDropRuleConfig RevenueDrop(AlertRule rule) => Read(rule,
        new RevenueDropRuleConfig(rule.Threshold ?? 70m, rule.Baseline ?? 50m, 7,
            [new PeakWindow(11, 13), new PeakWindow(17, 20)], 60));

    public static DangerousStockRuleConfig DangerousStock(AlertRule rule) => Read(rule,
        new DangerousStockRuleConfig(rule.Threshold ?? 50m));

    public static EmployeeAnomalyRuleConfig Employee(AlertRule rule) => Read(rule,
        new EmployeeAnomalyRuleConfig(rule.Threshold ?? 15m, 7, 10));

    public static PriceDiscountRuleConfig Price(AlertRule rule) => Read(rule,
        new PriceDiscountRuleConfig(rule.Threshold ?? 40m, rule.Baseline ?? 60m));

    public static string DefaultJson(string code) => code switch
    {
        "AL-01" => JsonSerializer.Serialize(new CancellationRuleConfig(30m, 50m, 7, 120), JsonOptions),
        "AL-02" => JsonSerializer.Serialize(new RevenueDropRuleConfig(70m, 50m, 7,
            [new PeakWindow(11, 13), new PeakWindow(17, 20)], 60), JsonOptions),
        "AL-03" => JsonSerializer.Serialize(new DangerousStockRuleConfig(50m), JsonOptions),
        "AL-04" => JsonSerializer.Serialize(new EmployeeAnomalyRuleConfig(15m, 7, 10), JsonOptions),
        "AL-05" => JsonSerializer.Serialize(new PriceDiscountRuleConfig(40m, 60m), JsonOptions),
        _ => "{}"
    };

    public static AlertSeverity? CancellationSeverity(decimal increasePercent, CancellationRuleConfig config) =>
        increasePercent >= config.CriticalIncreasePercent ? AlertSeverity.Critical :
        increasePercent >= config.HighIncreasePercent ? AlertSeverity.High : null;

    public static AlertSeverity? RevenueDropSeverity(decimal currentRevenue, decimal baselineRevenue, RevenueDropRuleConfig config)
    {
        if (baselineRevenue <= 0) return null;
        var percent = 100m * currentRevenue / baselineRevenue;
        return percent < config.CriticalRevenuePercent ? AlertSeverity.Critical :
            percent < config.HighRevenuePercent ? AlertSeverity.High : null;
    }

    public static AlertSeverity? DangerousStockSeverity(int available, int safetyStock, DangerousStockRuleConfig config)
    {
        if (available <= 0) return AlertSeverity.Critical;
        if (available <= safetyStock * config.HighSafetyPercent / 100m) return AlertSeverity.High;
        return available <= safetyStock ? AlertSeverity.Medium : null;
    }

    public static AlertSeverity? EmployeeSeverity(decimal observedRate, decimal branchBaseline, int sampleSize, EmployeeAnomalyRuleConfig config)
    {
        if (observedRate <= branchBaseline && observedRate < config.AbsoluteRatePercent) return null;
        return sampleSize >= config.MinimumSample ? AlertSeverity.High : AlertSeverity.Medium;
    }

    public static AlertSeverity? PriceSeverity(decimal discountPercent, bool belowFloor, PriceDiscountRuleConfig config) =>
        discountPercent > config.CriticalDiscountPercent ? AlertSeverity.Critical :
        discountPercent > config.HighDiscountPercent || belowFloor ? AlertSeverity.High : null;

    private static T Read<T>(AlertRule rule, T fallback)
    {
        if (string.IsNullOrWhiteSpace(rule.ConfigJson) || rule.ConfigJson == "{}") return fallback;
        try { return JsonSerializer.Deserialize<T>(rule.ConfigJson, JsonOptions) ?? fallback; }
        catch (JsonException) { return fallback; }
    }
}
