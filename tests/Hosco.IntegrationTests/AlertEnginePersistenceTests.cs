using Hosco.Application.Abstractions;
using Hosco.Application.Services;
using Hosco.Domain.Entities;
using Hosco.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Hosco.IntegrationTests;

public sealed class AlertEnginePersistenceTests
{
    [Fact]
    public async Task Dangerous_stock_rule_persists_alert_and_deduplicates_second_cycle()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<HoscoDbContext>().UseSqlite(connection).Options;
        await using var db = new HoscoDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await DemoSeed.SeedAsync(db);
        await db.AlertRules.ExecuteUpdateAsync(update => update.SetProperty(x => x.IsEnabled, false));
        await db.AlertRules.Where(x => x.TenantId == DemoSeedIds.TenantA && x.Code == "AL-03")
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.IsEnabled, true));

        var repository = new AlertRepository(db);
        var store = new AlertSignalDataStore(db);
        var notification = new NotificationSpy();
        var engine = new AlertEngine(repository, [new DangerousStockAlertEvaluator(store)], notification,
            new Clock(new DateTimeOffset(2026, 6, 30, 18, 0, 0, TimeSpan.Zero)));

        var first = await engine.EvaluateAllAsync(default);
        var second = await engine.EvaluateAllAsync(default);

        Assert.Equal(2, first.CreatedAlerts);
        Assert.Equal(0, second.CreatedAlerts);
        Assert.Equal(2, second.SuppressedDuplicates);
        Assert.Equal(2, notification.Count);
        Assert.Equal(2, await db.Alerts.CountAsync(x => x.RuleCode == "AL-03" &&
            x.DetectedAt == new DateTimeOffset(2026, 6, 30, 18, 0, 0, TimeSpan.Zero)));
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class NotificationSpy : INotificationSender
    {
        public int Count { get; private set; }
        public Task SendAsync(Alert alert, CancellationToken ct) { Count++; return Task.CompletedTask; }
    }
}
