using System.Text.Json;

namespace Hosco.IntegrationTests;

[Collection("api")]
public sealed class Gd6OpenApiTests(ApiFixture fixture)
{
    [Fact]
    public async Task Published_OpenApi_covers_current_demo_routes_and_Bearer_scheme()
    {
        using var response = await fixture.Client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = json.RootElement.GetProperty("paths");
        foreach (var route in new[] { "/api/v1/auth/login", "/api/v1/reporting/dashboard/summary",
            "/api/v1/reporting/dashboard/export", "/api/v1/reporting/products/ranking",
            "/api/v1/alerts", "/api/v1/alerts/{id}", "/api/v1/alert-rules", "/api/v1/alert-rules/{id}",
            "/api/v1/chat/messages" })
            Assert.True(paths.TryGetProperty(route, out _), $"Missing OpenAPI route: {route}");
        Assert.Equal("bearer", json.RootElement.GetProperty("components").GetProperty("securitySchemes")
            .GetProperty("Bearer").GetProperty("scheme").GetString());
    }
}
