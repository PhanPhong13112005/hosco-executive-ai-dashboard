using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Hosco.Api.Observability;
using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Hosco.Api.Chat;

public sealed class ReportingApiClient(HttpClient client, IHttpContextAccessor accessor, IServer server) : IReportingApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<ChatBranch>> GetBranchesAsync(CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(new ChatOperationRequest(ReportingOperation.Branches), cancellationToken);
        return result.Data.Deserialize<IReadOnlyList<ChatBranch>>(JsonOptions) ?? [];
    }

    public async Task<ReportingApiResult> ExecuteAsync(ChatOperationRequest request, CancellationToken cancellationToken)
    {
        var context = accessor.HttpContext ?? throw new InvalidOperationException("Chat reporting requires an active HTTP request.");
        var path = BuildPath(request);
        // Listener addresses are server-controlled; never send the JWT to a request-supplied Host.
        var address = server.Features.Get<IServerAddressesFeature>()?.Addresses
            .FirstOrDefault(x => x.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            ?? server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();
        if (address is null) throw new ReportingApiUnavailableException("Reporting listener is unavailable.");
        var origin = new UriBuilder(address.Replace("*", "127.0.0.1").Replace("+", "127.0.0.1"));
        origin.Host = "127.0.0.1";
        using var message = new HttpRequestMessage(HttpMethod.Get, new Uri(origin.Uri, path));
        if (AuthenticationHeaderValue.TryParse(context.Request.Headers.Authorization, out var authorization))
            message.Headers.Authorization = authorization;
        if (context.Items[CorrelationMiddleware.ItemKey]?.ToString() is { Length: > 0 } correlationId)
            message.Headers.TryAddWithoutValidation(CorrelationMiddleware.Header, correlationId);

        HttpResponseMessage response;
        try { response = await client.SendAsync(message, HttpCompletionOption.ResponseContentRead, cancellationToken); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ReportingApiUnavailableException("Reporting API timed out.");
        }
        catch (HttpRequestException exception)
        {
            throw new ReportingApiUnavailableException("Reporting API is unavailable.", exception);
        }
        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new ChatAuthenticationException();
            if (response.StatusCode == HttpStatusCode.Forbidden)
                throw new ForbiddenException("Bạn không có quyền xem dữ liệu này.");
            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new KeyNotFoundException("Không tìm thấy dữ liệu báo cáo được yêu cầu.");
            if (!response.IsSuccessStatusCode)
                throw new ReportingApiUnavailableException("Reporting API returned an unsuccessful response.");

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            JsonDocument document;
            try { document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken); }
            catch (JsonException exception) { throw new ReportingApiUnavailableException("Invalid reporting response.", exception); }
            using var parsed = document;
            var root = document.RootElement;
            if (request.Operation == ReportingOperation.AlertList)
                return new ReportingApiResult(root.Clone(), "alerts.list.v1");
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("data", out var data) ||
                !root.TryGetProperty("meta", out var meta) || meta.ValueKind != JsonValueKind.Object ||
                !meta.TryGetProperty("queryId", out var queryId) || queryId.ValueKind != JsonValueKind.String)
                throw new ReportingApiUnavailableException("Invalid reporting envelope.");
            return new ReportingApiResult(data.Clone(), queryId.GetString() ?? "unknown",
                meta.TryGetProperty("isStale", out var stale) && stale.ValueKind == JsonValueKind.True);
        }
    }

    private static string BuildPath(ChatOperationRequest request)
    {
        var path = request.Operation switch
        {
            ReportingOperation.DashboardSummary => "/api/v1/reporting/dashboard/summary",
            ReportingOperation.RevenueTrend => "/api/v1/reporting/revenue/trend",
            ReportingOperation.TopProducts => "/api/v1/reporting/products/top",
            ReportingOperation.BottomProducts => "/api/v1/reporting/products/bottom",
            ReportingOperation.DangerousInventory => "/api/v1/reporting/inventory/dangerous",
            ReportingOperation.AlertList => "/api/v1/alerts",
            ReportingOperation.Branches => "/api/v1/reporting/branches",
            _ => throw new ArgumentOutOfRangeException(nameof(request.Operation))
        };
        var query = new List<string>();
        Add(query, "from", request.From?.ToString("O"));
        Add(query, "to", request.To?.ToString("O"));
        Add(query, "branchId", request.BranchId?.ToString());
        Add(query, "pageSize", request.Limit?.ToString());
        Add(query, "severity", request.Severity);
        if (request.Operation == ReportingOperation.AlertList) Add(query, "status", "Open");
        return query.Count == 0 ? path : $"{path}?{string.Join('&', query)}";
    }

    private static void Add(List<string> query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) query.Add($"{name}={Uri.EscapeDataString(value)}");
    }
}
