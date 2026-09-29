namespace Hosco.Application.Semantics;

public enum DefinitionStatus { Implemented, ProvisionalTechnicalPreview, BlockedByBusinessDefinition }

public sealed record MetricDefinition(
    string MetricId,
    string Code,
    string Name,
    string Description,
    string Unit,
    IReadOnlyList<string> SupportedFilters,
    IReadOnlyList<string> SupportedDimensions,
    string Version,
    string QueryId,
    DefinitionStatus Status,
    string? Blocker);

public interface IMetricCatalog { IReadOnlyList<MetricDefinition> All { get; } MetricDefinition Get(string code); }

public sealed class MetricCatalog : IMetricCatalog
{
    private static readonly string[] Filters = ["from", "to", "branchId"];
    private static readonly string[] Dimensions = ["date", "branch"];
    public IReadOnlyList<MetricDefinition> All { get; } =
    [
        M("KPI-01", "revenue", "Revenue", "Recognized net sales less discount and returned-goods value.", "currency", "revenue.summary.v1"),
        M("KPI-02", "gmv", "GMV", "Unit selling price multiplied by valid sold quantity before discount.", "currency", "gmv.summary.v1"),
        M("KPI-03", "total-orders", "Total Orders", "Distinct recognized completed/delivered sale orders.", "count", "orders.summary.v1"),
        M("KPI-04", "aov", "AOV", "Revenue divided by Total Orders; null when no valid order exists.", "currency", "aov.summary.v1"),
        M("KPI-05", "gross-profit", "Gross Profit / Margin", "Revenue less COGS for valid sold quantity after returns.", "currency/percent", "gross-profit.summary.v1"),
        M("KPI-06", "cancel-return-rate", "Cancellation / Return Rate", "Distinct cancelled or refunded orders divided by all orders created.", "percent", "cancel-return-rate.summary.v1"),
        M("KPI-07", "sku-ranking", "Top / Bottom SKU", "Ranked by valid quantity sold after returned quantity.", "rank", "products.ranking.v1"),
        M("KPI-08", "dangerous-stock", "Dangerous Stock", "Available stock (OnHand - Reserved) at or below SafetyStock.", "count", "inventory.dangerous.v1")
    ];

    public MetricDefinition Get(string code) => All.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Unknown metric code '{code}'.");

    private static MetricDefinition M(string id, string code, string name, string description, string unit, string queryId) =>
        new(id, code, name, description, unit, Filters, Dimensions, "2.0", queryId,
            DefinitionStatus.Implemented, null);
}
