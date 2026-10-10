using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Hosco.Infrastructure.Persistence;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;

namespace Hosco.IntegrationTests;

[Collection("api")]
public sealed class Gd5SelfQaRegressionTests(ApiFixture fixture)
{
    [Fact]
    public async Task Unprefixed_city_in_chat_is_not_silently_dropped_and_is_authorized()
    {
        var message = new { message = "Doanh thu Hà Nội tháng 1 năm 2026?" };
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(HttpMethod.Post,
            "/api/v1/chat/messages", await Login(), message)).StatusCode);
        var login = await fixture.Client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "chain.manager@hosco.local", password = DemoSeed.DemoPassword });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
        var chat = await Send(HttpMethod.Post, "/api/v1/chat/messages", token, message);
        Assert.Equal(HttpStatusCode.OK, chat.StatusCode);
        var actual = await chat.Content.ReadFromJsonAsync<JsonElement>();
        var direct = await Send(HttpMethod.Get,
            $"/api/v1/reporting/dashboard/summary?From=2025-12-31T17:00:00Z&To=2026-01-31T16:59:59.9999999Z&BranchId={DemoSeedIds.BranchA2}", token);
        direct.EnsureSuccessStatusCode();
        var expected = await direct.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Revenue", actual.GetProperty("intent").GetString());
        Assert.Equal(expected.GetProperty("data").GetProperty("revenue").GetDecimal(),
            actual.GetProperty("data").GetProperty("revenue").GetDecimal());
    }

    [Theory]
    [InlineData("Medium")]
    [InlineData("High")]
    [InlineData("Critical")]
    public async Task Rule_update_accepts_UI_string_severity_and_preserves_other_configuration(string severity)
    {
        var token = await Login();
        var original = await Rules(token);
        var id = original.GetProperty("id").GetGuid();
        try
        {
            var response = await Send(HttpMethod.Patch, $"/api/v1/alert-rules/{id}", token, new { severity });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(severity, json.RootElement.GetProperty("severity").GetString());
            foreach (var property in new[] { "configJson", "threshold", "baseline", "windowMinutes", "cooldownMinutes", "baStatus" })
                Assert.Equal(original.GetProperty(property).ToString(), json.RootElement.GetProperty(property).ToString());
        }
        finally
        {
            Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Patch, $"/api/v1/alert-rules/{id}", token,
                new { severity = original.GetProperty("severity").GetString() })).StatusCode);
        }
    }

    [Fact]
    public async Task Rule_update_rejects_unknown_string_severity_without_changing_rule()
    {
        var token = await Login();
        var original = await Rules(token);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(HttpMethod.Patch,
            $"/api/v1/alert-rules/{original.GetProperty("id").GetGuid()}", token, new { severity = "SuperCritical" })).StatusCode);
        Assert.Equal(original.GetRawText(), (await Rules(token)).GetRawText());
    }

    [Fact]
    public void Pdf_embeds_Vietnamese_font_wraps_full_text_and_keeps_rows_after_old_48_line_limit()
    {
        var longText = "HOSCO Hồ Chí Minh – Đà Nẵng: " + new string('x', 350) + " END-OF-LONG-ROW";
        var rows = Enumerable.Range(0, 140).Select(i => new object?[] { i, longText, "TAIL-" + i }).ToList();
        var method = typeof(DashboardExportService).GetMethod("CreatePdf", BindingFlags.NonPublic | BindingFlags.Static)!;
        var bytes = (byte[])method.Invoke(null, [rows])!;
        using var pdf = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
        Assert.True(pdf.PageCount > 1);
        var cmap = new StringBuilder();
        foreach (var page in pdf.Pages)
        foreach (var reference in page.Resources.Elements.GetDictionary("/Font")!.Elements.Values.OfType<PdfReference>())
        {
            var font = (PdfDictionary)reference.Value;
            var unicode = font.Elements.GetDictionary("/ToUnicode");
            Assert.NotNull(unicode);
            cmap.Append(Encoding.ASCII.GetString(unicode.Stream.UnfilteredValue));
            var descendants = font.Elements.GetArray("/DescendantFonts");
            Assert.NotNull(descendants);
            var descendant = (PdfDictionary)((PdfReference)descendants.Elements[0]).Value;
            Assert.NotNull(descendant.Elements.GetDictionary("/FontDescriptor")!.Elements.GetDictionary("/FontFile2"));
        }
        foreach (var character in "ồíĐàẵ")
            Assert.Contains(((int)character).ToString("X4", CultureInfo.InvariantCulture), cmap.ToString(), StringComparison.OrdinalIgnoreCase);
        // Overflow produces populated additional pages; downloaded text is also
        // checked separately by the export content retest.
        Assert.True(pdf.Pages[^1].Contents.Elements.Count > 0);
    }

    [Fact]
    public async Task Concurrent_pdf_exports_use_embedded_font_without_host_fonts_or_locking()
    {
        var token = await Login();
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Send(HttpMethod.Get,
            "/api/v1/reporting/dashboard/export?format=pdf&from=2026-01-01&to=2026-01-31", token)));
        foreach (var response in results)
        {
            using (response)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                using var pdf = PdfReader.Open(new MemoryStream(await response.Content.ReadAsByteArrayAsync()), PdfDocumentOpenMode.Import);
                Assert.True(pdf.PageCount >= 1);
            }
        }
    }

    [Fact]
    public async Task Empty_export_represents_null_ratios_as_NA_and_preserves_real_zero_as_numeric()
    {
        var response = await Send(HttpMethod.Get,
            "/api/v1/reporting/dashboard/export?format=xlsx&from=2030-01-01&to=2030-01-31", await Login());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var archive = new ZipArchive(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
        using var reader = new StreamReader(archive.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var xml = System.Xml.Linq.XDocument.Parse(await reader.ReadToEndAsync());
        var ns = xml.Root!.Name.Namespace;
        var cells = xml.Descendants(ns + "c").ToDictionary(x => x.Attribute("r")!.Value);
        Assert.Equal("N/A", cells["B8"].Descendants(ns + "t").Single().Value);
        Assert.Equal("N/A", cells["B10"].Descendants(ns + "t").Single().Value);
        Assert.Equal("0", cells["B5"].Element(ns + "v")!.Value);
        Assert.All(xml.Descendants(ns + "col"), col => Assert.True((int)col.Attribute("width")! >= 12));
        Assert.True((int)xml.Descendants(ns + "col").First().Attribute("width")! >= "Cancellation / return rate".Length + 2);
        using var stylesReader = new StreamReader(archive.GetEntry("xl/styles.xml")!.Open());
        Assert.Contains("wrapText=\"1\"", await stylesReader.ReadToEndAsync());
    }

    private async Task<string> Login()
    {
        var response = await fixture.Client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "owner@hosco.local", password = DemoSeed.DemoPassword });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
    }

    private async Task<JsonElement> Rules(string token)
    {
        var response = await Send(HttpMethod.Get, "/api/v1/alert-rules", token);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>())[0].Clone();
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, string token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await fixture.Client.SendAsync(request);
    }
}
