using Hosco.Application.Models;
using Hosco.Application.Services;
using Hosco.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hosco.Infrastructure.Persistence;

public sealed class KpiSnapshotStore(HoscoDbContext db) : IKpiSnapshotStore
{
    public async Task<IReadOnlyList<KpiOrderSnapshot>> LoadAsync(
        ReportingScope scope, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking().Where(x => x.TenantId == scope.TenantId);
        if (scope.RestrictedBranchIds is not null)
        {
            var branchIds = scope.RestrictedBranchIds.ToArray();
            query = query.Where(x => branchIds.Contains(x.BranchId));
        }
        if (!db.Database.IsSqlite())
        {
            if (from.HasValue) query = query.Where(x => x.OrderedAt >= from.Value);
            if (to.HasValue) query = query.Where(x => x.OrderedAt <= to.Value);
        }
        var orders = await query.ToListAsync(ct);
        if (db.Database.IsSqlite())
            orders = orders.Where(x => (!from.HasValue || x.OrderedAt >= from.Value) && (!to.HasValue || x.OrderedAt <= to.Value)).ToList();
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
        var returnByItem = returnRows.GroupBy(x => x.OrderItemId)
            .ToDictionary(x => x.Key, x => new ReturnProjection(x.Key, x.Sum(v => v.Quantity), x.Sum(v => v.ReturnedValue)));
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

    private sealed record ItemProjection(
        Guid Id, Guid OrderId, Guid ProductId, string Sku, string Name, string Currency,
        int Quantity, decimal UnitPrice, decimal UnitCostAtSale, decimal LineTotal);
    private sealed record ReturnProjection(Guid OrderItemId, int Quantity, decimal ReturnedValue);
}
