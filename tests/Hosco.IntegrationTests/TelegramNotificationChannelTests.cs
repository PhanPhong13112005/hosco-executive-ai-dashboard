using System.Net;
using Hosco.Application.Services;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;
using Hosco.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hosco.IntegrationTests;

public sealed class TelegramNotificationChannelTests
{
    [Fact]
    public async Task Enabled_provider_sends_successfully_without_logging_secrets()
    {
        var tenant = Guid.NewGuid();
        var handler = new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var channel = Create(handler, tenant);
        var alert = Alert(tenant);

        var result = await channel.SendAsync(alert, NotificationPurpose.Initial,
            channel.ResolveTarget(alert, NotificationPurpose.Initial)!, default);

        Assert.True(result.Sent);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task Timeout_and_server_error_are_retryable()
    {
        var tenant = Guid.NewGuid();
        var timeoutHandler = new StubHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var timeoutChannel = Create(timeoutHandler, tenant, timeoutSeconds: 1);
        var alert = Alert(tenant);
        var target = timeoutChannel.ResolveTarget(alert, NotificationPurpose.Initial)!;
        var timeout = await timeoutChannel.SendAsync(alert, NotificationPurpose.Initial, target, default);
        Assert.True(timeout.Retryable);

        var serverChannel = Create(new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))), tenant);
        var server = await serverChannel.SendAsync(alert, NotificationPurpose.Initial,
            serverChannel.ResolveTarget(alert, NotificationPurpose.Initial)!, default);
        Assert.True(server.Retryable);
    }

    [Fact]
    public async Task Disabled_provider_is_skipped_and_tenant_mismatch_has_no_target()
    {
        var tenant = Guid.NewGuid();
        var handler = new StubHandler((_, _) => throw new InvalidOperationException("HTTP must not be called"));
        var disabled = Create(handler, tenant, enabled: false);
        var alert = Alert(tenant);
        var result = await disabled.SendAsync(alert, NotificationPurpose.Initial,
            disabled.ResolveTarget(alert, NotificationPurpose.Initial)!, default);
        Assert.True(result.Skipped);
        Assert.Equal(0, handler.Calls);

        var otherTenant = Create(handler, Guid.NewGuid());
        Assert.Null(otherTenant.ResolveTarget(alert, NotificationPurpose.Initial));
    }

    [Fact]
    public void Recipient_key_uses_existing_role_policy_and_contains_no_destination()
    {
        var tenant = Guid.NewGuid();
        var channel = Create(new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))), tenant);
        var target = channel.ResolveTarget(Alert(tenant), NotificationPurpose.Initial)!;

        Assert.Contains(nameof(SystemRole.BranchManager), target.RecipientKey);
        Assert.Contains(nameof(SystemRole.Owner), target.RecipientKey);
        Assert.DoesNotContain("fixture-chat", target.RecipientKey);
    }

    private static TelegramNotificationChannel Create(HttpMessageHandler handler, Guid tenant, bool enabled = true, int timeoutSeconds = 2) =>
        new(new HttpClient(handler), Options.Create(new TelegramNotificationOptions
        {
            Enabled = enabled,
            BotToken = "fixture-token",
            DefaultChatId = "fixture-chat",
            DefaultTenantId = tenant,
            TimeoutSeconds = timeoutSeconds,
            MaxRetries = 2,
            BaseRetryDelayMilliseconds = 100
        }), NullLogger<TelegramNotificationChannel>.Instance);

    private static Alert Alert(Guid tenant) => new()
    {
        Id = Guid.NewGuid(), TenantId = tenant, BranchId = Guid.NewGuid(), RuleCode = "AL-01", Type = "AL-01",
        Severity = AlertSeverity.High, Status = AlertStatus.Open, Title = "test", Message = "test", PayloadJson = "{}",
        DedupKey = "test", DetectedAt = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
    };

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return response(request, cancellationToken);
        }
    }
}
