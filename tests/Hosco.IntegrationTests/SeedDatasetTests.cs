using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Hosco.IntegrationTests;

[Collection("api")]
public sealed class SeedDatasetTests(ApiFixture fixture)
{
    [Fact]
    public async Task Seed_exposes_six_month_deterministic_tenant_dataset()
    {
        var login = await fixture.Client.PostAsJsonAsync("/api/v1/auth/login", new { email = "owner@hosco.local", password = "HoscoDemo!2026" });
        login.EnsureSuccessStatusCode();
        using var loginJson = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = loginJson.RootElement.GetProperty("accessToken").GetString();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/reporting/orders?pageSize=1&sortBy=orderedAt&sortDirection=asc");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await fixture.Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1_042, json.RootElement.GetProperty("meta").GetProperty("totalCount").GetInt32());
        Assert.Equal(new DateTimeOffset(2026, 1, 1, 8, 0, 0, TimeSpan.Zero),
            json.RootElement.GetProperty("data")[0].GetProperty("orderedAt").GetDateTimeOffset());
    }
}
