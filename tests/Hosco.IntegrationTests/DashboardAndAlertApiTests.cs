using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Hosco.IntegrationTests;

[Collection("api")]
public sealed class DashboardAndAlertApiTests(ApiFixture fixture)
{
    private static readonly Guid TenantA = TestId("tenant-a");
    private static readonly Guid BranchA1 = TestId("tenant-a-branch-1");
    private static readonly Guid BranchA2 = TestId("tenant-a-branch-2");
    private static readonly Guid CancellationAlert = TestId($"alert-{TenantA}-cancellation-spike");
    private static readonly Guid DiscountAlert = TestId($"alert-{TenantA}-abnormal-discount");
    private static readonly Guid EmployeeAlert = TestId($"alert-{TenantA}-employee-cancellation");
    private static readonly Guid BranchA2Al01Rule = TestId($"alert-rule-{TenantA}-{BranchA2}-AL-01");

    [Fact]
    public async Task Dashboard_requires_authentication()
    {
        var response = await fixture.Client.GetAsync("/api/v1/reporting/dashboard/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_dashboard_supports_branch_filter_and_tenant_scope()
    {
        var token = await Login("branch.manager@hosco.local");
        var response = await Send(HttpMethod.Get,
            $"/api/v1/reporting/dashboard/summary?branchId={BranchA1}&from=2026-06-01&to=2026-06-30T23:59:59Z", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(BranchA1, json.RootElement.GetProperty("meta").GetProperty("branchId").GetGuid());
        Assert.Equal("ImplementedFinalGd1", json.RootElement.GetProperty("data").GetProperty("definitionStatus").GetString());
        Assert.True(json.RootElement.GetProperty("data").TryGetProperty("gmv", out _));
        Assert.True(json.RootElement.GetProperty("data").TryGetProperty("cancellationReturnRate", out _));

        var forbidden = await Send(HttpMethod.Get, $"/api/v1/reporting/dashboard/summary?branchId={BranchA2}", token);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Dashboard_rejects_invalid_date_filter()
    {
        var response = await Send(HttpMethod.Get,
            "/api/v1/reporting/dashboard/summary?from=2026-07-01&to=2026-06-01", await Login("owner@hosco.local"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_supporting_endpoints_execute()
    {
        var token = await Login("owner@hosco.local");
        foreach (var path in new[]
        {
            "/api/v1/reporting/revenue/trend?from=2026-06-01&to=2026-06-30T23:59:59Z",
            "/api/v1/reporting/orders/trend?from=2026-06-01&to=2026-06-30T23:59:59Z",
            "/api/v1/reporting/products/top?pageSize=5",
            "/api/v1/reporting/products/bottom?pageSize=5",
            "/api/v1/reporting/kpis/KPI-01/drilldown?from=2026-06-01&to=2026-06-30T23:59:59Z",
            "/api/v1/reporting/branches"
        })
            Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Get, path, token)).StatusCode);
    }

    [Fact]
    public async Task Alerts_require_authentication()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Client.GetAsync("/api/v1/alerts")).StatusCode);
    }

    [Fact]
    public async Task Alert_list_detail_acknowledge_and_resolve_workflow_executes()
    {
        var token = await Login("owner@hosco.local");
        var list = await Send(HttpMethod.Get, "/api/v1/alerts?status=Open", token);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using (var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync()))
            Assert.NotEmpty(json.RootElement.GetProperty("items").EnumerateArray());

        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Get, $"/api/v1/alerts/{CancellationAlert}", token)).StatusCode);
        var acknowledged = await Send(HttpMethod.Post, $"/api/v1/alerts/{CancellationAlert}/acknowledge", token);
        Assert.Equal(HttpStatusCode.OK, acknowledged.StatusCode);
        using (var json = JsonDocument.Parse(await acknowledged.Content.ReadAsStringAsync()))
            Assert.Equal("Acknowledged", json.RootElement.GetProperty("status").GetString());

        var resolved = await Send(HttpMethod.Post, $"/api/v1/alerts/{CancellationAlert}/resolve", token);
        Assert.Equal(HttpStatusCode.OK, resolved.StatusCode);
        using (var json = JsonDocument.Parse(await resolved.Content.ReadAsStringAsync()))
        {
            Assert.Equal("Resolved", json.RootElement.GetProperty("status").GetString());
            Assert.NotEqual(Guid.Empty, json.RootElement.GetProperty("resolvedBy").GetGuid());
        }
    }

    [Fact]
    public async Task Alert_detail_enforces_tenant_and_branch_isolation()
    {
        var otherTenant = await Send(HttpMethod.Get, $"/api/v1/alerts/{CancellationAlert}", await Login("owner@fixture.local"));
        Assert.Equal(HttpStatusCode.NotFound, otherTenant.StatusCode);

        var otherBranch = await Send(HttpMethod.Get, $"/api/v1/alerts/{DiscountAlert}", await Login("branch.manager@hosco.local"));
        Assert.Equal(HttpStatusCode.NotFound, otherBranch.StatusCode);
    }

    [Fact]
    public async Task Al04_workflow_is_branch_manager_only_and_requires_resolution_note()
    {
        var owner = await Login("owner@hosco.local");
        Assert.Equal(HttpStatusCode.Forbidden,
            (await Send(HttpMethod.Post, $"/api/v1/alerts/{EmployeeAlert}/acknowledge", owner)).StatusCode);

        var manager = await Login("branch.manager@hosco.local");
        Assert.Equal(HttpStatusCode.OK,
            (await Send(HttpMethod.Post, $"/api/v1/alerts/{EmployeeAlert}/acknowledge", manager)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await Send(HttpMethod.Post, $"/api/v1/alerts/{EmployeeAlert}/resolve", manager)).StatusCode);
        var resolved = await Send(HttpMethod.Post, $"/api/v1/alerts/{EmployeeAlert}/resolve", manager,
            new { note = "Đã xác minh và xử lý giao dịch bất thường." });
        Assert.Equal(HttpStatusCode.OK, resolved.StatusCode);
        using var json = JsonDocument.Parse(await resolved.Content.ReadAsStringAsync());
        Assert.Equal("Resolved", json.RootElement.GetProperty("status").GetString());
        Assert.Equal("Đã xác minh và xử lý giao dịch bất thường.", json.RootElement.GetProperty("resolutionNote").GetString());
    }

    [Fact]
    public async Task Alert_rule_list_and_role_protected_configuration_execute()
    {
        var ownerToken = await Login("owner@hosco.local");
        var list = await Send(HttpMethod.Get, "/api/v1/alert-rules", ownerToken);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
        Assert.Equal(5, json.RootElement.GetArrayLength());
        Assert.All(json.RootElement.EnumerateArray(), x => Assert.Equal("PENDING", x.GetProperty("baStatus").GetString()));
        var ruleId = json.RootElement[0].GetProperty("id").GetGuid();

        var forbidden = await Send(HttpMethod.Patch, $"/api/v1/alert-rules/{ruleId}", await Login("branch.manager@hosco.local"), new { isEnabled = true });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        var updated = await Send(HttpMethod.Patch, $"/api/v1/alert-rules/{ruleId}", ownerToken, new { isEnabled = true });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        var chainToken = await Login("chain.manager@hosco.local");
        var chainList = await Send(HttpMethod.Get, "/api/v1/alert-rules", chainToken);
        Assert.Equal(HttpStatusCode.OK, chainList.StatusCode);
        using (var chainJson = JsonDocument.Parse(await chainList.Content.ReadAsStringAsync()))
        {
            Assert.Equal(10, chainJson.RootElement.GetArrayLength());
            var chainRuleId = chainJson.RootElement[0].GetProperty("id").GetGuid();
            Assert.Equal(HttpStatusCode.OK,
                (await Send(HttpMethod.Patch, $"/api/v1/alert-rules/{chainRuleId}", chainToken, new { isEnabled = true })).StatusCode);
        }

        var adminToken = await Login("admin@hosco.local");
        var adminList = await Send(HttpMethod.Get, "/api/v1/alert-rules", adminToken);
        Assert.Equal(HttpStatusCode.OK, adminList.StatusCode);
        using (var adminJson = JsonDocument.Parse(await adminList.Content.ReadAsStringAsync()))
        {
            Assert.Equal(5, adminJson.RootElement.GetArrayLength());
            var adminRuleId = adminJson.RootElement[0].GetProperty("id").GetGuid();
            Assert.Equal(HttpStatusCode.OK,
                (await Send(HttpMethod.Patch, $"/api/v1/alert-rules/{adminRuleId}", adminToken, new { isEnabled = true })).StatusCode);
        }
        Assert.Equal(HttpStatusCode.NotFound,
            (await Send(HttpMethod.Patch, $"/api/v1/alert-rules/{BranchA2Al01Rule}", adminToken, new { isEnabled = true })).StatusCode);
    }

    private async Task<string> Login(string email)
    {
        var response = await fixture.Client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "HoscoDemo!2026" });
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, string token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await fixture.Client.SendAsync(request);
    }

    private static Guid TestId(string value) => new(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))[..16]);
}
