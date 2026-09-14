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
        M("KPI-01", "revenue", "Revenue", "Net recognized sales; exact status/discount/refund rules await BA KPI Dictionary.", "currency", "revenue.summary.v1"),
        M("KPI-02", "gmv", "GMV", "Gross merchandise value; included order statuses await BA approval.", "currency", "gmv.summary.v1"),
        M("KPI-03", "total-orders", "Total Orders", "Order count; included statuses await BA approval.", "count", "orders.summary.v1"),
        M("KPI-04", "aov", "AOV", "Average order value; numerator/denominator await BA approval.", "currency", "aov.summary.v1"),
        M("KPI-05", "gross-profit", "Gross Profit / Margin", "Uses UnitCostAtSale when approved; return allocation awaits BA.", "currency/percent", "gross-profit.summary.v1"),
        M("KPI-06", "cancel-return-rate", "Cancellation / Return Rate", "Count-versus-value denominator awaits BA approval.", "percent", "cancel-return-rate.summary.v1"),
        M("KPI-07", "sku-ranking", "Top / Bottom SKU", "Ranking basis (revenue or quantity) awaits BA approval.", "rank", "products.ranking.v1"),
        M("KPI-08", "dangerous-stock", "Dangerous Stock", "Threshold source/rule awaits BA approval.", "count", "inventory.dangerous.v1")
    ];

    public MetricDefinition Get(string code) => All.FirstOrDefault(x => x.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Unknown metric code '{code}'.");

    private static MetricDefinition M(string id, string code, string name, string description, string unit, string queryId) =>
        new(id, code, name, description, unit, Filters, Dimensions, "1.0", queryId,
            DefinitionStatus.BlockedByBusinessDefinition, "TODO(BA): confirm formula in the approved KPI Dictionary.");
}
