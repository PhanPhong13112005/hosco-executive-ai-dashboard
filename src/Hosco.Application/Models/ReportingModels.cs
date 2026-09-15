namespace Hosco.Application.Models;

public sealed record ReportingFilter(
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    Guid? BranchId = null,
    int Page = 1,
    int PageSize = 50,
    string? SortBy = null,
    string SortDirection = "desc")
{
    public const int MaxPageSize = 200;

    public ReportingFilter Validate()
    {
        if (Page < 1) throw new Abstractions.ValidationException("page must be at least 1.");
        if (PageSize is < 1 or > MaxPageSize)
            throw new Abstractions.ValidationException($"pageSize must be between 1 and {MaxPageSize}.");
        if (From.HasValue && To.HasValue && From > To)
            throw new Abstractions.ValidationException("from must be earlier than or equal to to.");
        if (!string.Equals(SortDirection, "asc", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
            throw new Abstractions.ValidationException("sortDirection must be 'asc' or 'desc'.");
        return this;
    }
}

public sealed record ReportingScope(Guid TenantId, IReadOnlySet<Guid>? RestrictedBranchIds)
{
    public bool Allows(Guid branchId) => RestrictedBranchIds is null || RestrictedBranchIds.Contains(branchId);
}

public sealed record ReportMeta(
    DateTimeOffset? From,
    DateTimeOffset? To,
    Guid? BranchId,
    DateTimeOffset LastUpdatedAt,
    string QueryId,
    string CorrelationId,
    int? Page = null,
    int? PageSize = null,
    int? TotalCount = null,
    bool IsStale = false);

public sealed record ApiEnvelope<T>(T Data, ReportMeta Meta);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount);

public sealed record RevenuePoint(DateOnly Date, decimal Amount, string Currency);
public sealed record OrderTrendPoint(DateOnly Date, int TotalOrders, int CancelledOrders, int ReturnedOrders);
public sealed record OrderRow(Guid Id, string OrderNumber, Guid BranchId, string Status, DateTimeOffset OrderedAt, decimal TotalAmount, string Currency);
public sealed record ProductRankRow(Guid ProductId, string Sku, string Name, int Quantity, decimal Amount, string Currency);
public sealed record DangerousInventoryRow(Guid BranchId, Guid ProductId, string Sku, string ProductName, int QuantityOnHand, int SafetyStock);
public sealed record KpiValue(string Code, string Name, decimal? Value, string Unit, string DefinitionStatus, string? Note);
public sealed record BranchRow(Guid Id, string Code, string Name);
public sealed record DashboardSummary(
    decimal Revenue, int TotalOrders, decimal Aov, decimal GrossProfit, decimal GrossMarginPercent,
    decimal CancellationRate, decimal ReturnRate, int DangerousStockCount, string Currency,
    int OpenAlerts, int UrgentAlerts, string DefinitionStatus, string Note);
public sealed record KpiDrilldownPoint(DateOnly Date, decimal Value);
public sealed record KpiDrilldown(
    string MetricId, string Code, string Name, string Unit, decimal? CurrentValue,
    IReadOnlyList<KpiDrilldownPoint> Trend, string DefinitionStatus, string? Note);

public interface IReportingDataStore
{
    Task<IReadOnlyList<RevenuePoint>> GetRevenueTrendAsync(ReportingScope scope, ReportingFilter filter, CancellationToken cancellationToken);
    Task<PagedResult<OrderRow>> GetOrdersAsync(ReportingScope scope, ReportingFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductRankRow>> GetProductRankingAsync(ReportingScope scope, ReportingFilter filter, bool bottom, CancellationToken cancellationToken);
    Task<IReadOnlyList<DangerousInventoryRow>> GetDangerousInventoryAsync(ReportingScope scope, ReportingFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<OrderTrendPoint>> GetOrderTrendAsync(ReportingScope scope, ReportingFilter filter, CancellationToken cancellationToken);
    Task<DashboardSummary> GetDashboardSummaryAsync(ReportingScope scope, ReportingFilter filter, AlertSummary alertSummary, CancellationToken cancellationToken);
    Task<IReadOnlyList<KpiDrilldownPoint>> GetKpiDrilldownAsync(string metricCode, ReportingScope scope, ReportingFilter filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<BranchRow>> GetBranchesAsync(ReportingScope scope, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, decimal>> GetTechnicalPreviewSummaryAsync(ReportingScope scope, ReportingFilter filter, CancellationToken cancellationToken);
    Task<DateTimeOffset> GetLastUpdatedAtAsync(ReportingScope scope, CancellationToken cancellationToken);
}
