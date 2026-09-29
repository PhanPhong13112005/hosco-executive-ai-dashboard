using Hosco.Application.Abstractions;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hosco.Infrastructure.Persistence;

public sealed class NotificationDeliveryStore(HoscoDbContext db, TimeProvider timeProvider) : INotificationDeliveryStore
{
    public async Task<NotificationDelivery> GetOrCreateAsync(Alert alert, string channel, NotificationPurpose purpose,
        NotificationTarget target, string idempotencyKey, CancellationToken cancellationToken)
    {
        var existing = await db.NotificationDeliveries
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null) return existing;

        var now = timeProvider.GetUtcNow();
        var delivery = new NotificationDelivery
        {
            Id = Guid.NewGuid(),
            TenantId = alert.TenantId,
            AlertId = alert.Id,
            RecipientKey = target.RecipientKey,
            Channel = channel,
            Purpose = purpose,
            Status = NotificationDeliveryStatus.Pending,
            IdempotencyKey = idempotencyKey,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.NotificationDeliveries.Add(delivery);
        await db.SaveChangesAsync(cancellationToken);
        return delivery;
    }

    public async Task<IReadOnlyList<NotificationDelivery>> GetRetryableAsync(DateTimeOffset now, int limit,
        CancellationToken cancellationToken)
    {
        var retryable = db.NotificationDeliveries
            .Where(x => x.Status == NotificationDeliveryStatus.Pending);

        // SQLite cannot translate ORDER BY for DateTimeOffset. Keep filtering in SQL,
        // then preserve the same ordering semantics on the client for the test/runtime provider.
        if (db.Database.IsSqlite())
        {
            var candidates = await retryable.ToListAsync(cancellationToken);
            return candidates
                .OrderBy(x => x.LastAttemptAt)
                .ThenBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Take(limit)
                .ToList();
        }

        return await retryable
            .OrderBy(x => x.LastAttemptAt)
            .ThenBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<Alert?> GetAlertAsync(Guid alertId, CancellationToken cancellationToken) =>
        db.Alerts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == alertId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
