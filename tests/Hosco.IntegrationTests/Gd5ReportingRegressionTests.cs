using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Hosco.Application.Models;
using Hosco.Infrastructure.Persistence;

namespace Hosco.IntegrationTests;

[Collection("api")]
public sealed class Gd5ReportingRegressionTests(ApiFixture fixture)
{
    private static readonly string January = "From=2025-12-31T17:00:00Z&To=2026-01-31T16:59:59.9999999Z";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ranking_pages_cover_stable_scoped_ranking_with_metadata_and_empty_overflow(bool bottom)
    {
        var token = await Login("owner@hosco.local");
        var path = $"/api/v1/reporting/products/ranking?{January}&BranchId={DemoSeedIds.BranchA1}&bottom={bottom}";
        var all = await Read<ApiEnvelope<List<ProductRankRow>>>($"{path}&Page=1&PageSize=200", token);
        Assert.NotEmpty(all.Data);
        Assert.All(all.Data, row => Assert.StartsWith("HOSCO-A-", row.Sku));
        var expected = bottom ? all.Data.OrderBy(x => x.Quantity).ThenBy(x => x.Sku).ThenBy(x => x.ProductId)
            : all.Data.OrderByDescending(x => x.Quantity).ThenBy(x => x.Sku).ThenBy(x => x.ProductId);
        Assert.Equal(expected, all.Data);
        Assert.Contains(all.Data.GroupBy(x => x.Quantity), group => group.Count() > 1); // real seed ties
        var pages = new List<ProductRankRow>();
        var lastPage = (all.Data.Count + 2) / 3;
        for (var page = 1; page <= lastPage + 1; page++)
        {
            var result = await Read<ApiEnvelope<List<ProductRankRow>>>($"{path}&Page={page}&PageSize=3", token);
            Assert.Equal(page, result.Meta.Page);
            Assert.Equal(3, result.Meta.PageSize);
            Assert.Equal(all.Data.Count, result.Meta.TotalCount);
            Assert.Equal(DemoSeedIds.BranchA1, result.Meta.BranchId);
            Assert.Equal(all.Data.Skip((page - 1) * 3).Take(3), result.Data);
            pages.AddRange(result.Data);
        }
        Assert.Equal(all.Data, pages);
        Assert.Equal(pages.Count, pages.Select(x => x.ProductId).Distinct().Count());
        var repeated = await Read<ApiEnvelope<List<ProductRankRow>>>($"{path}&Page=2&PageSize=3", token);
        Assert.Equal(all.Data.Skip(3).Take(3), repeated.Data);
        var overflow = await Read<ApiEnvelope<List<ProductRankRow>>>($"{path}&Page={int.MaxValue}&PageSize=200", token);
        Assert.Empty(overflow.Data);
        Assert.Equal(all.Data.Count, overflow.Meta.TotalCount);
    }

    [Theory]
    [InlineData("top")]
    [InlineData("bottom")]
    public async Task Ranking_aliases_paginate_and_reject_out_of_scope_branches(string alias)
    {
        var token = await Login("owner@hosco.local");
        var first = await Read<ApiEnvelope<List<ProductRankRow>>>($"/api/v1/reporting/products/{alias}?{January}&Page=1&PageSize=3", token);
        var second = await Read<ApiEnvelope<List<ProductRankRow>>>($"/api/v1/reporting/products/{alias}?{January}&Page=2&PageSize=3", token);
        Assert.Empty(first.Data.Select(x => x.ProductId).Intersect(second.Data.Select(x => x.ProductId)));
        foreach (var branch in new[] { DemoSeedIds.BranchA2, DemoSeedIds.BranchB1 })
        {
            using var response = await Get($"/api/v1/reporting/products/{alias}?{January}&BranchId={branch}&Page=2&PageSize=3", token);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        var fixtureToken = await Login("owner@fixture.local");
        var fixtureRows = await Read<ApiEnvelope<List<ProductRankRow>>>($"/api/v1/reporting/products/{alias}?{January}&PageSize=200", fixtureToken);
        Assert.NotEmpty(fixtureRows.Data);
        Assert.All(fixtureRows.Data, row => Assert.StartsWith("HOSCO-B-", row.Sku));
    }

    [Theory]
    [InlineData("owner@hosco.local")]
    [InlineData("owner@fixture.local")]
    public async Task Empty_dashboard_preserves_null_denominators_and_valid_zero_values(string email)
    {
        var result = await Read<ApiEnvelope<DashboardSummary>>("/api/v1/reporting/dashboard/summary?From=2029-12-31T17:00:00Z&To=2030-01-31T16:59:59.9999999Z", await Login(email));
        Assert.Equal(0, result.Data.TotalOrders);
        Assert.Equal(0, result.Data.Revenue);
        Assert.Equal(0, result.Data.GrossProfit);
        Assert.Null(result.Data.Aov);
        Assert.Null(result.Data.GrossMarginPercent);
    }

    private async Task<string> Login(string email)
    {
        using var response = await fixture.Client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "HoscoDemo!2026" });
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }
    private async Task<HttpResponseMessage> Get(string path, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await fixture.Client.SendAsync(request);
    }
    private async Task<T> Read<T>(string path, string token)
    {
        using var response = await Get(path, token);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
