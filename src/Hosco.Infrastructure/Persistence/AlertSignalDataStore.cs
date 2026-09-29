using System.Text.Json;
using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Application.Services;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hosco.Infrastructure.Persistence;

public sealed class AlertSignalDataStore(
    HoscoDbContext db,
    IKpiCalculator kpis,
    IKpiSnapshotStore snapshots,
    IBusinessTime businessTime) : IAlertSignalDataStore
{
    public async Task<IReadOnlyList<AlertSignal>> GetCancellationRateSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct)
    {
        var config = AlertRuleConfiguration.Cancellation(rule);
        var observedFrom = now.AddMinutes(-rule.WindowMinutes);
        var historyFrom = observedFrom.AddDays(-config.BaselineDays);
        var orders = await snapshots.LoadAsync(Scope(rule), historyFrom, now, ct);
        var result = new List<AlertSignal>();
        foreach (var branch in orders.GroupBy(x => x.BranchId))
        {
            var observed = branch.Where(x => x.OrderedAt >= observedFrom).ToList();
            if (observed.Count == 0) continue;
            var observedCancelled = observed.Count(x => x.Status == OrderStatus.Cancelled);
            var observedRate = 100m * observedCancelled / observed.Count;
            var dailyRates = branch.Where(x => x.OrderedAt < observedFrom)
                .GroupBy(x => businessTime.GetBusinessDate(x.OrderedAt))
                .Select(day => 100m * day.Count(x => x.Status == OrderStatus.Cancelled) / day.Count()).ToList();
            if (dailyRates.Count == 0) continue;
            var baseline = dailyRates.Average();
            var increase = baseline <= 0 ? (observedRate > 0 ? 100m : 0m) : 100m * (observedRate - baseline) / baseline;
            var severity = AlertRuleConfiguration.CancellationSeverity(increase, config);
            if (!severity.HasValue) continue;
            result.Add(Signal(branch.Key, "branch", severity.Value, "Tỷ lệ hủy đơn bất thường",
                $"Tỷ lệ hủy {observedRate:0.##}% tăng {increase:0.##}% so với baseline {baseline:0.##}%.",
                increase, config.HighIncreasePercent, baseline,
                new { rule = rule.Code, observedRate, baseline, increasePercent = increase, cancelledOrders = observedCancelled,
                    totalOrders = observed.Count, branchId = branch.Key, lastUpdatedAt = observed.Max(x => x.UpdatedAt), baStatus = "PROPOSED_CONFIGURABLE" }));
        }
        return result;
    }

    public async Task<IReadOnlyList<AlertSignal>> GetRevenueDropSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct)
    {
        var config = AlertRuleConfiguration.RevenueDrop(rule);
        var local = businessTime.ToBusinessTime(now);
        var peak = config.PeakWindows.FirstOrDefault(x => local.Hour >= x.StartHour && local.Hour < x.EndHour);
        if (peak is null) return [];
        var currentDate = businessTime.GetBusinessDate(now);
        var currentStart = businessTime.StartOfBusinessDayUtc(currentDate).AddHours(peak.StartHour);
        var currentEnd = DateTimeOffset.Compare(now, businessTime.StartOfBusinessDayUtc(currentDate).AddHours(peak.EndHour)) < 0
            ? now
            : businessTime.StartOfBusinessDayUtc(currentDate).AddHours(peak.EndHour);
        var elapsed = currentEnd - currentStart;
        var orders = await snapshots.LoadAsync(Scope(rule), currentStart.AddDays(-config.BaselineDays), currentEnd, ct);
        var result = new List<AlertSignal>();
        foreach (var branch in orders.GroupBy(x => x.BranchId))
        {
            var current = kpis.Calculate(branch.Where(x => x.OrderedAt >= currentStart && x.OrderedAt <= currentEnd)).Revenue;
            var baselineValues = Enumerable.Range(1, config.BaselineDays).Select(dayOffset =>
            {
                var from = currentStart.AddDays(-dayOffset);
                return kpis.Calculate(branch.Where(x => x.OrderedAt >= from && x.OrderedAt <= from + elapsed)).Revenue;
            }).ToList();
            var baseline = baselineValues.Average();
            var severity = AlertRuleConfiguration.RevenueDropSeverity(current, baseline, config);
            if (!severity.HasValue) continue;
            result.Add(Signal(branch.Key, $"peak-{peak.StartHour:00}-{peak.EndHour:00}", severity.Value,
                "Doanh thu giờ cao điểm giảm",
                $"Doanh thu khung {peak.StartHour:00}:00–{peak.EndHour:00}:00 là {current:0} VND, baseline {baseline:0} VND.",
                current, baseline * config.HighRevenuePercent / 100m, baseline,
                new { rule = rule.Code, observedRevenue = current, baseline, peakWindow = $"{peak.StartHour:00}:00-{peak.EndHour:00}:00",
                    branchId = branch.Key, lastUpdatedAt = branch.Max(x => x.UpdatedAt), timezone = "UTC+7", baStatus = "PROPOSED_CONFIGURABLE" }));
        }
        return result;
    }

    public async Task<IReadOnlyList<AlertSignal>> GetDangerousStockSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct)
    {
        var config = AlertRuleConfiguration.DangerousStock(rule);
        var query = db.Inventories.AsNoTracking().Include(x => x.Product)
            .Where(x => x.TenantId == rule.TenantId && x.Product.IsKeySku);
        if (rule.BranchId.HasValue) query = query.Where(x => x.BranchId == rule.BranchId.Value);
        var rows = await query.ToListAsync(ct);
        return rows.Select(x => new { Inventory = x, Available = x.QuantityOnHand - x.ReservedQuantity,
                Severity = AlertRuleConfiguration.DangerousStockSeverity(x.QuantityOnHand - x.ReservedQuantity, x.SafetyStock, config) })
            .Where(x => x.Severity.HasValue)
            .Select(x => Signal(x.Inventory.BranchId, x.Inventory.ProductId.ToString("N"), x.Severity!.Value,
                $"Tồn kho nguy hiểm: {x.Inventory.Product.Sku}",
                $"{x.Inventory.Product.Name}: available {x.Available}, safety stock {x.Inventory.SafetyStock}.",
                x.Available, x.Inventory.SafetyStock, null,
                new { rule = rule.Code, x.Inventory.ProductId, x.Inventory.Product.Sku, onHand = x.Inventory.QuantityOnHand,
                    reserved = x.Inventory.ReservedQuantity, available = x.Available, x.Inventory.SafetyStock,
                    x.Inventory.BranchId, lastUpdatedAt = x.Inventory.UpdatedAt, baStatus = "PROPOSED_CONFIGURABLE" })).ToList();
    }

    public async Task<IReadOnlyList<AlertSignal>> GetEmployeeCancellationSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct)
    {
        var config = AlertRuleConfiguration.Employee(rule);
        var observedFrom = now.AddMinutes(-rule.WindowMinutes);
        var historyFrom = observedFrom.AddDays(-config.BaselineDays);
        var orders = await snapshots.LoadAsync(Scope(rule), historyFrom, now, ct);
        var employeeIds = orders.Select(x => x.EmployeeId).Distinct().ToArray();
        var employeeNames = await db.Employees.AsNoTracking().Where(x => x.TenantId == rule.TenantId && employeeIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => new { x.EmployeeCode, x.DisplayName }, ct);
        var result = new List<AlertSignal>();
        foreach (var branch in orders.GroupBy(x => x.BranchId))
        {
            var historical = branch.Where(x => x.OrderedAt < observedFrom).ToList();
            var baseline = historical.Count == 0 ? 0m : 100m * historical.Count(IsCancellationOrReturn) / historical.Count;
            foreach (var employee in branch.Where(x => x.OrderedAt >= observedFrom).GroupBy(x => x.EmployeeId))
            {
                var affected = employee.Count(IsCancellationOrReturn);
                var rate = employee.Any() ? 100m * affected / employee.Count() : 0m;
                var severity = AlertRuleConfiguration.EmployeeSeverity(rate, baseline, employee.Count(), config);
                if (!severity.HasValue) continue;
                var identity = employeeNames.GetValueOrDefault(employee.Key);
                result.Add(Signal(branch.Key, employee.Key.ToString("N"), severity.Value,
                    $"Giao dịch bất thường: {identity?.EmployeeCode ?? "employee"}",
                    $"{identity?.DisplayName ?? "Nhân viên"} có tỷ lệ hủy/hoàn {rate:0.##}% trên {employee.Count()} đơn.",
                    rate, config.AbsoluteRatePercent, baseline,
                    new { rule = rule.Code, employeeId = employee.Key, identity?.EmployeeCode, identity?.DisplayName,
                        observedRate = rate, branchBaseline = baseline, affectedOrders = affected, totalOrders = employee.Count(),
                        branchId = branch.Key, lastUpdatedAt = employee.Max(x => x.UpdatedAt), recipients = new[] { "BranchManager", "ChainManagerWhenHigh" },
                        baStatus = "PROPOSED_CONFIGURABLE" }));
            }
        }
        return result;
    }

    public async Task<IReadOnlyList<AlertSignal>> GetPriceDiscountSignalsAsync(AlertRule rule, DateTimeOffset now, CancellationToken ct)
    {
        var config = AlertRuleConfiguration.Price(rule);
        var from = now.AddMinutes(-rule.WindowMinutes);
        var query = db.OrderItems.AsNoTracking().Include(x => x.Product).Include(x => x.Order).ThenInclude(x => x.Employee)
            .Where(x => x.TenantId == rule.TenantId);
        if (rule.BranchId.HasValue) query = query.Where(x => x.Order.BranchId == rule.BranchId.Value);
        if (!db.Database.IsSqlite()) query = query.Where(x => x.Order.OrderedAt >= from && x.Order.OrderedAt <= now);
        var rows = await query.ToListAsync(ct);
        if (db.Database.IsSqlite()) rows = rows.Where(x => x.Order.OrderedAt >= from && x.Order.OrderedAt <= now).ToList();
        return rows.Where(x => kpis.IsRecognizedSale(x.Order.Status)).Select(x =>
            {
                var gross = x.UnitPrice * x.Quantity;
                var discountRate = gross <= 0 ? 0m : 100m * x.DiscountAmount / gross;
                var belowFloor = x.Product.FloorPrice.HasValue && x.UnitPrice < x.Product.FloorPrice.Value;
                return new { Item = x, DiscountRate = discountRate, BelowFloor = belowFloor,
                    Severity = AlertRuleConfiguration.PriceSeverity(discountRate, belowFloor, config) };
            }).Where(x => x.Severity.HasValue)
            .Select(x => Signal(x.Item.Order.BranchId, x.Item.ProductId.ToString("N"), x.Severity!.Value,
                $"Giá bán/giảm giá bất thường: {x.Item.Product.Sku}",
                $"Đơn {x.Item.Order.OrderNumber}: discount {x.DiscountRate:0.##}%, unit price {x.Item.UnitPrice:0} VND.",
                x.DiscountRate, config.HighDiscountPercent, x.Item.Product.FloorPrice,
                new { rule = rule.Code, orderId = x.Item.OrderId, orderItemId = x.Item.Id, x.Item.Order.OrderNumber,
                    x.Item.ProductId, x.Item.Product.Sku, unitPrice = x.Item.UnitPrice, discountPercent = x.DiscountRate,
                    floorPrice = x.Item.Product.FloorPrice, x.BelowFloor, x.Item.Order.BranchId,
                    employeeId = x.Item.Order.EmployeeId, employeeName = x.Item.Order.Employee.DisplayName,
                    lastUpdatedAt = x.Item.Order.UpdatedAt, baStatus = "PROPOSED_CONFIGURABLE" })).ToList();
    }

    private static ReportingScope Scope(AlertRule rule) => new(rule.TenantId,
        rule.BranchId.HasValue ? new HashSet<Guid> { rule.BranchId.Value } : null);

    private static bool IsCancellationOrReturn(KpiOrderSnapshot order) =>
        order.Status is OrderStatus.Cancelled or OrderStatus.Returned or OrderStatus.PartiallyReturned || order.HasCompletedRefund;

    private static AlertSignal Signal(Guid? branchId, string entityKey, AlertSeverity severity, string title, string message,
        decimal detected, decimal threshold, decimal? baseline, object context) =>
        new(branchId, entityKey, severity, title, message, detected, threshold, baseline, JsonSerializer.Serialize(context));
}
