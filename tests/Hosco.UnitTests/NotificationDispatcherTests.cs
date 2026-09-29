using Hosco.Application.Abstractions;
using Hosco.Application.Services;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;

namespace Hosco.UnitTests;

public sealed class NotificationDispatcherTests
{
    [Fact]
    public async Task Repeated_dispatch_uses_one_delivery_and_sends_once()
    {
        var clock = new Clock(DateTimeOffset.UtcNow);
        var store = new MemoryStore(clock);
        var channel = new Channel([new NotificationAttemptResult(true, false)]);
        var dispatcher = new NotificationDispatcher([channel], store, clock);
        var alert = NewAlert();

        await dispatcher.SendAsync(alert, default);
        await dispatcher.SendAsync(alert, default);

        Assert.Single(store.Deliveries);
        Assert.Equal(NotificationDeliveryStatus.Sent, store.Deliveries[0].Status);
        Assert.Equal(1, channel.SendCount);
    }

    [Fact]
    public async Task Transient_failure_is_retried_after_backoff()
    {
        var clock = new Clock(DateTimeOffset.UtcNow);
        var store = new MemoryStore(clock);
        var channel = new Channel([
            new NotificationAttemptResult(false, true, false, "temporary"),
            new NotificationAttemptResult(true, false)]);
        var dispatcher = new NotificationDispatcher([channel], store, clock);

        await dispatcher.SendAsync(NewAlert(), default);
        Assert.Equal(NotificationDeliveryStatus.Pending, store.Deliveries[0].Status);
        await dispatcher.RetryPendingAsync(default);
        Assert.Equal(1, channel.SendCount);
        clock.Advance(TimeSpan.FromSeconds(2));
        await dispatcher.RetryPendingAsync(default);

        Assert.Equal(NotificationDeliveryStatus.Sent, store.Deliveries[0].Status);
        Assert.Equal(2, store.Deliveries[0].AttemptCount);
    }

    [Fact]
    public async Task Non_retryable_failure_is_terminal()
    {
        var clock = new Clock(DateTimeOffset.UtcNow);
        var store = new MemoryStore(clock);
        var channel = new Channel([new NotificationAttemptResult(false, false, false, "bad request")]);
        var dispatcher = new NotificationDispatcher([channel], store, clock);

        await dispatcher.SendAsync(NewAlert(), default);
        clock.Advance(TimeSpan.FromMinutes(5));
        await dispatcher.RetryPendingAsync(default);

        Assert.Equal(NotificationDeliveryStatus.Failed, store.Deliveries[0].Status);
        Assert.Equal(1, channel.SendCount);
    }

    [Fact]
    public async Task Retryable_failure_stops_at_max_attempts()
    {
        var clock = new Clock(DateTimeOffset.UtcNow);
        var store = new MemoryStore(clock);
        var failures = Enumerable.Range(0, 3)
            .Select(_ => new NotificationAttemptResult(false, true, false, "temporary"));
        var channel = new Channel(failures);
        var dispatcher = new NotificationDispatcher([channel], store, clock);

        await dispatcher.SendAsync(NewAlert(), default);
        for (var i = 0; i < 3; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(2));
            await dispatcher.RetryPendingAsync(default);
        }

        Assert.Equal(NotificationDeliveryStatus.Failed, store.Deliveries[0].Status);
        Assert.Equal(3, store.Deliveries[0].AttemptCount);
        Assert.Equal(3, channel.SendCount);
    }

    private static Alert NewAlert() => new()
    {
        Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), BranchId = Guid.NewGuid(), RuleCode = "AL-01", Type = "AL-01",
        Severity = AlertSeverity.High, Status = AlertStatus.Open, Title = "Revenue alert", Message = "Revenue dropped",
        PayloadJson = "{}", DedupKey = "test", DetectedAt = DateTimeOffset.UtcNow,
        CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
    };

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan value) => _now += value;
    }

    private sealed class Channel(Queue<NotificationAttemptResult> results) : INotificationChannel
    {
        public Channel(IEnumerable<NotificationAttemptResult> results) : this(new Queue<NotificationAttemptResult>(results)) { }
        public string Name => "test";
        public int MaxAttempts => 3;
        public int SendCount { get; private set; }
        public TimeSpan RetryDelay(int completedAttempts) => TimeSpan.FromSeconds(1);
        public NotificationTarget ResolveTarget(Alert alert, NotificationPurpose purpose) => new("recipient", "address");
        public Task<NotificationAttemptResult> SendAsync(Alert alert, NotificationPurpose purpose, NotificationTarget target, CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.FromResult(results.Dequeue());
        }
    }

    private sealed class MemoryStore(Clock clock) : INotificationDeliveryStore
    {
        public List<NotificationDelivery> Deliveries { get; } = [];
        private readonly Dictionary<Guid, Alert> _alerts = [];

        public Task<NotificationDelivery> GetOrCreateAsync(Alert alert, string channel, NotificationPurpose purpose,
            NotificationTarget target, string idempotencyKey, CancellationToken cancellationToken)
        {
            _alerts[alert.Id] = alert;
            var delivery = Deliveries.SingleOrDefault(x => x.IdempotencyKey == idempotencyKey);
            if (delivery is null)
            {
                delivery = new NotificationDelivery
                {
                    Id = Guid.NewGuid(), AlertId = alert.Id, TenantId = alert.TenantId, Channel = channel,
                    RecipientKey = target.RecipientKey, Purpose = purpose, Status = NotificationDeliveryStatus.Pending,
                    IdempotencyKey = idempotencyKey, CreatedAt = clock.GetUtcNow(), UpdatedAt = clock.GetUtcNow()
                };
                Deliveries.Add(delivery);
            }
            return Task.FromResult(delivery);
        }

        public Task<IReadOnlyList<NotificationDelivery>> GetRetryableAsync(DateTimeOffset now, int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NotificationDelivery>>(Deliveries.Where(x => x.Status == NotificationDeliveryStatus.Pending).Take(limit).ToList());
        public Task<Alert?> GetAlertAsync(Guid alertId, CancellationToken cancellationToken) =>
            Task.FromResult(_alerts.GetValueOrDefault(alertId));
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
