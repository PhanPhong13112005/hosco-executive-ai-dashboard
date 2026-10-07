using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Hosco.Api.Chat;
using Hosco.Application.Abstractions;
using Hosco.Application.Models;
using Hosco.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hosco.IntegrationTests;

public sealed class LlmProviderTests
{
    internal const string Candidate = """
        {"intent":"Revenue","status":"Resolved","from":"2026-05-01","to":"2026-05-31","branch":null,"limit":null,"metric":"revenue","severity":null,"comparison":"None","confidence":0.95}
        """;

    [Theory]
    [InlineData(LlmProviderKind.OpenAI)] [InlineData(LlmProviderKind.Gemini)]
    public async Task Adapter_uses_fixed_endpoint_secret_header_strict_schema_and_intent_only_input(LlmProviderKind kind)
    {
        var handler = new Handler(async (request, ct) =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(kind == LlmProviderKind.OpenAI ? "api.openai.com" : "generativelanguage.googleapis.com", request.RequestUri!.Host);
            Assert.Empty(request.RequestUri.Query);
            Assert.DoesNotContain("fixture-key", request.RequestUri.ToString());
            if (kind == LlmProviderKind.OpenAI) Assert.Equal("fixture-key", request.Headers.Authorization!.Parameter);
            else { Assert.Null(request.Headers.Authorization); Assert.Equal("fixture-key", request.Headers.GetValues("x-goog-api-key").Single()); }
            var body = await request.Content!.ReadAsStringAsync(ct);
            Assert.DoesNotContain("forged-secret", body); Assert.DoesNotContain("fixture-key", body);
            Assert.DoesNotContain("tenantId", body); Assert.DoesNotContain("branchId", body);
            using var json = JsonDocument.Parse(body);
            if (kind == LlmProviderKind.OpenAI) Assert.Equal("fixture-model", json.RootElement.GetProperty("model").GetString());
            else Assert.EndsWith("/fixture-model:generateContent", request.RequestUri.AbsolutePath);
            // Gemini encodes its model in the URL rather than the body.
            var schema = kind == LlmProviderKind.OpenAI
                ? json.RootElement.GetProperty("text").GetProperty("format").GetProperty("schema")
                : json.RootElement.GetProperty("generationConfig").GetProperty("responseJsonSchema");
            Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
            Assert.Equal(10, schema.GetProperty("required").GetArrayLength());
            if (kind == LlmProviderKind.OpenAI) Assert.False(json.RootElement.GetProperty("store").GetBoolean());
            return Envelope(kind, Candidate);
        });
        var result = await Provider(kind, handler).TryResolveAsync("Tiền bán hàng trong tháng năm?", new("Revenue", "forged-secret"), default);
        Assert.Equal(ChatIntent.Revenue, result!.Intent); Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(LlmProviderKind.OpenAI, "malformed")]
    [InlineData(LlmProviderKind.Gemini, "malformed")]
    [InlineData(LlmProviderKind.OpenAI, "invalid-schema")]
    [InlineData(LlmProviderKind.Gemini, "invalid-schema")]
    [InlineData(LlmProviderKind.OpenAI, "refusal")]
    [InlineData(LlmProviderKind.Gemini, "refusal")]
    [InlineData(LlmProviderKind.OpenAI, "incomplete")]
    [InlineData(LlmProviderKind.Gemini, "incomplete")]
    [InlineData(LlmProviderKind.OpenAI, "oversized")]
    [InlineData(LlmProviderKind.Gemini, "oversized")]
    public async Task Invalid_blocked_truncated_or_oversized_output_falls_back(LlmProviderKind kind, string failure)
    {
        var handler = new Handler((_, _) => Task.FromResult(failure switch
        {
            "malformed" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not-json") },
            "invalid-schema" => Envelope(kind, "{}"),
            "oversized" => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(new string('x', 65537)) },
            "refusal" => Envelope(kind, Candidate, "blocked"), _ => Envelope(kind, Candidate, "incomplete")
        }));
        var resolver = Resolver(Provider(kind, handler));
        var result = await resolver.ResolveAsync("Doanh thu hôm nay?", null, default);
        Assert.Equal(ChatIntent.Revenue, result.Intent); Assert.Equal("hôm nay", result.DateRange!.Label);
        Assert.Equal(1, handler.Calls);
    }

    [Theory]
    [InlineData(LlmProviderKind.OpenAI, 500, 2)] [InlineData(LlmProviderKind.Gemini, 500, 2)]
    [InlineData(LlmProviderKind.OpenAI, 429, 2)] [InlineData(LlmProviderKind.Gemini, 429, 2)]
    [InlineData(LlmProviderKind.OpenAI, 401, 1)] [InlineData(LlmProviderKind.Gemini, 403, 1)]
    [InlineData(LlmProviderKind.OpenAI, 302, 1)] [InlineData(LlmProviderKind.Gemini, 400, 1)]
    public async Task Http_failure_is_bounded_and_does_not_surface_provider_body(LlmProviderKind kind, int status, int calls)
    {
        var handler = new Handler((_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
            { Content = new StringContent("fixture-key system prompt secret") }));
        var result = await Resolver(Provider(kind, handler)).ResolveAsync("Doanh thu hôm nay?", null, default);
        Assert.Equal(ChatResolutionStatus.Resolved, result.Status); Assert.Equal(calls, handler.Calls);
        Assert.DoesNotContain("secret", JsonSerializer.Serialize(result));
    }

    [Theory]
    [InlineData(LlmProviderKind.OpenAI)] [InlineData(LlmProviderKind.Gemini)]
    public async Task Retry_can_recover_from_transient_server_failure(LlmProviderKind kind)
    {
        var calls = 0;
        var handler = new Handler((_, _) => Task.FromResult(++calls == 1 ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) : Envelope(kind, Candidate)));
        Assert.NotNull(await Provider(kind, handler).TryResolveAsync("Doanh thu tháng năm?", null, default));
        Assert.Equal(2, handler.Calls);
    }

    [Theory]
    [InlineData(LlmProviderKind.OpenAI)] [InlineData(LlmProviderKind.Gemini)]
    public async Task Network_failure_retries_then_falls_back(LlmProviderKind kind)
    {
        var handler = new Handler((_, _) => throw new HttpRequestException("fixture-key private"));
        var result = await Resolver(Provider(kind, handler)).ResolveAsync("Doanh thu hôm nay?", null, default);
        Assert.Equal("hôm nay", result.DateRange!.Label); Assert.Equal(2, handler.Calls);
    }

    [Theory]
    [InlineData(LlmProviderKind.OpenAI)] [InlineData(LlmProviderKind.Gemini)]
    public async Task Total_timeout_falls_back_but_caller_cancellation_propagates(LlmProviderKind kind)
    {
        var handler = new Handler(async (_, ct) => { await Task.Delay(Timeout.Infinite, ct); return Envelope(kind, Candidate); });
        var provider = Provider(kind, handler, TimeSpan.FromMilliseconds(30));
        Assert.Equal(ChatIntent.Revenue, (await Resolver(provider).ResolveAsync("Doanh thu hôm nay?", null, default)).Intent);
        Assert.Equal(1, handler.Calls);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(10));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.TryResolveAsync("Doanh thu hôm nay?", null, cancellation.Token));
    }

    [Theory]
    [InlineData(LlmProviderKind.OpenAI)] [InlineData(LlmProviderKind.Gemini)]
    public async Task Missing_credentials_never_sends_http(LlmProviderKind kind)
    {
        var handler = new Handler((_, _) => throw new InvalidOperationException("Must not call network"));
        var client = new HttpClient(handler);
        var options = new LlmProviderOptions { Provider = kind, Model = "fixture-model" };
        ILlmProvider provider = kind == LlmProviderKind.OpenAI ? new OpenAiLlmProvider(client, options, Contract()) : new GeminiLlmProvider(client, options, Contract());
        Assert.Null(await provider.TryResolveAsync("Doanh thu hôm nay?", null, default)); Assert.Equal(0, handler.Calls);
    }

    [Theory]
    [InlineData(LlmProviderKind.OpenAI)] [InlineData(LlmProviderKind.Gemini)]
    public async Task Host_registration_selects_configured_adapter_and_scoped_wrapper(LlmProviderKind kind)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBusinessTime, VietnamBusinessTime>(); services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IChatSafetyGuard, ChatSafetyGuard>();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AI_PROVIDER"] = kind.ToString(), ["AI_MODEL"] = "fixture-model", ["OPENAI_API_KEY"] = "fixture-key", ["GEMINI_API_KEY"] = "fixture-key"
        }).Build();
        services.AddChatLlm(config);
        var handler = new Handler((_, _) => Task.FromResult(Envelope(kind, Candidate)));
        if (kind == LlmProviderKind.OpenAI) services.AddHttpClient<OpenAiLlmProvider>().ConfigurePrimaryHttpMessageHandler(() => handler);
        else services.AddHttpClient<GeminiLlmProvider>().ConfigurePrimaryHttpMessageHandler(() => handler);
        using var container = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = container.CreateScope();
        var selected = scope.ServiceProvider.GetRequiredService<ILlmProvider>();
        Assert.Equal(kind == LlmProviderKind.OpenAI ? typeof(OpenAiLlmProvider) : typeof(GeminiLlmProvider), selected.GetType());
        var resolver = scope.ServiceProvider.GetRequiredService<IIntentResolver>();
        Assert.IsType<LlmIntentResolver>(resolver);
        Assert.Equal("2026-05-01 → 2026-05-31", (await resolver.ResolveAsync("Tiền bán hàng tháng năm?", null, default)).DateRange!.Label);
        Assert.Equal(1, handler.Calls);
    }

    internal static LlmIntentContract Contract() => new(new VietnamBusinessTime(), TimeProvider.System);
    internal static ILlmProvider Provider(LlmProviderKind kind, Handler handler, TimeSpan? timeout = null)
    {
        var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        var options = new LlmProviderOptions { Provider = kind, Model = "fixture-model", ApiKey = "fixture-key", Timeout = timeout ?? TimeSpan.FromSeconds(5) };
        return kind == LlmProviderKind.OpenAI ? new OpenAiLlmProvider(client, options, Contract()) : new GeminiLlmProvider(client, options, Contract());
    }
    private static LlmIntentResolver Resolver(ILlmProvider provider) => new(provider,
        new ChatIntentResolver(new VietnamBusinessTime(), TimeProvider.System, new DisabledLlmProvider()), new ChatSafetyGuard());

    internal static HttpResponseMessage Envelope(LlmProviderKind kind, string candidate, string status = "completed") => new(HttpStatusCode.OK)
    {
        Content = kind == LlmProviderKind.OpenAI
            ? JsonContent.Create(new { status, output = new[] { new { type = "message", role = "assistant", status = "completed",
                content = new[] { new { type = status == "blocked" ? "refusal" : "output_text", text = candidate } } } } })
            : JsonContent.Create(new { candidates = new[] { new { finishReason = status == "completed" ? "STOP" : "SAFETY",
                content = new { parts = new[] { new { text = candidate } } } } } })
    };
    internal sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { Calls++; return respond(request, cancellationToken); }
    }
}
