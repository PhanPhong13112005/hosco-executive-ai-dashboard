using Hosco.Api.Observability;
using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Application.Semantics;
using Hosco.Application.Services;
using Hosco.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hosco.Api.Controllers;

[ApiController]
[Authorize(Policy = "ReportingReader")]
[Route("api/v1/reporting")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public sealed class ReportingController(
    IReportingDataStore data,
    IAlertRepository alerts,
    IReportingScopeFactory scopes,
    IMetricCatalog metrics,
    IAuditWriter audit,
    ICorrelationContext correlation) : ControllerBase
{
    [HttpGet("dashboard/summary")]
    [ProducesResponseType<ApiEnvelope<DashboardSummary>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiEnvelope<DashboardSummary>>> Dashboard([FromQuery] ReportingFilter filter, CancellationToken ct)
    {
        filter.Validate(); var scope = await scopes.CreateAsync(filter.BranchId, ct);
        var result = await data.GetDashboardSummaryAsync(scope, filter, await alerts.GetSummaryAsync(scope, ct), ct);
        return Ok(new ApiEnvelope<DashboardSummary>(result, await Meta(scope, filter, "dashboard.summary.v1", null, ct)));
    }

    [HttpGet("kpis/summary")]
    [ProducesResponseType<ApiEnvelope<IReadOnlyList<KpiValue>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<KpiValue>>>> Summary([FromQuery] ReportingFilter filter, CancellationToken ct)
    {
        filter.Validate(); var scope = await scopes.CreateAsync(filter.BranchId, ct);
        var values = await data.GetKpiSummaryAsync(scope, filter, ct);
        var result = metrics.All.Select(m => new KpiValue(m.Code, m.Name, values.GetValueOrDefault(m.Code), m.Unit,
            m.Status.ToString(), m.Blocker)).ToList();
        return Ok(new ApiEnvelope<IReadOnlyList<KpiValue>>(result, await Meta(scope, filter, "kpis.summary.v1", null, ct)));
    }

    [HttpGet("revenue")]
    [ProducesResponseType<ApiEnvelope<IReadOnlyList<RevenuePoint>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<RevenuePoint>>>> Revenue([FromQuery] ReportingFilter filter, CancellationToken ct)
    {
        filter.Validate(); var scope = await scopes.CreateAsync(filter.BranchId, ct);
        var result = await data.GetRevenueTrendAsync(scope, filter, ct);
        return Ok(new ApiEnvelope<IReadOnlyList<RevenuePoint>>(result, await Meta(scope, filter, "revenue.trend.v1", null, ct)));
    }

    [HttpGet("revenue/trend")]
    [ProducesResponseType<ApiEnvelope<IReadOnlyList<RevenuePoint>>>(StatusCodes.Status200OK)]
    public Task<ActionResult<ApiEnvelope<IReadOnlyList<RevenuePoint>>>> RevenueTrend([FromQuery] ReportingFilter filter, CancellationToken ct) => Revenue(filter, ct);

    [HttpGet("orders/trend")]
    [ProducesResponseType<ApiEnvelope<IReadOnlyList<OrderTrendPoint>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<OrderTrendPoint>>>> OrderTrend([FromQuery] ReportingFilter filter, CancellationToken ct)
    {
        filter.Validate(); var scope = await scopes.CreateAsync(filter.BranchId, ct);
        var result = await data.GetOrderTrendAsync(scope, filter, ct);
        return Ok(new ApiEnvelope<IReadOnlyList<OrderTrendPoint>>(result, await Meta(scope, filter, "orders.trend.v1", null, ct)));
    }

    [HttpGet("orders")]
    [ProducesResponseType<ApiEnvelope<IReadOnlyList<OrderRow>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<OrderRow>>>> Orders([FromQuery] ReportingFilter filter, CancellationToken ct)
    {
        filter.Validate(); var scope = await scopes.CreateAsync(filter.BranchId, ct);
        var result = await data.GetOrdersAsync(scope, filter, ct);
        await audit.WriteAsync("reporting.query", "Order", null, "orders.list.v1", filter.BranchId, new { filter.From, filter.To, filter.Page, filter.PageSize }, ct);
        return Ok(new ApiEnvelope<IReadOnlyList<OrderRow>>(result.Items, await Meta(scope, filter, "orders.list.v1", result.TotalCount, ct)));
    }

    [HttpGet("products/ranking")]
    [ProducesResponseType<ApiEnvelope<IReadOnlyList<ProductRankRow>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<ProductRankRow>>>> Products([FromQuery] ReportingFilter filter, [FromQuery] bool bottom = false, CancellationToken ct = default)
    {
        filter.Validate(); var scope = await scopes.CreateAsync(filter.BranchId, ct);
        var result = await data.GetProductRankingAsync(scope, filter, bottom, ct);
        return Ok(new ApiEnvelope<IReadOnlyList<ProductRankRow>>(result, await Meta(scope, filter, "products.ranking.v1", null, ct)));
    }

    [HttpGet("products/top")]
    [ProducesResponseType<ApiEnvelope<IReadOnlyList<ProductRankRow>>>(StatusCodes.Status200OK)]
    public Task<ActionResult<ApiEnvelope<IReadOnlyList<ProductRankRow>>>> TopProducts([FromQuery] ReportingFilter filter, CancellationToken ct) => Products(filter, false, ct);

    [HttpGet("products/bottom")]
    [ProducesResponseType<ApiEnvelope<IReadOnlyList<ProductRankRow>>>(StatusCodes.Status200OK)]
    public Task<ActionResult<ApiEnvelope<IReadOnlyList<ProductRankRow>>>> BottomProducts([FromQuery] ReportingFilter filter, CancellationToken ct) => Products(filter, true, ct);

    [HttpGet("inventory/dangerous")]
    [ProducesResponseType<ApiEnvelope<IReadOnlyList<DangerousInventoryRow>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<DangerousInventoryRow>>>> Inventory([FromQuery] ReportingFilter filter, CancellationToken ct)
    {
        filter.Validate(); var scope = await scopes.CreateAsync(filter.BranchId, ct);
        var result = await data.GetDangerousInventoryAsync(scope, filter, ct);
        return Ok(new ApiEnvelope<IReadOnlyList<DangerousInventoryRow>>(result, await Meta(scope, filter, "inventory.dangerous.v1", null, ct)));
    }

    [HttpGet("branches")]
    [ProducesResponseType<ApiEnvelope<IReadOnlyList<BranchRow>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<BranchRow>>>> Branches(CancellationToken ct)
    {
        var filter = new ReportingFilter();
        var scope = await scopes.CreateAsync(null, ct);
        var result = await data.GetBranchesAsync(scope, ct);
        return Ok(new ApiEnvelope<IReadOnlyList<BranchRow>>(result, await Meta(scope, filter, "branches.list.v1", null, ct)));
    }

    [HttpGet("kpis/{metricId}/drilldown")]
    [ProducesResponseType<ApiEnvelope<KpiDrilldown>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiEnvelope<KpiDrilldown>>> Drilldown(string metricId, [FromQuery] ReportingFilter filter, CancellationToken ct)
    {
        filter.Validate();
        var metric = metrics.All.FirstOrDefault(x => x.MetricId.Equals(metricId, StringComparison.OrdinalIgnoreCase) ||
                                                     x.Code.Equals(metricId, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Unknown metric '{metricId}'.");
        var scope = await scopes.CreateAsync(filter.BranchId, ct);
        var trend = await data.GetKpiDrilldownAsync(metric.Code, scope, filter, ct);
        var preview = await data.GetKpiSummaryAsync(scope, filter, ct);
        var result = new KpiDrilldown(metric.MetricId, metric.Code, metric.Name, metric.Unit,
            preview.GetValueOrDefault(metric.Code), trend, metric.Status.ToString(), metric.Blocker);
        return Ok(new ApiEnvelope<KpiDrilldown>(result, await Meta(scope, filter, $"{metric.Code}.drilldown.v1", null, ct)));
    }

    private async Task<ReportMeta> Meta(ReportingScope scope, ReportingFilter filter, string queryId, int? total, CancellationToken ct)
    {
        HttpContext.Items["Hosco.QueryId"] = queryId;
        var updated = await data.GetLastUpdatedAtAsync(scope, ct);
        return new ReportMeta(filter.From, filter.To, filter.BranchId, updated, queryId, correlation.CorrelationId,
            total.HasValue ? filter.Page : null, total.HasValue ? filter.PageSize : null, total, DateTimeOffset.UtcNow - updated > TimeSpan.FromMinutes(30));
    }
}
