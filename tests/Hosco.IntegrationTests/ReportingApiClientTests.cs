using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Hosco.Api.Chat;
using Hosco.Api.Observability;
using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Hosco.IntegrationTests;

public sealed class ReportingApiClientTests
{
    [Fact]
    public async Task Adapter_uses_server_listener_and_forwards_only_auth_and_correlation()
    {
        Uri? uri = null;
        var handler = new Handler((message, _) =>
        {
            uri = message.RequestUri;
            Assert.Equal("fixture-token", message.Headers.Authorization?.Parameter);
            Assert.Equal("fixture-correlation", message.Headers.GetValues(CorrelationMiddleware.Header).Single());
            Assert.False(message.Headers.Contains("X-Untrusted"));
            return Task.FromResult(Json(HttpStatusCode.OK, """{"data":{"revenue":123},"meta":{"queryId":"dashboard.summary.v1","isStale":false}}"""));
        });
        using var http = new HttpClient(handler);
        var result = await Adapter(http).ExecuteAsync(new ChatOperationRequest(ReportingOperation.DashboardSummary), default);
        Assert.Equal("127.0.0.1", uri!.Host);
        Assert.Equal(57000, uri.Port);
        Assert.Equal("/api/v1/reporting/dashboard/summary", uri.AbsolutePath);
        Assert.Equal(123, result.Data.GetProperty("revenue").GetInt32());
    }

    [Theory]
    [InlineData(401, typeof(ChatAuthenticationException))]
    [InlineData(403, typeof(ForbiddenException))]
    [InlineData(404, typeof(KeyNotFoundException))]
    [InlineData(500, typeof(ReportingApiUnavailableException))]
    [InlineData(503, typeof(ReportingApiUnavailableException))]
    [InlineData(302, typeof(ReportingApiUnavailableException))]
    public async Task Adapter_maps_http_failures_without_leaking_response(int status, Type exceptionType)
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json((HttpStatusCode)status, "secret SQL exception"))));
        var exception = await Record.ExceptionAsync(() => Adapter(http).ExecuteAsync(new ChatOperationRequest(ReportingOperation.DashboardSummary), default));
        Assert.IsType(exceptionType, exception);
        Assert.DoesNotContain("secret", exception!.Message);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{}")]
    [InlineData("[]")]
    public async Task Invalid_envelope_returns_unavailable(string payload)
    {
        using var http = new HttpClient(new Handler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, payload))));
        await Assert.ThrowsAsync<ReportingApiUnavailableException>(() => Adapter(http).ExecuteAsync(new ChatOperationRequest(ReportingOperation.TopProducts), default));
    }

    [Fact]
    public async Task Network_failure_returns_unavailable()
    {
        using var http = new HttpClient(new Handler((_, _) => throw new HttpRequestException("fixture network")));
        await Assert.ThrowsAsync<ReportingApiUnavailableException>(() => Adapter(http).ExecuteAsync(new ChatOperationRequest(ReportingOperation.DashboardSummary), default));
    }

    [Fact]
    public async Task Timeout_returns_unavailable_but_caller_cancellation_propagates()
    {
        using var http = new HttpClient(new Handler(async (_, token) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return Json(HttpStatusCode.OK, "{}");
        })) { Timeout = TimeSpan.FromMilliseconds(50) };
        await Assert.ThrowsAsync<ReportingApiUnavailableException>(() => Adapter(http).ExecuteAsync(new ChatOperationRequest(ReportingOperation.DashboardSummary), default));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Adapter(http).ExecuteAsync(new ChatOperationRequest(ReportingOperation.DashboardSummary), cancellation.Token));
    }

    [Fact]
    public async Task Unknown_operation_never_sends_http_request()
    {
        var calls = 0;
        using var http = new HttpClient(new Handler((_, _) => { calls++; return Task.FromResult(Json(HttpStatusCode.OK, "{}")); }));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Adapter(http).ExecuteAsync(new ChatOperationRequest((ReportingOperation)999), default));
        Assert.Equal(0, calls);
    }

    private static ReportingApiClient Adapter(HttpClient http)
    {
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("attacker.invalid", 9999);
        context.Request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "fixture-token").ToString();
        context.Request.Headers["X-Untrusted"] = "do-not-forward";
        context.Items[CorrelationMiddleware.ItemKey] = "fixture-correlation";
        return new ReportingApiClient(http, new HttpContextAccessor { HttpContext = context }, new Server());
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string payload) => new(status)
    { Content = new StringContent(payload, Encoding.UTF8, "application/json") };

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }

    private sealed class Server : IServer
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();
        public Server()
        {
            var addresses = new ServerAddressesFeature();
            addresses.Addresses.Add("http://127.0.0.1:57000");
            Features.Set<IServerAddressesFeature>(addresses);
        }
        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public void Dispose() { }
    }
}
