using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Application.Semantics;

namespace Hosco.UnitTests;

public sealed class CatalogAndFilterTests
{
    [Fact]
    public void Metric_catalog_contains_all_eight_kpis_and_marks_unapproved_definitions()
    {
        var catalog = new MetricCatalog();
        Assert.Equal(8, catalog.All.Count);
        Assert.All(catalog.All, x => Assert.Equal(DefinitionStatus.BlockedByBusinessDefinition, x.Status));
        Assert.Equal("KPI-01", catalog.Get("revenue").MetricId);
    }

    [Fact]
    public void Query_catalog_rejects_non_allowlisted_query()
    {
        var catalog = new QueryCatalog();
        Assert.Equal("orders.list.v1", catalog.Get("orders.list.v1").QueryId);
        Assert.Throws<KeyNotFoundException>(() => catalog.Get("raw-sql.user-input"));
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(1, 0)]
    [InlineData(1, 201)]
    public void Pagination_rejects_out_of_range_values(int page, int pageSize) =>
        Assert.Throws<ValidationException>(() => new ReportingFilter(Page: page, PageSize: pageSize).Validate());

    [Fact]
    public void Filter_rejects_inverted_date_range() =>
        Assert.Throws<ValidationException>(() => new ReportingFilter(
            From: DateTimeOffset.Parse("2026-06-01"), To: DateTimeOffset.Parse("2026-05-01")).Validate());

    [Fact]
    public void Filter_accepts_maximum_page_size() =>
        Assert.Equal(ReportingFilter.MaxPageSize, new ReportingFilter(PageSize: ReportingFilter.MaxPageSize).Validate().PageSize);
}
