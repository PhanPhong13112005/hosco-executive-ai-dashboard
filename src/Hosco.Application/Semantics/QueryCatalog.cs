namespace Hosco.Application.Semantics;

public sealed record QueryDefinition(
    string QueryId,
    string? MetricCode,
    IReadOnlyList<string> Parameters,
    string Output,
    IReadOnlyList<string> AllowedRoles,
    string Version,
    DefinitionStatus Status);

public interface IQueryCatalog { IReadOnlyList<QueryDefinition> All { get; } QueryDefinition Get(string queryId); }

public sealed class QueryCatalog : IQueryCatalog
{
    private static readonly string[] Roles = ["Owner", "BranchManager", "ChainManager", "SystemAdmin"];
    private static readonly string[] Range = ["from", "to", "branchId"];
    public IReadOnlyList<QueryDefinition> All { get; } =
    [
        Q("revenue.summary.v1", "revenue", Range, "RevenuePoint[]", DefinitionStatus.BlockedByBusinessDefinition),
        Q("revenue.trend.v1", "revenue", Range, "RevenuePoint[]", DefinitionStatus.BlockedByBusinessDefinition),
        Q("dashboard.summary.v1", null, Range, "DashboardSummary", DefinitionStatus.ProvisionalTechnicalPreview),
        Q("orders.trend.v1", "total-orders", Range, "OrderTrendPoint[]", DefinitionStatus.ProvisionalTechnicalPreview),
        Q("branches.list.v1", null, [], "BranchRow[]", DefinitionStatus.Implemented),
        Q("gmv.summary.v1", "gmv", Range, "KpiValue", DefinitionStatus.BlockedByBusinessDefinition),
        Q("orders.summary.v1", "total-orders", Range, "KpiValue", DefinitionStatus.BlockedByBusinessDefinition),
        Q("orders.list.v1", null, [.. Range, "page", "pageSize", "sortBy", "sortDirection"], "PagedResult<OrderRow>", DefinitionStatus.Implemented),
        Q("aov.summary.v1", "aov", Range, "KpiValue", DefinitionStatus.BlockedByBusinessDefinition),
        Q("products.ranking.v1", "sku-ranking", [.. Range, "bottom", "pageSize"], "ProductRankRow[]", DefinitionStatus.BlockedByBusinessDefinition),
        Q("inventory.dangerous.v1", "dangerous-stock", ["branchId"], "DangerousInventoryRow[]", DefinitionStatus.BlockedByBusinessDefinition),
        Q("gross-profit.summary.v1", "gross-profit", Range, "KpiValue", DefinitionStatus.BlockedByBusinessDefinition),
        Q("cancel-return-rate.summary.v1", "cancel-return-rate", Range, "KpiValue", DefinitionStatus.BlockedByBusinessDefinition),
        Q("kpis.summary.v1", null, Range, "KpiValue[]", DefinitionStatus.ProvisionalTechnicalPreview)
    ];

    public QueryDefinition Get(string queryId) => All.FirstOrDefault(x => x.QueryId.Equals(queryId, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Query '{queryId}' is not allow-listed.");

    private static QueryDefinition Q(string id, string? metric, IReadOnlyList<string> parameters, string output, DefinitionStatus status) =>
        new(id, metric, parameters, output, Roles, "1", status);
}
