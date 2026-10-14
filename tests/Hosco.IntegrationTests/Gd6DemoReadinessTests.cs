using Hosco.Domain.Enums;
using Hosco.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Hosco.IntegrationTests;

public sealed class Gd6DemoReadinessTests
{
    [Fact]
    public async Task Repeated_demo_seed_keeps_relational_counts_ids_and_scope_without_duplicates()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new HoscoDbContext(new DbContextOptionsBuilder<HoscoDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        await DemoSeed.SeedAsync(db);
        var before = await Counts(db);
        var orders = await db.Orders.AsNoTracking().Select(x => x.Id).OrderBy(x => x).ToListAsync();
        var branches = await db.UserBranches.AsNoTracking().Select(x => new { x.UserId, x.BranchId }).ToListAsync();
        db.ChangeTracker.Clear();
        await DemoSeed.SeedAsync(db);
        Assert.Equal(before, await Counts(db));
        Assert.Equal(orders, await db.Orders.AsNoTracking().Select(x => x.Id).OrderBy(x => x).ToListAsync());
        Assert.Equal(branches.OrderBy(x => x.UserId).ThenBy(x => x.BranchId),
            (await db.UserBranches.AsNoTracking().Select(x => new { x.UserId, x.BranchId }).ToListAsync())
                .OrderBy(x => x.UserId).ThenBy(x => x.BranchId));
        Assert.Equal(2, await db.Tenants.CountAsync());
        Assert.Equal(4, await db.Branches.CountAsync());
        Assert.Equal(5, await db.Users.CountAsync());
        Assert.Equal(20, await db.AlertRules.CountAsync());
        Assert.Equal(Enum.GetValues<SystemRole>().Length, await db.Roles.CountAsync());
    }

    [Fact]
    public async Task Demo_reset_uses_new_database_with_reproducible_data_not_a_production_delete()
    {
        var snapshots = new List<int[]>();
        for (var iteration = 0; iteration < 2; iteration++)
        {
            await using var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            await using var db = new HoscoDbContext(new DbContextOptionsBuilder<HoscoDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            await DemoSeed.SeedAsync(db);
            snapshots.Add(await Counts(db));
        }
        Assert.Equal(snapshots[0], snapshots[1]);
        // Seed creates sales/rules/alert fixtures, not delivery attempts.
        // Notifications are generated later by the dispatcher/scheduler.
        Assert.True(snapshots[0].Take(15).All(count => count > 0));
        Assert.Equal(0, snapshots[0][15]);
    }

    private static async Task<int[]> Counts(HoscoDbContext db) =>
    [
        await db.Tenants.CountAsync(), await db.Branches.CountAsync(), await db.Users.CountAsync(),
        await db.UserBranches.CountAsync(), await db.Customers.CountAsync(), await db.Employees.CountAsync(),
        await db.Products.CountAsync(), await db.Inventories.CountAsync(), await db.Orders.CountAsync(),
        await db.OrderItems.CountAsync(), await db.Payments.CountAsync(), await db.Refunds.CountAsync(),
        await db.RefundItems.CountAsync(), await db.AlertRules.CountAsync(), await db.Alerts.CountAsync(),
        await db.NotificationDeliveries.CountAsync()
    ];
}
