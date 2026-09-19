using Hosco.Application.Models;
using Hosco.Domain.Enums;

namespace Hosco.Application.Services;

public interface IBusinessTime
{
    TimeSpan UtcOffset { get; }
    DateTimeOffset ToBusinessTime(DateTimeOffset instant);
    DateOnly GetBusinessDate(DateTimeOffset instant);
    TimeOnly GetBusinessClockTime(DateTimeOffset instant);
    DateTimeOffset StartOfBusinessDayUtc(DateOnly date);
}

public sealed class VietnamBusinessTime : IBusinessTime
{
    public static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    public TimeSpan UtcOffset => VietnamOffset;
    public DateTimeOffset ToBusinessTime(DateTimeOffset instant) => instant.ToOffset(VietnamOffset);
    public DateOnly GetBusinessDate(DateTimeOffset instant) => DateOnly.FromDateTime(ToBusinessTime(instant).DateTime);
    public TimeOnly GetBusinessClockTime(DateTimeOffset instant) => TimeOnly.FromDateTime(ToBusinessTime(instant).DateTime);
    public DateTimeOffset StartOfBusinessDayUtc(DateOnly date) =>
        new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), VietnamOffset).ToUniversalTime();
}

public sealed record KpiLineSnapshot(
    Guid ProductId,
    string Sku,
    string Name,
    string Currency,
    int Quantity,
    decimal UnitPrice,
    decimal UnitCostAtSale,
    decimal LineTotal,
    int ReturnedQuantity,
    decimal ReturnedValue);

public sealed record KpiOrderSnapshot(
    Guid OrderId,
    Guid BranchId,
    Guid EmployeeId,
    OrderStatus Status,
    DateTimeOffset OrderedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<KpiLineSnapshot> Lines,
    bool HasCompletedRefund);

public sealed record KpiSnapshot(
    decimal Revenue,
    decimal Gmv,
    int TotalOrders,
    decimal? Aov,
    decimal Cogs,
    decimal GrossProfit,
    decimal? GrossMarginPercent,
    decimal CancellationReturnRate);

public interface IKpiCalculator
{
    KpiSnapshot Calculate(IEnumerable<KpiOrderSnapshot> orders);
    int ValidQuantity(KpiOrderSnapshot order, KpiLineSnapshot line);
    decimal NetRevenue(KpiOrderSnapshot order, KpiLineSnapshot line);
    bool IsRecognizedSale(OrderStatus status);
}

public interface IKpiSnapshotStore
{
    Task<IReadOnlyList<KpiOrderSnapshot>> LoadAsync(
        ReportingScope scope,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken);
}

public sealed class KpiCalculator : IKpiCalculator
{
    public bool IsRecognizedSale(OrderStatus status) => status is
        OrderStatus.Completed or OrderStatus.Delivered or OrderStatus.Returned or OrderStatus.PartiallyReturned;

    public int ValidQuantity(KpiOrderSnapshot order, KpiLineSnapshot line)
    {
        if (!IsRecognizedSale(order.Status)) return 0;
        var returned = EffectiveReturnedQuantity(order, line);
        return Math.Max(0, line.Quantity - returned);
    }

    public decimal NetRevenue(KpiOrderSnapshot order, KpiLineSnapshot line)
    {
        if (!IsRecognizedSale(order.Status)) return 0m;
        var recognizedLineValue = Math.Max(0m, line.LineTotal);
        return Math.Max(0m, recognizedLineValue - EffectiveReturnedValue(order, line));
    }

    public KpiSnapshot Calculate(IEnumerable<KpiOrderSnapshot> source)
    {
        var orders = source.GroupBy(x => x.OrderId).Select(x => x.First()).ToList();
        var recognized = orders.Where(x => IsRecognizedSale(x.Status)).ToList();
        var revenue = RoundVnd(recognized.Sum(order => order.Lines.Sum(line => NetRevenue(order, line))));
        var gmv = RoundVnd(recognized.Sum(order => order.Lines.Sum(line => line.UnitPrice * ValidQuantity(order, line))));
        var totalOrders = recognized.Select(x => x.OrderId).Distinct().Count();
        var cogs = RoundVnd(recognized.Sum(order => order.Lines.Sum(line => line.UnitCostAtSale * ValidQuantity(order, line))));
        var grossProfit = RoundVnd(revenue - cogs);
        var affectedOrders = orders.Count(x => x.Status is OrderStatus.Cancelled or OrderStatus.Returned or OrderStatus.PartiallyReturned || x.HasCompletedRefund);
        var cancellationReturnRate = orders.Count == 0 ? 0m : decimal.Round(100m * affectedOrders / orders.Count, 2, MidpointRounding.AwayFromZero);
        return new KpiSnapshot(
            revenue,
            gmv,
            totalOrders,
            totalOrders == 0 ? null : decimal.Round(revenue / totalOrders, 1, MidpointRounding.AwayFromZero),
            cogs,
            grossProfit,
            revenue == 0 ? null : decimal.Round(100m * grossProfit / revenue, 2, MidpointRounding.AwayFromZero),
            cancellationReturnRate);
    }

    private static int EffectiveReturnedQuantity(KpiOrderSnapshot order, KpiLineSnapshot line) =>
        order.Status == OrderStatus.Returned && line.ReturnedQuantity == 0
            ? line.Quantity
            : Math.Clamp(line.ReturnedQuantity, 0, line.Quantity);

    private static decimal EffectiveReturnedValue(KpiOrderSnapshot order, KpiLineSnapshot line)
    {
        var recognizedLineValue = Math.Max(0m, line.LineTotal);
        return order.Status == OrderStatus.Returned && line.ReturnedValue == 0
            ? recognizedLineValue
            : Math.Clamp(line.ReturnedValue, 0m, recognizedLineValue);
    }

    private static decimal RoundVnd(decimal value) => decimal.Round(value, 0, MidpointRounding.AwayFromZero);
}
