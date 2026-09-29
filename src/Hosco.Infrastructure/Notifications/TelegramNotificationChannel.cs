using System.Net;
using System.Net.Http.Json;
using Hosco.Application.Abstractions;
using Hosco.Application.Services;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hosco.Infrastructure.Notifications;

public sealed class TelegramNotificationOptions
{
    public const string Section = "Notifications:Telegram";
    public bool Enabled { get; set; }
    public string BotToken { get; set; } = string.Empty;
    public string DefaultChatId { get; set; } = string.Empty;
    public Guid? DefaultTenantId { get; set; }
    public Guid? DefaultBranchId { get; set; }
    public int TimeoutSeconds { get; set; } = 10;
    public int MaxRetries { get; set; } = 3;
    public int BaseRetryDelayMilliseconds { get; set; } = 500;
}

public sealed class TelegramNotificationChannel(
    HttpClient httpClient,
    IOptions<TelegramNotificationOptions> options,
    ILogger<TelegramNotificationChannel> logger) : INotificationChannel
{
    private readonly TelegramNotificationOptions _options = options.Value;
    public string Name => "telegram";
    public int MaxAttempts => 1 + Math.Clamp(_options.MaxRetries, 0, 5);

    public TimeSpan RetryDelay(int completedAttempts)
    {
        var exponent = Math.Clamp(completedAttempts - 1, 0, 5);
        return TimeSpan.FromMilliseconds(Math.Clamp(_options.BaseRetryDelayMilliseconds, 100, 60_000) * Math.Pow(2, exponent));
    }

    public NotificationTarget? ResolveTarget(Alert alert, NotificationPurpose purpose)
    {
        if (_options.DefaultTenantId.HasValue && _options.DefaultTenantId != alert.TenantId) return null;
        if (_options.DefaultBranchId.HasValue && _options.DefaultBranchId != alert.BranchId) return null;
        var roles = purpose == NotificationPurpose.Escalation
            ? AlertRecipientPolicy.EscalationRecipients(alert)
            : AlertRecipientPolicy.InitialRecipients(alert);
        if (roles.Count == 0) return null;
        var recipientKey = $"tenant:{alert.TenantId:N}:branch:{alert.BranchId?.ToString("N") ?? "all"}:roles:{string.Join('-', roles.Order())}";
        return new NotificationTarget(recipientKey, _options.DefaultChatId);
    }

    public async Task<NotificationAttemptResult> SendAsync(Alert alert, NotificationPurpose purpose,
        NotificationTarget target, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
            return new NotificationAttemptResult(false, false, true, "Telegram channel is disabled.");
        if (!_options.DefaultTenantId.HasValue)
            return new NotificationAttemptResult(false, false, true, "Telegram tenant scope is not configured.");
        if (string.IsNullOrWhiteSpace(_options.BotToken) || string.IsNullOrWhiteSpace(target.Address))
            return new NotificationAttemptResult(false, false, true, "Telegram credentials or destination are not configured.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 1, 30)));
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                $"https://api.telegram.org/bot{_options.BotToken}/sendMessage",
                new { chat_id = target.Address, text = BuildMessage(alert, purpose) }, timeout.Token);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Telegram notification sent alertId={AlertId} purpose={Purpose} tenantId={TenantId} branchId={BranchId}",
                    alert.Id, purpose, alert.TenantId, alert.BranchId);
                return new NotificationAttemptResult(true, false);
            }

            var retryable = response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
                            (int)response.StatusCode >= 500;
            logger.LogWarning("Telegram notification rejected alertId={AlertId} purpose={Purpose} statusCode={StatusCode} retryable={Retryable}",
                alert.Id, purpose, (int)response.StatusCode, retryable);
            return new NotificationAttemptResult(false, retryable, false,
                $"Telegram returned HTTP {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new NotificationAttemptResult(false, true, false, "Telegram request timed out.");
        }
        catch (HttpRequestException)
        {
            return new NotificationAttemptResult(false, true, false, "Telegram transport error.");
        }
    }

    private static string BuildMessage(Alert alert, NotificationPurpose purpose)
    {
        var prefix = purpose == NotificationPurpose.Escalation ? "[ESCALATION]" : "[HOSCO ALERT]";
        var text = $"{prefix} {alert.RuleCode} - {alert.Severity}\n{alert.Title}\n{alert.Message}\nAlert: {alert.Id}";
        return text.Length <= 4096 ? text : text[..4096];
    }
}
