using System.Security.Cryptography;
using System.Text;
using Hosco.Application.Abstractions;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;

namespace Hosco.Application.Services;

public sealed class NotificationDispatcher(
    IEnumerable<INotificationChannel> channels,
    INotificationDeliveryStore deliveries,
    TimeProvider timeProvider) : INotificationSender
{
    private readonly IReadOnlyDictionary<string, INotificationChannel> _channels = channels
        .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

    public Task SendAsync(Alert alert, CancellationToken cancellationToken) =>
        DispatchAsync(alert, NotificationPurpose.Initial, cancellationToken);

    public Task SendEscalationAsync(Alert alert, CancellationToken cancellationToken) =>
        DispatchAsync(alert, NotificationPurpose.Escalation, cancellationToken);

    public async Task RetryPendingAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        foreach (var delivery in await deliveries.GetRetryableAsync(now, 100, cancellationToken))
        {
            if (!_channels.TryGetValue(delivery.Channel, out var channel))
            {
                delivery.Status = NotificationDeliveryStatus.Failed;
                delivery.LastError = "Notification channel is no longer registered.";
                delivery.UpdatedAt = now;
                await deliveries.SaveChangesAsync(cancellationToken);
                continue;
            }

            if (delivery.AttemptCount >= channel.MaxAttempts)
            {
                delivery.Status = NotificationDeliveryStatus.Failed;
                delivery.UpdatedAt = now;
                await deliveries.SaveChangesAsync(cancellationToken);
                continue;
            }

            if (delivery.LastAttemptAt.HasValue &&
                now < delivery.LastAttemptAt.Value + channel.RetryDelay(delivery.AttemptCount))
                continue;

            var alert = await deliveries.GetAlertAsync(delivery.AlertId, cancellationToken);
            if (alert is null)
            {
                delivery.Status = NotificationDeliveryStatus.Failed;
                delivery.LastError = "Alert no longer exists.";
                delivery.UpdatedAt = now;
                await deliveries.SaveChangesAsync(cancellationToken);
                continue;
            }

            var target = channel.ResolveTarget(alert, delivery.Purpose);
            if (target is null || !string.Equals(target.RecipientKey, delivery.RecipientKey, StringComparison.Ordinal))
            {
                delivery.Status = NotificationDeliveryStatus.Skipped;
                delivery.LastError = "Recipient configuration no longer matches this delivery.";
                delivery.UpdatedAt = now;
                await deliveries.SaveChangesAsync(cancellationToken);
                continue;
            }

            await AttemptAsync(alert, delivery, channel, target, cancellationToken);
        }
    }

    private async Task DispatchAsync(Alert alert, NotificationPurpose purpose, CancellationToken cancellationToken)
    {
        foreach (var channel in _channels.Values)
        {
            var target = channel.ResolveTarget(alert, purpose);
            if (target is null) continue;
            var key = IdempotencyKey(alert.Id, purpose, channel.Name, target.RecipientKey);
            var delivery = await deliveries.GetOrCreateAsync(alert, channel.Name, purpose, target, key, cancellationToken);
            if (delivery.Status is NotificationDeliveryStatus.Sent or NotificationDeliveryStatus.Skipped ||
                delivery.AttemptCount >= channel.MaxAttempts)
                continue;
            await AttemptAsync(alert, delivery, channel, target, cancellationToken);
        }
    }

    private async Task AttemptAsync(Alert alert, NotificationDelivery delivery, INotificationChannel channel,
        NotificationTarget target, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        delivery.AttemptCount++;
        delivery.LastAttemptAt = now;
        delivery.UpdatedAt = now;
        try
        {
            var result = await channel.SendAsync(alert, delivery.Purpose, target, cancellationToken);
            delivery.Status = result.Sent
                ? NotificationDeliveryStatus.Sent
                : result.Skipped
                    ? NotificationDeliveryStatus.Skipped
                    : result.Retryable && delivery.AttemptCount < channel.MaxAttempts
                        ? NotificationDeliveryStatus.Pending
                        : NotificationDeliveryStatus.Failed;
            delivery.SentAt = result.Sent ? now : null;
            delivery.LastError = Truncate(result.Error);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            delivery.Status = delivery.AttemptCount < channel.MaxAttempts
                ? NotificationDeliveryStatus.Pending
                : NotificationDeliveryStatus.Failed;
            delivery.LastError = Truncate(exception.Message);
        }
        await deliveries.SaveChangesAsync(cancellationToken);
    }

    private static string IdempotencyKey(Guid alertId, NotificationPurpose purpose, string channel, string recipientKey)
    {
        var value = $"{alertId:N}|{purpose}|{channel.ToLowerInvariant()}|{recipientKey}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private static string? Truncate(string? value) => value is { Length: > 1000 } ? value[..1000] : value;
}
