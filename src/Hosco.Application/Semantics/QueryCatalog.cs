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
        Q("revenue.summary.v1", "revenue", Range, "RevenuePoint[]", DefinitionStatus.Implemented),
        Q("revenue.trend.v1", "revenue", Range, "RevenuePoint[]", DefinitionStatus.Implemented),
        Q("dashboard.summary.v1", null, Range, "DashboardSummary", DefinitionStatus.Implemented),
        Q("orders.trend.v1", "total-orders", Range, "OrderTrendPoint[]", DefinitionStatus.Implemented),
        Q("branches.list.v1", null, [], "BranchRow[]", DefinitionStatus.Implemented, "1.0"),
        Q("gmv.summary.v1", "gmv", Range, "KpiValue", DefinitionStatus.Implemented),
        Q("orders.summary.v1", "total-orders", Range, "KpiValue", DefinitionStatus.Implemented),
        Q("orders.list.v1", null, [.. Range, "page", "pageSize", "sortBy", "sortDirection"], "PagedResult<OrderRow>", DefinitionStatus.Implemented, "1.0"),
        Q("aov.summary.v1", "aov", Range, "KpiValue", DefinitionStatus.Implemented),
        Q("products.ranking.v1", "sku-ranking", [.. Range, "bottom", "pageSize"], "ProductRankRow[]", DefinitionStatus.Implemented),
        Q("inventory.dangerous.v1", "dangerous-stock", ["branchId"], "DangerousInventoryRow[]", DefinitionStatus.Implemented),
        Q("gross-profit.summary.v1", "gross-profit", Range, "KpiValue", DefinitionStatus.Implemented),
        Q("cancel-return-rate.summary.v1", "cancel-return-rate", Range, "KpiValue", DefinitionStatus.Implemented),
        Q("kpis.summary.v1", null, Range, "KpiValue[]", DefinitionStatus.Implemented)
    ];

    public QueryDefinition Get(string queryId) => All.FirstOrDefault(x => x.QueryId.Equals(queryId, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Query '{queryId}' is not allow-listed.");

    private static QueryDefinition Q(string id, string? metric, IReadOnlyList<string> parameters, string output,
        DefinitionStatus status, string version = "2.0") =>
        new(id, metric, parameters, output, Roles, version, status);
}
