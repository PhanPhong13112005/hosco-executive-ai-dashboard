using Hosco.Application.Models;
using Hosco.Application.Services;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hosco.Infrastructure.Persistence;

public sealed class ReportingDataStore(
    HoscoDbContext db,
    IKpiCalculator kpis,
    IBusinessTime businessTime,
    TimeProvider timeProvider) : IReportingDataStore
{
    public async Task<IReadOnlyList<RevenuePoint>> GetRevenueTrendAsync(ReportingScope scope, ReportingFilter filter, CancellationToken ct)
    {
        var orders = await LoadKpiOrdersAsync(scope, filter, ct);
        return orders.GroupBy(x => businessTime.GetBusinessDate(x.OrderedAt)).OrderBy(x => x.Key)
            .Select(group => new RevenuePoint(group.Key, kpis.Calculate(group).Revenue, "VND")).ToList();
    }

    public async Task<PagedResult<OrderRow>> GetOrdersAsync(ReportingScope scope, ReportingFilter filter, CancellationToken ct)
    {
        var query = Orders(scope, filter);
        if (db.Database.IsSqlite())
        {
            var sqliteRows = await query.ToListAsync(ct);
            var filtered = ApplyDateFilter(sqliteRows, filter);
            filtered = (filter.SortBy?.ToLowerInvariant(), filter.SortDirection.ToLowerInvariant()) switch
            {
                ("amount", "asc") => filtered.OrderBy(x => x.TotalAmount),
                ("amount", _) => filtered.OrderByDescending(x => x.TotalAmount),
                ("ordernumber", "asc") => filtered.OrderBy(x => x.OrderNumber),
                ("ordernumber", _) => filtered.OrderByDescending(x => x.OrderNumber),
                ("orderedat", "asc") => filtered.OrderBy(x => x.OrderedAt),
                _ => filtered.OrderByDescending(x => x.OrderedAt)
            };
            var materialized = filtered.ToList();
            var page = materialized.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
                .Select(ToOrderRow).ToList();
            return new PagedResult<OrderRow>(page, materialized.Count);
        }

        var total = await query.CountAsync(ct);
        query = (filter.SortBy?.ToLowerInvariant(), filter.SortDirection.ToLowerInvariant()) switch
        {
            ("amount", "asc") => query.OrderBy(x => x.TotalAmount),
            ("amount", _) => query.OrderByDescending(x => x.TotalAmount),
            ("ordernumber", "asc") => query.OrderBy(x => x.OrderNumber),
            ("ordernumber", _) => query.OrderByDescending(x => x.OrderNumber),
            ("orderedat", "asc") => query.OrderBy(x => x.OrderedAt),
            _ => query.OrderByDescending(x => x.OrderedAt)
        };
        var rows = await query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .Select(x => new OrderRow(x.Id, x.OrderNumber, x.BranchId, x.Status.ToString(), x.OrderedAt, x.TotalAmount, x.Currency))
            .ToListAsync(ct);
        return new PagedResult<OrderRow>(rows, total);
    }

    public async Task<IReadOnlyList<ProductRankRow>> GetProductRankingAsync(ReportingScope scope, ReportingFilter filter, bool bottom, CancellationToken ct)
    {
        // Keep aggregation after materialization: SQL Server cannot translate the former
        // GroupBy key containing Product navigation members.
        var orders = await LoadKpiOrdersAsync(scope, filter, ct);
        var rows = orders.Where(x => kpis.IsRecognizedSale(x.Status))
            .SelectMany(order => order.Lines.Select(line => new
            {
                line.ProductId,
                line.Sku,
                line.Name,
                line.Currency,
                Quantity = kpis.ValidQuantity(order, line),
                Revenue = kpis.NetRevenue(order, line)
            }))
            .GroupBy(x => new { x.ProductId, x.Sku, x.Name, x.Currency })
            .Select(group => new ProductRankRow(
                group.Key.ProductId, group.Key.Sku, group.Key.Name,
                group.Sum(x => x.Quantity),
                decimal.Round(group.Sum(x => x.Revenue), 0, MidpointRounding.AwayFromZero),
                group.Key.Currency))
            .Where(x => x.Quantity > 0);
        var ranked = bottom
            ? rows.OrderBy(x => x.Quantity).ThenBy(x => x.Sku).ThenBy(x => x.ProductId)
            : rows.OrderByDescending(x => x.Quantity).ThenBy(x => x.Sku).ThenBy(x => x.ProductId);
        return ranked.Take(filter.PageSize).ToList();
    }

    public async Task<IReadOnlyList<DangerousInventoryRow>> GetDangerousInventoryAsync(ReportingScope scope, ReportingFilter filter, CancellationToken ct)
    {
        var query = db.Inventories.AsNoTracking()
            .Where(x => x.TenantId == scope.TenantId && x.QuantityOnHand - x.ReservedQuantity <= x.SafetyStock);
        query = ApplyBranchScope(query, scope, x => x.BranchId);
        return await query.OrderBy(x => x.QuantityOnHand - x.ReservedQuantity - x.SafetyStock)
            .ThenBy(x => x.Product.Sku).Take(filter.PageSize)
            .Select(x => new DangerousInventoryRow(
                x.BranchId, x.ProductId, x.Product.Sku, x.Product.Name,
                x.QuantityOnHand, x.ReservedQuantity, x.QuantityOnHand - x.ReservedQuantity,
                x.SafetyStock, x.Product.IsKeySku))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OrderTrendPoint>> GetOrderTrendAsync(ReportingScope scope, ReportingFilter filter, CancellationToken ct)
    {
        var orders = await LoadKpiOrdersAsync(scope, filter, ct);
        return orders.GroupBy(x => businessTime.GetBusinessDate(x.OrderedAt)).OrderBy(x => x.Key)
            .Select(group => new OrderTrendPoint(
                group.Key,
                kpis.Calculate(group).TotalOrders,
                group.Count(x => x.Status == OrderStatus.Cancelled),
                group.Count(x => x.Status is OrderStatus.Returned or OrderStatus.PartiallyReturned || x.HasCompletedRefund)))
            .ToList();
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync(ReportingScope scope, ReportingFilter filter, AlertSummary alertSummary, CancellationToken ct)
    {
        var snapshot = kpis.Calculate(await LoadKpiOrdersAsync(scope, filter, ct));
        var dangerous = await GetDangerousInventoryAsync(scope, filter with { PageSize = ReportingFilter.MaxPageSize }, ct);
        return new DashboardSummary(
            snapshot.Revenue, snapshot.Gmv, snapshot.TotalOrders, snapshot.Aov,
            snapshot.GrossProfit, snapshot.GrossMarginPercent, snapshot.CancellationReturnRate,
            dangerous.Count, "VND", alertSummary.Open, alertSummary.Urgent,
            "ImplementedFinalGd1",
            "Final GD1 KPI Dictionary; UTC+7; item-level returns allocated by RefundItem.");
    }

    public async Task<IReadOnlyList<KpiDrilldownPoint>> GetKpiDrilldownAsync(string metricCode, ReportingScope scope, ReportingFilter filter, CancellationToken ct)
    {
        if (metricCode.Equals("dangerous-stock", StringComparison.OrdinalIgnoreCase))
        {
            var dangerous = await GetDangerousInventoryAsync(scope, filter with { PageSize = ReportingFilter.MaxPageSize }, ct);
            return [new KpiDrilldownPoint(businessTime.GetBusinessDate(timeProvider.GetUtcNow()), dangerous.Count)];
        }

        var orders = await LoadKpiOrdersAsync(scope, filter, ct);
        return orders.GroupBy(x => businessTime.GetBusinessDate(x.OrderedAt)).OrderBy(x => x.Key).Select(group =>
        {
            var snapshot = kpis.Calculate(group);
            var value = metricCode.ToLowerInvariant() switch
            {
                "revenue" => snapshot.Revenue,
                "gmv" => snapshot.Gmv,
                "total-orders" => snapshot.TotalOrders,
                "aov" => snapshot.Aov ?? 0m,
                "gross-profit" => snapshot.GrossProfit,
                "cancel-return-rate" => snapshot.CancellationReturnRate,
                "sku-ranking" => group.Sum(order => order.Lines.Sum(line => kpis.ValidQuantity(order, line))),
                _ => throw new KeyNotFoundException($"Unknown metric code '{metricCode}'.")
            };
            return new KpiDrilldownPoint(group.Key, value);
        }).ToList();
    }

    public async Task<IReadOnlyList<BranchRow>> GetBranchesAsync(ReportingScope scope, CancellationToken ct)
    {
        var query = db.Branches.AsNoTracking().Where(x => x.TenantId == scope.TenantId && x.IsActive);
        if (scope.RestrictedBranchIds is not null)
        {
            var ids = scope.RestrictedBranchIds.ToArray();
            query = query.Where(x => ids.Contains(x.Id));
        }
        return await query.OrderBy(x => x.Name).Select(x => new BranchRow(x.Id, x.Code, x.Name)).ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, decimal?>> GetKpiSummaryAsync(ReportingScope scope, ReportingFilter filter, CancellationToken ct)
    {
        var snapshot = kpis.Calculate(await LoadKpiOrdersAsync(scope, filter, ct));
        var dangerous = await GetDangerousInventoryAsync(scope, filter with { PageSize = ReportingFilter.MaxPageSize }, ct);
        return new Dictionary<string, decimal?>
        {
            ["revenue"] = snapshot.Revenue,
            ["gmv"] = snapshot.Gmv,
            ["total-orders"] = snapshot.TotalOrders,
            ["aov"] = snapshot.Aov,
            ["gross-profit"] = snapshot.GrossProfit,
            ["cancel-return-rate"] = snapshot.CancellationReturnRate,
            ["sku-ranking"] = null,
            ["dangerous-stock"] = dangerous.Count
        };
    }

    public async Task<DateTimeOffset> GetLastUpdatedAtAsync(ReportingScope scope, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking().Where(x => x.TenantId == scope.TenantId);
        query = ApplyBranchScope(query, scope, x => x.BranchId);
        if (db.Database.IsSqlite())
            return (await query.Select(x => x.UpdatedAt).ToListAsync(ct)).DefaultIfEmpty(timeProvider.GetUtcNow()).Max();
        return await query.MaxAsync(x => (DateTimeOffset?)x.UpdatedAt, ct) ?? timeProvider.GetUtcNow();
    }

    private async Task<List<KpiOrderSnapshot>> LoadKpiOrdersAsync(ReportingScope scope, ReportingFilter filter, CancellationToken ct)
    {
        var orders = await MaterializedOrdersAsync(scope, filter, ct);
        if (orders.Count == 0) return [];
        var orderIds = orders.Select(x => x.Id).ToArray();
        var items = await db.OrderItems.AsNoTracking()
            .Where(x => x.TenantId == scope.TenantId && orderIds.Contains(x.OrderId))
            .Select(x => new ItemProjection(
                x.Id, x.OrderId, x.ProductId, x.Product.Sku, x.Product.Name, x.Product.Currency,
                x.Quantity, x.UnitPrice, x.UnitCostAtSale, x.LineTotal))
            .ToListAsync(ct);
        var itemIds = items.Select(x => x.Id).ToArray();
        var returnRows = itemIds.Length == 0
            ? []
            : await db.RefundItems.AsNoTracking()
                .Where(x => x.TenantId == scope.TenantId && itemIds.Contains(x.OrderItemId) && x.Refund.Status == RefundStatus.Completed)
                .Select(x => new ReturnProjection(x.OrderItemId, x.Quantity, x.ReturnedValue))
                .ToListAsync(ct);
        var returnByItem = returnRows.GroupBy(x => x.OrderItemId).ToDictionary(
            group => group.Key,
            group => new ReturnProjection(group.Key, group.Sum(x => x.Quantity), group.Sum(x => x.ReturnedValue)));
        var refundedOrderIds = (await db.Refunds.AsNoTracking()
            .Where(x => x.TenantId == scope.TenantId && orderIds.Contains(x.OrderId) && x.Status == RefundStatus.Completed)
            .Select(x => x.OrderId).ToListAsync(ct)).ToHashSet();
        var itemsByOrder = items.GroupBy(x => x.OrderId).ToDictionary(x => x.Key, x => x.ToList());

        return orders.Select(order => new KpiOrderSnapshot(
            order.Id, order.BranchId, order.EmployeeId, order.Status, order.OrderedAt, order.UpdatedAt,
            itemsByOrder.GetValueOrDefault(order.Id, []).Select(item =>
            {
                var returned = returnByItem.GetValueOrDefault(item.Id);
                return new KpiLineSnapshot(item.ProductId, item.Sku, item.Name, item.Currency,
                    item.Quantity, item.UnitPrice, item.UnitCostAtSale, item.LineTotal,
                    returned?.Quantity ?? 0, returned?.ReturnedValue ?? 0m);
            }).ToList(),
            refundedOrderIds.Contains(order.Id))).ToList();
    }

    private IQueryable<Order> Orders(ReportingScope scope, ReportingFilter filter)
    {
        var query = db.Orders.AsNoTracking().Where(x => x.TenantId == scope.TenantId);
        query = ApplyBranchScope(query, scope, x => x.BranchId);
        if (!db.Database.IsSqlite())
        {
            if (filter.From.HasValue) query = query.Where(x => x.OrderedAt >= filter.From.Value);
            if (filter.To.HasValue) query = query.Where(x => x.OrderedAt <= filter.To.Value);
        }
        return query;
    }

    private async Task<List<Order>> MaterializedOrdersAsync(ReportingScope scope, ReportingFilter filter, CancellationToken ct)
    {
        var rows = await Orders(scope, filter).ToListAsync(ct);
        return db.Database.IsSqlite() ? ApplyDateFilter(rows, filter).ToList() : rows;
    }

    private static IEnumerable<Order> ApplyDateFilter(IEnumerable<Order> rows, ReportingFilter filter) =>
        rows.Where(x => (!filter.From.HasValue || x.OrderedAt >= filter.From.Value) &&
                        (!filter.To.HasValue || x.OrderedAt <= filter.To.Value));

    private static OrderRow ToOrderRow(Order x) =>
        new(x.Id, x.OrderNumber, x.BranchId, x.Status.ToString(), x.OrderedAt, x.TotalAmount, x.Currency);

    private static IQueryable<T> ApplyBranchScope<T>(IQueryable<T> query, ReportingScope scope,
        System.Linq.Expressions.Expression<Func<T, Guid>> branchSelector)
    {
        if (scope.RestrictedBranchIds is null) return query;
        var ids = scope.RestrictedBranchIds.ToArray();
        var parameter = branchSelector.Parameters[0];
        var contains = System.Linq.Expressions.Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), [typeof(Guid)],
            System.Linq.Expressions.Expression.Constant(ids), branchSelector.Body);
        return query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(contains, parameter));
    }

    private sealed record ItemProjection(
        Guid Id, Guid OrderId, Guid ProductId, string Sku, string Name, string Currency,
        int Quantity, decimal UnitPrice, decimal UnitCostAtSale, decimal LineTotal);
    private sealed record ReturnProjection(Guid OrderItemId, int Quantity, decimal ReturnedValue);
}
