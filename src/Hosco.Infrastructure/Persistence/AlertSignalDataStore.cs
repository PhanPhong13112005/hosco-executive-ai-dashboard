using System.Text.Json;
using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hosco.Infrastructure.Persistence;

public sealed class AlertSignalDataStore(HoscoDbContext db) : IAlertSignalDataStore
{
    public async Task<IReadOnlyList<AlertSignal>> GetCancellationRateSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct)
    {
        if (!rule.Threshold.HasValue) return [];
        var rows = (await WindowOrdersAsync(rule, now, ct)).Select(x => new { x.BranchId, x.Status }).ToList();
        return rows.GroupBy(x => x.BranchId).Select(group =>
        {
            var rate = group.Any() ? 100m * group.Count(x => x.Status == OrderStatus.Cancelled) / group.Count() : 0m;
            return Signal(group.Key, "branch", "Tỷ lệ hủy đơn bất thường",
                $"Tỷ lệ hủy đơn preview là {rate:0.##}% trong cửa sổ {rule.WindowMinutes} phút.", rate, rule.Threshold.Value,
                new { rule = rule.Code, orders = group.Count(), cancelled = group.Count(x => x.Status == OrderStatus.Cancelled), baStatus = "PENDING" });
        }).ToList();
    }

    public async Task<IReadOnlyList<AlertSignal>> GetRevenueDropSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct)
    {
        if (!rule.Threshold.HasValue) return [];
        var window = TimeSpan.FromMinutes(rule.WindowMinutes);
        var currentFrom = now - window;
        var baselineFrom = currentFrom - window;
        var query = TenantOrders(rule).Where(x => x.Status == OrderStatus.Completed);
        if (!db.Database.IsSqlite()) query = query.Where(x => x.OrderedAt >= baselineFrom && x.OrderedAt <= now);
        var orders = await query.ToListAsync(ct);
        if (db.Database.IsSqlite()) orders = orders.Where(x => x.OrderedAt >= baselineFrom && x.OrderedAt <= now).ToList();
        var rows = orders.Select(x => new { x.BranchId, x.OrderedAt, x.TotalAmount }).ToList();
        return rows.GroupBy(x => x.BranchId).Select(group =>
        {
            var current = group.Where(x => x.OrderedAt >= currentFrom).Sum(x => x.TotalAmount);
            var baseline = rule.Baseline ?? group.Where(x => x.OrderedAt < currentFrom).Sum(x => x.TotalAmount);
            var drop = baseline <= 0 ? 0 : Math.Max(0, 100m * (baseline - current) / baseline);
            return Signal(group.Key, "branch", "Doanh thu giờ cao điểm giảm",
                $"Doanh thu preview giảm {drop:0.##}% so với baseline cấu hình/cửa sổ trước.", drop, rule.Threshold.Value,
                new { rule = rule.Code, currentRevenue = current, baselineRevenue = baseline, baStatus = "PENDING" });
        }).ToList();
    }

    public async Task<IReadOnlyList<AlertSignal>> GetDangerousStockSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct)
    {
        var query = db.Inventories.AsNoTracking().Include(x => x.Product).Where(x => x.TenantId == rule.TenantId);
        if (rule.BranchId.HasValue) query = query.Where(x => x.BranchId == rule.BranchId.Value);
        var rows = await query.ToListAsync(ct);
        return rows.Select(x =>
        {
            var threshold = rule.Threshold ?? x.SafetyStock;
            return Signal(x.BranchId, x.ProductId.ToString("N"), $"Tồn kho nguy hiểm: {x.Product.Sku}",
                $"{x.Product.Name} còn {x.QuantityOnHand}, ngưỡng cấu hình/safety stock là {threshold:0.##}.",
                x.QuantityOnHand, threshold, new { rule = rule.Code, x.ProductId, x.Product.Sku, x.QuantityOnHand, x.SafetyStock, baStatus = "PENDING" });
        }).ToList();
    }

    public async Task<IReadOnlyList<AlertSignal>> GetEmployeeCancellationSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct)
    {
        if (!rule.Threshold.HasValue) return [];
        var rows = (await WindowOrdersAsync(rule, now, ct, includeEmployee: true))
            .Where(x => x.Status == OrderStatus.Cancelled || x.Status == OrderStatus.Returned)
            .Select(x => new { x.BranchId, x.EmployeeId, x.Employee.EmployeeCode, x.Employee.DisplayName, x.Status }).ToList();
        return rows.GroupBy(x => new { x.BranchId, x.EmployeeId, x.EmployeeCode, x.DisplayName }).Select(group =>
            Signal(group.Key.BranchId, group.Key.EmployeeId.ToString("N"), $"Giao dịch bất thường: {group.Key.EmployeeCode}",
                $"{group.Key.DisplayName} có {group.Count()} giao dịch hủy/hoàn trong cửa sổ preview.", group.Count(), rule.Threshold.Value,
                new { rule = rule.Code, group.Key.EmployeeId, group.Key.EmployeeCode, cancelled = group.Count(x => x.Status == OrderStatus.Cancelled), returned = group.Count(x => x.Status == OrderStatus.Returned), baStatus = "PENDING" })).ToList();
    }

    public async Task<IReadOnlyList<AlertSignal>> GetPriceDiscountSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct)
    {
        if (!rule.Threshold.HasValue) return [];
        var rows = (await WindowOrdersAsync(rule, now, ct)).Where(x => x.Subtotal > 0)
            .Select(x => new { x.Id, x.BranchId, x.OrderNumber, x.Subtotal, x.DiscountAmount }).ToList();
        return rows.Select(x =>
        {
            var rate = 100m * x.DiscountAmount / x.Subtotal;
            return Signal(x.BranchId, x.Id.ToString("N"), $"Giảm giá bất thường: {x.OrderNumber}",
                $"Tỷ lệ giảm giá preview của đơn là {rate:0.##}%.", rate, rule.Threshold.Value,
                new { rule = rule.Code, orderId = x.Id, x.OrderNumber, x.Subtotal, x.DiscountAmount, baStatus = "PENDING" });
        }).ToList();
    }

    private async Task<List<Order>> WindowOrdersAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct, bool includeEmployee = false)
    {
        var from = now.AddMinutes(-rule.WindowMinutes);
        var query = TenantOrders(rule);
        if (includeEmployee) query = query.Include(x => x.Employee);
        if (!db.Database.IsSqlite()) query = query.Where(x => x.OrderedAt >= from && x.OrderedAt <= now);
        var rows = await query.ToListAsync(ct);
        return db.Database.IsSqlite() ? rows.Where(x => x.OrderedAt >= from && x.OrderedAt <= now).ToList() : rows;
    }

    private IQueryable<Order> TenantOrders(AlertRule rule)
    {
        var query = db.Orders.AsNoTracking().Where(x => x.TenantId == rule.TenantId);
        if (rule.BranchId.HasValue) query = query.Where(x => x.BranchId == rule.BranchId.Value);
        return query;
    }

    private static AlertSignal Signal(Guid? branchId, string entityKey, string title, string message,
        decimal detected, decimal threshold, object context) =>
        new(branchId, entityKey, title, message, detected, threshold, JsonSerializer.Serialize(context));
}
