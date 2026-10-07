using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Hosco.IntegrationTests;

[Collection("api")]
public sealed class AlertRuleEncodingTests(ApiFixture fixture)
{
    // BUG-UI-001: validate decoded strings, not raw JSON (Unicode escapes are valid JSON).
    [Theory]
    [InlineData("owner@hosco.local", 5)]
    [InlineData("chain.manager@hosco.local", 10)]
    [InlineData("owner@fixture.local", 5)]
    public async Task Rule_names_descriptions_and_time_ranges_round_trip_as_utf8(string email, int count)
    {
        using var login = await fixture.Client.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password = "HoscoDemo!2026" });
        login.EnsureSuccessStatusCode();
        using var credentials = JsonDocument.Parse(await login.Content.ReadAsByteArrayAsync());
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/alert-rules");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentials.RootElement.GetProperty("accessToken").GetString());
        using var response = await fixture.Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType?.CharSet?.Trim('"'), ignoreCase: true);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var json = JsonDocument.Parse(new UTF8Encoding(false, true).GetString(bytes));
        var rules = json.RootElement.EnumerateArray().ToArray();
        Assert.Equal(count, rules.Length);
        Assert.Equal(new[] { "AL-01", "AL-02", "AL-03", "AL-04", "AL-05" },
            rules.Select(rule => rule.GetProperty("code").GetString()).Distinct().OrderBy(code => code));

        var expected = new Dictionary<string, (string Name, string Description)>
        {
            ["AL-01"] = ("Tỷ lệ hủy đơn bất thường", "So sánh tỷ lệ hủy 1 ngày với baseline 7 ngày cùng chi nhánh."),
            ["AL-02"] = ("Doanh thu giờ cao điểm giảm", "So sánh khung 11:00–13:00 và 17:00–20:00 UTC+7 với 7 ngày trước."),
            ["AL-03"] = ("SKU quan trọng tồn kho thấp", "IsKeySku với Available = OnHand - Reserved so với SafetyStock."),
            ["AL-04"] = ("Nhân viên có hủy/hoàn bất thường", "So sánh Rate_NV 1 ngày với baseline chi nhánh 7 ngày hoặc ngưỡng tuyệt đối."),
            ["AL-05"] = ("Giá bán hoặc giảm giá bất thường", "Kiểm tra discount và FloorPrice theo SKU.")
        };
        foreach (var rule in rules)
        {
            var copy = expected[rule.GetProperty("code").GetString()!];
            Assert.Equal(copy.Name, rule.GetProperty("name").GetString());
            Assert.Equal(copy.Description, rule.GetProperty("description").GetString());
        }
    }
}
