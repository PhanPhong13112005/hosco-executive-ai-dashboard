using Hosco.Application.Models;
using Hosco.Domain.Enums;
using Hosco.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Hosco.IntegrationTests;

public sealed class ProductRankingPersistenceTests
{
    [Fact]
    public async Task Sqlite_product_ranking_applies_direction_scope_date_completed_status_and_page_size()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<HoscoDbContext>().UseSqlite(connection).Options;
        await using var db = new HoscoDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await DemoSeed.SeedAsync(db);

        var items = await db.OrderItems.Include(x => x.Order).Include(x => x.Product).ToListAsync();
        items.First(x => x.TenantId == DemoSeedIds.TenantA && x.Order.BranchId == DemoSeedIds.BranchA1 &&
                         x.Order.Status == OrderStatus.Cancelled).LineTotal = 1_000_000_001m;
        items.First(x => x.TenantId == DemoSeedIds.TenantA && x.Order.BranchId == DemoSeedIds.BranchA2 &&
                         x.Order.Status == OrderStatus.Completed).LineTotal = 1_000_000_002m;
        items.First(x => x.TenantId == DemoSeedIds.TenantB && x.Order.Status == OrderStatus.Completed)
            .LineTotal = 1_000_000_003m;
        items.First(x => x.TenantId == DemoSeedIds.TenantA && x.Order.BranchId == DemoSeedIds.BranchA1 &&
                         x.Order.Status == OrderStatus.Completed && x.Order.OrderedAt.Month > 1).LineTotal = 1_000_000_004m;
        await db.SaveChangesAsync();

        var store = new ReportingDataStore(db);
        var scope = new ReportingScope(DemoSeedIds.TenantA, new HashSet<Guid> { DemoSeedIds.BranchA1 });
        var filter = new ReportingFilter(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 31, 23, 59, 59, TimeSpan.Zero),
            DemoSeedIds.BranchA1,
            PageSize: 3);

        var top = await store.GetProductRankingAsync(scope, filter, bottom: false, CancellationToken.None);
        var bottom = await store.GetProductRankingAsync(scope, filter, bottom: true, CancellationToken.None);

        Assert.Equal(3, top.Count);
        Assert.Equal(3, bottom.Count);
        Assert.Equal(top.OrderByDescending(x => x.Amount).ThenBy(x => x.Sku).ThenBy(x => x.ProductId), top);
        Assert.Equal(bottom.OrderBy(x => x.Amount).ThenBy(x => x.Sku).ThenBy(x => x.ProductId), bottom);
        Assert.All(top.Concat(bottom), row =>
        {
            Assert.StartsWith("HOSCO-A-", row.Sku);
            Assert.True(row.Amount < 1_000_000_000m);
        });

        var outsideDateRange = await store.GetProductRankingAsync(scope, filter with
        {
            From = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero),
            To = new DateTimeOffset(2027, 1, 31, 23, 59, 59, TimeSpan.Zero)
        }, bottom: false, CancellationToken.None);
        Assert.Empty(outsideDateRange);
    }
}
