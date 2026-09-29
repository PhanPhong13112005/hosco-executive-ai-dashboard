using Hosco.Application.Models;
using Hosco.Application.Services;
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
        var employeeId = await db.Employees.Where(x => x.TenantId == DemoSeedIds.TenantA && x.BranchId == DemoSeedIds.BranchA1)
            .Select(x => x.Id).FirstAsync();
        var zeroProduct = new Hosco.Domain.Entities.Product
        {
            Id = Guid.NewGuid(), TenantId = DemoSeedIds.TenantA, Sku = "ZERO-ONLY", Name = "Fully returned only",
            CurrentPrice = 1_000_000m, CurrentCost = 1m, Currency = "VND", CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        var returnedOrder = new Hosco.Domain.Entities.Order
        {
            Id = Guid.NewGuid(), TenantId = DemoSeedIds.TenantA, BranchId = DemoSeedIds.BranchA1, EmployeeId = employeeId,
            OrderNumber = $"ZERO-{Guid.NewGuid():N}", Status = OrderStatus.Returned,
            OrderedAt = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
            Subtotal = 100_000_000m, DiscountAmount = 0m, TotalAmount = 100_000_000m, Currency = "VND",
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        };
        db.Products.Add(zeroProduct);
        db.Orders.Add(returnedOrder);
        db.OrderItems.Add(new Hosco.Domain.Entities.OrderItem
        {
            Id = Guid.NewGuid(), TenantId = DemoSeedIds.TenantA, OrderId = returnedOrder.Id, ProductId = zeroProduct.Id,
            Quantity = 100, UnitPrice = 1_000_000m, UnitCostAtSale = 1m, DiscountAmount = 0m, LineTotal = 100_000_000m,
            CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var store = new ReportingDataStore(db, new KpiCalculator(), new VietnamBusinessTime(), TimeProvider.System);
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
        Assert.Equal(top.OrderByDescending(x => x.Quantity).ThenBy(x => x.Sku).ThenBy(x => x.ProductId), top);
        Assert.Equal(bottom.OrderBy(x => x.Quantity).ThenBy(x => x.Sku).ThenBy(x => x.ProductId), bottom);
        Assert.All(top.Concat(bottom), row =>
        {
            Assert.StartsWith("HOSCO-A-", row.Sku);
            Assert.True(row.Amount < 1_000_000_000m);
        });
        Assert.DoesNotContain(top.Concat(bottom), row => row.Sku == "ZERO-ONLY");

        var outsideDateRange = await store.GetProductRankingAsync(scope, filter with
        {
            From = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero),
            To = new DateTimeOffset(2027, 1, 31, 23, 59, 59, TimeSpan.Zero)
        }, bottom: false, CancellationToken.None);
        Assert.Empty(outsideDateRange);
    }
}
