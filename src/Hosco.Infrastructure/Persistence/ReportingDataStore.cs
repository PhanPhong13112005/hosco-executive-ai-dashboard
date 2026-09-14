using Hosco.Application.Models;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hosco.Infrastructure.Persistence;

public sealed class ReportingDataStore(HoscoDbContext db) : IReportingDataStore
{
    public async Task<IReadOnlyList<RevenuePoint>> GetRevenueTrendAsync(ReportingScope scope, ReportingFilter filter, CancellationToken ct)
    {
        var rows = await Orders(scope, filter).Where(x => x.Status == OrderStatus.Completed)
            .Select(x => new { x.OrderedAt, x.TotalAmount, x.Currency }).ToListAsync(ct);
        return rows.GroupBy(x => new { Date = DateOnly.FromDateTime(x.OrderedAt.UtcDateTime), x.Currency })
            .OrderBy(x => x.Key.Date).Select(x => new RevenuePoint(x.Key.Date, x.Sum(v => v.TotalAmount), x.Key.Currency)).ToList();
    }

    public async Task<PagedResult<OrderRow>> GetOrdersAsync(ReportingScope scope, ReportingFilter filter, CancellationToken ct)
    {
        var query = Orders(scope, filter);
        if (db.Database.IsSqlite())
        {
            var sqliteRows = await query.ToListAsync(ct);
            var filtered = sqliteRows.Where(x => (!filter.From.HasValue || x.OrderedAt >= filter.From) && (!filter.To.HasValue || x.OrderedAt <= filter.To));
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
                .Select(x => new OrderRow(x.Id, x.OrderNumber, x.BranchId, x.Status.ToString(), x.OrderedAt, x.TotalAmount, x.Currency)).ToList();
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
        if (db.Database.IsSqlite())
        {
            var sqliteOrders = await Orders(scope, filter).Where(x => x.Status == OrderStatus.Completed).ToListAsync(ct);
            var ids = sqliteOrders.Where(x => (!filter.From.HasValue || x.OrderedAt >= filter.From) && (!filter.To.HasValue || x.OrderedAt <= filter.To))
                .Select(x => x.Id).ToHashSet();
            var items = await db.OrderItems.AsNoTracking().Include(x => x.Product)
                .Where(x => x.TenantId == scope.TenantId && ids.Contains(x.OrderId)).ToListAsync(ct);
            var rows = items.GroupBy(x => new { x.ProductId, x.Product.Sku, x.Product.Name, x.Product.Currency })
                .Select(x => new ProductRankRow(x.Key.ProductId, x.Key.Sku, x.Key.Name, x.Sum(v => v.Quantity), x.Sum(v => v.LineTotal), x.Key.Currency));
            rows = bottom ? rows.OrderBy(x => x.Amount) : rows.OrderByDescending(x => x.Amount);
            return rows.Take(filter.PageSize).ToList();
        }
        var orderIds = Orders(scope, filter).Where(x => x.Status == OrderStatus.Completed).Select(x => x.Id);
        var grouped = db.OrderItems.AsNoTracking().Where(x => x.TenantId == scope.TenantId && orderIds.Contains(x.OrderId))
            .GroupBy(x => new { x.ProductId, x.Product.Sku, x.Product.Name, x.Product.Currency })
            .Select(x => new ProductRankRow(x.Key.ProductId, x.Key.Sku, x.Key.Name, x.Sum(v => v.Quantity), x.Sum(v => v.LineTotal), x.Key.Currency));
        grouped = bottom ? grouped.OrderBy(x => x.Amount) : grouped.OrderByDescending(x => x.Amount);
        return await grouped.Take(filter.PageSize).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DangerousInventoryRow>> GetDangerousInventoryAsync(ReportingScope scope, ReportingFilter filter, CancellationToken ct)
    {
        var query = db.Inventories.AsNoTracking().Where(x => x.TenantId == scope.TenantId && x.QuantityOnHand <= x.SafetyStock);
        query = ApplyBranchScope(query, scope, x => x.BranchId);
        return await query.OrderBy(x => x.QuantityOnHand - x.SafetyStock).Take(filter.PageSize)
            .Select(x => new DangerousInventoryRow(x.BranchId, x.ProductId, x.Product.Sku, x.Product.Name, x.QuantityOnHand, x.SafetyStock))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetTechnicalPreviewSummaryAsync(ReportingScope scope, ReportingFilter filter, CancellationToken ct)
    {
        var orders = await Orders(scope, filter).Select(x => new { x.Status, x.TotalAmount }).ToListAsync(ct);
        var completed = orders.Where(x => x.Status == OrderStatus.Completed).ToList();
        return new Dictionary<string, decimal>
        {
            ["revenue"] = completed.Sum(x => x.TotalAmount),
            ["gmv"] = orders.Sum(x => x.TotalAmount),
            ["total-orders"] = orders.Count,
            ["aov"] = completed.Count == 0 ? 0 : completed.Average(x => x.TotalAmount),
            ["cancel-return-rate"] = orders.Count == 0 ? 0 : 100m * orders.Count(x => x.Status is OrderStatus.Cancelled or OrderStatus.Returned) / orders.Count
        };
    }

    public async Task<DateTimeOffset> GetLastUpdatedAtAsync(ReportingScope scope, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking().Where(x => x.TenantId == scope.TenantId);
        query = ApplyBranchScope(query, scope, x => x.BranchId);
        if (db.Database.IsSqlite())
            return (await query.Select(x => x.UpdatedAt).ToListAsync(ct)).DefaultIfEmpty(DateTimeOffset.UtcNow).Max();
        return await query.MaxAsync(x => (DateTimeOffset?)x.UpdatedAt, ct) ?? DateTimeOffset.UtcNow;
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

    private static IQueryable<T> ApplyBranchScope<T>(IQueryable<T> query, ReportingScope scope, System.Linq.Expressions.Expression<Func<T, Guid>> branchSelector)
    {
        if (scope.RestrictedBranchIds is null) return query;
        var ids = scope.RestrictedBranchIds.ToArray();
        var parameter = branchSelector.Parameters[0];
        var contains = System.Linq.Expressions.Expression.Call(typeof(Enumerable), nameof(Enumerable.Contains), [typeof(Guid)],
            System.Linq.Expressions.Expression.Constant(ids), branchSelector.Body);
        return query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(contains, parameter));
    }
}
