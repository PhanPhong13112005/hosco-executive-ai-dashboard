using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Hosco.IntegrationTests;

[Collection("api")]
public sealed class ReportingApiTests(ApiFixture fixture)
{
    private static readonly Guid TenantB = TestId("tenant-b");
    private static readonly Guid BranchA1 = TestId("tenant-a-branch-1");
    private static readonly Guid BranchA2 = TestId("tenant-a-branch-2");
    private static readonly Guid BranchB1 = TestId("tenant-b-branch-1");
    [Fact]
    public async Task Reporting_without_login_returns_401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reporting/orders");
        var response = await fixture.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("unauthorized", json.RootElement.GetProperty("code").GetString());
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
    }

    [Fact]
    public async Task Branch_manager_can_read_assigned_branch_only()
    {
        var token = await Login("branch.manager@hosco.local");
        var response = await GetOrders(token, $"branchId={BranchA1}&pageSize=20");
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200, received {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var rows = json.RootElement.GetProperty("data").EnumerateArray().ToArray();
        Assert.NotEmpty(rows);
        Assert.All(rows, x => Assert.Equal(BranchA1, x.GetProperty("branchId").GetGuid()));
    }

    [Fact]
    public async Task Branch_manager_is_forbidden_from_other_assigned_tenant_branch()
    {
        var token = await Login("branch.manager@hosco.local");
        var response = await GetOrders(token, $"branchId={BranchA2}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("owner@hosco.local")]
    [InlineData("admin@hosco.local")]
    public async Task Owner_and_system_admin_cannot_read_unassigned_branch(string email)
    {
        var response = await GetOrders(await Login(email), $"branchId={BranchA2}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Cross_tenant_branch_is_forbidden()
    {
        var token = await Login("owner@hosco.local");
        var response = await GetOrders(token, $"branchId={BranchB1}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Chain_manager_can_read_branch_in_own_tenant()
    {
        var token = await Login("chain.manager@hosco.local");
        var response = await GetOrders(token, $"branchId={BranchA2}&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Reporting_never_leaks_other_tenant_data()
    {
        var token = await Login("owner@hosco.local");
        var response = await GetOrders(token, "pageSize=200");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.All(json.RootElement.GetProperty("data").EnumerateArray(), x => Assert.Equal(BranchA1, x.GetProperty("branchId").GetGuid()));
    }

    [Fact]
    public async Task Client_tenantId_query_parameter_cannot_override_claim()
    {
        var token = await Login("owner@hosco.local");
        var response = await GetOrders(token, $"tenantId={TenantB}&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.All(json.RootElement.GetProperty("data").EnumerateArray(), x => Assert.NotEqual(BranchB1, x.GetProperty("branchId").GetGuid()));
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task Health_endpoint_is_healthy(string path)
    {
        var response = await fixture.Client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_filter_returns_400()
    {
        var token = await Login("owner@hosco.local");
        var response = await GetOrders(token, "pageSize=10000");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_document_is_available_when_enabled()
    {
        var response = await fixture.Client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/v1/reporting/orders", content);
    }

    [Theory]
    [InlineData("/api/v1/reporting/kpis/summary?pageSize=5")]
    [InlineData("/api/v1/reporting/revenue?pageSize=5")]
    [InlineData("/api/v1/reporting/products/ranking?pageSize=5")]
    [InlineData("/api/v1/reporting/inventory/dangerous?pageSize=5")]
    public async Task Reporting_contract_endpoints_execute(string path)
    {
        var token = await Login("owner@hosco.local");
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await fixture.Client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200, received {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}\n{fixture.ProcessOutput}");
    }

    [Fact]
    public async Task Valid_correlation_id_is_reused()
    {
        var token = await Login("owner@hosco.local");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reporting/orders?pageSize=1");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Correlation-ID", "gd2-test-correlation");
        var response = await fixture.Client.SendAsync(request);
        Assert.Equal("gd2-test-correlation", response.Headers.GetValues("X-Correlation-ID").Single());
    }

    private async Task<string> Login(string email)
    {
        var response = await fixture.Client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "HoscoDemo!2026" });
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }

    private Task<HttpResponseMessage> GetOrders(string token, string query)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/reporting/orders?{query}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return fixture.Client.SendAsync(request);
    }

    private static Guid TestId(string value) => new(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))[..16]);
}
