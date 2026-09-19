using Hosco.Application.Services;
using Hosco.Domain.Enums;

namespace Hosco.UnitTests;

public sealed class KpiCalculatorTests
{
    private readonly KpiCalculator _calculator = new();

    [Fact]
    public void Revenue_gmv_aov_and_profit_follow_canonical_discount_formula()
    {
        var snapshot = _calculator.Calculate([
            Order(OrderStatus.Completed, Line(quantity: 2, unitPrice: 100, unitCost: 40, lineTotal: 180)),
            Order(OrderStatus.Delivered, Line(quantity: 1, unitPrice: 50, unitCost: 20, lineTotal: 50)),
            Order(OrderStatus.Cancelled, Line(quantity: 9, unitPrice: 1_000, unitCost: 1, lineTotal: 9_000)),
            Order(OrderStatus.Pending, Line(quantity: 9, unitPrice: 1_000, unitCost: 1, lineTotal: 9_000))
        ]);

        Assert.Equal(230, snapshot.Revenue);
        Assert.Equal(250, snapshot.Gmv);
        Assert.Equal(2, snapshot.TotalOrders);
        Assert.Equal(115.0m, snapshot.Aov);
        Assert.Equal(100, snapshot.Cogs);
        Assert.Equal(130, snapshot.GrossProfit);
        Assert.Equal(56.52m, snapshot.GrossMarginPercent);
    }

    [Fact]
    public void Full_return_contributes_zero_revenue_gmv_and_cogs_but_is_post_completion_order()
    {
        var snapshot = _calculator.Calculate([Order(OrderStatus.Returned, Line(2, 100, 40, 180))]);

        Assert.Equal(0, snapshot.Revenue);
        Assert.Equal(0, snapshot.Gmv);
        Assert.Equal(0, snapshot.Cogs);
        Assert.Equal(1, snapshot.TotalOrders);
        Assert.Null(snapshot.GrossMarginPercent);
        Assert.Equal(100, snapshot.CancellationReturnRate);
    }

    [Fact]
    public void Partial_return_subtracts_allocated_value_quantity_and_cogs()
    {
        var snapshot = _calculator.Calculate([
            Order(OrderStatus.PartiallyReturned, Line(3, 100, 40, 270, returnedQuantity: 1, returnedValue: 90))
        ]);

        Assert.Equal(180, snapshot.Revenue);
        Assert.Equal(200, snapshot.Gmv);
        Assert.Equal(80, snapshot.Cogs);
        Assert.Equal(100, snapshot.GrossProfit);
    }

    [Fact]
    public void Zero_orders_returns_null_aov_and_margin()
    {
        var snapshot = _calculator.Calculate([]);
        Assert.Null(snapshot.Aov);
        Assert.Null(snapshot.GrossMarginPercent);
        Assert.Equal(0, snapshot.CancellationReturnRate);
    }

    [Fact]
    public void Cancellation_return_rate_counts_each_order_once_over_all_created_orders()
    {
        var returnedWithRefund = Order(OrderStatus.Returned, Line(1, 10, 2, 10), hasRefund: true);
        var snapshot = _calculator.Calculate([
            Order(OrderStatus.Pending, Line(1, 10, 2, 10)),
            Order(OrderStatus.Cancelled, Line(1, 10, 2, 10)),
            returnedWithRefund,
            Order(OrderStatus.Completed, Line(1, 10, 2, 10), hasRefund: true)
        ]);
        Assert.Equal(75m, snapshot.CancellationReturnRate);
    }

    [Fact]
    public void Vietnam_business_date_moves_2330_utc_to_next_day()
    {
        var time = new VietnamBusinessTime();
        var instant = new DateTimeOffset(2026, 6, 30, 23, 30, 0, TimeSpan.Zero);
        Assert.Equal(new DateOnly(2026, 7, 1), time.GetBusinessDate(instant));
        Assert.Equal(new TimeOnly(6, 30), time.GetBusinessClockTime(instant));
        Assert.Equal(new DateTimeOffset(2026, 6, 30, 17, 0, 0, TimeSpan.Zero), time.StartOfBusinessDayUtc(new DateOnly(2026, 7, 1)));
    }

    private static KpiOrderSnapshot Order(OrderStatus status, KpiLineSnapshot line, bool hasRefund = false) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), status,
            new DateTimeOffset(2026, 6, 30, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 30, 12, 1, 0, TimeSpan.Zero), [line], hasRefund);

    private static KpiLineSnapshot Line(int quantity, decimal unitPrice, decimal unitCost, decimal lineTotal,
        int returnedQuantity = 0, decimal returnedValue = 0) =>
        new(Guid.NewGuid(), $"SKU-{Guid.NewGuid():N}", "Product", "VND", quantity, unitPrice, unitCost,
            lineTotal, returnedQuantity, returnedValue);
}
