using Hosco.Application.Services;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;
using Hosco.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Hosco.IntegrationTests;

public sealed class AlertSignalDataStoreTests
{
    [Fact]
    public async Task Al01_compares_observed_window_with_seven_business_day_baseline()
    {
        await using var fixture = await SignalFixture.CreateAsync();
        var now = new DateTimeOffset(2026, 7, 8, 5, 0, 0, TimeSpan.Zero);
        var observedFrom = now.AddDays(-1);
        for (var day = 1; day <= 7; day++)
        {
            fixture.AddOrder(observedFrom.AddDays(-day).AddHours(1), OrderStatus.Cancelled);
            fixture.AddOrder(observedFrom.AddDays(-day).AddHours(2), OrderStatus.Completed);
        }
        fixture.AddOrder(observedFrom.AddHours(1), OrderStatus.Cancelled);
        fixture.AddOrder(observedFrom.AddHours(2), OrderStatus.Cancelled);
        fixture.AddOrder(observedFrom.AddHours(3), OrderStatus.Cancelled);
        fixture.AddOrder(observedFrom.AddHours(4), OrderStatus.Completed);
        await fixture.Db.SaveChangesAsync();

        var signal = Assert.Single(await fixture.Store.GetCancellationRateSignalsAsync(
            fixture.Rule("AL-01", 1_440), now, default));

        Assert.Equal(AlertSeverity.Critical, signal.Severity);
        Assert.Equal(50m, signal.BaselineValue);
        Assert.Equal(50m, signal.DetectedValue);
    }

    [Fact]
    public async Task Al02_uses_only_same_elapsed_peak_window_in_utc_plus_seven()
    {
        await using var fixture = await SignalFixture.CreateAsync();
        var now = new DateTimeOffset(2026, 7, 8, 5, 0, 0, TimeSpan.Zero); // 12:00 UTC+7
        var currentStart = new DateTimeOffset(2026, 7, 8, 4, 0, 0, TimeSpan.Zero); // 11:00 UTC+7
        fixture.AddOrder(currentStart.AddMinutes(30), OrderStatus.Completed, lineTotal: 40m);
        fixture.AddOrder(currentStart.AddHours(-1), OrderStatus.Completed, lineTotal: 10_000m); // outside peak
        for (var day = 1; day <= 7; day++)
            fixture.AddOrder(currentStart.AddDays(-day).AddMinutes(30), OrderStatus.Completed, lineTotal: 100m);
        await fixture.Db.SaveChangesAsync();

        var rule = fixture.Rule("AL-02", 180);
        Assert.Empty(await fixture.Store.GetRevenueDropSignalsAsync(rule, now.AddHours(-2), default));
        var signal = Assert.Single(await fixture.Store.GetRevenueDropSignalsAsync(rule, now, default));

        Assert.Equal(AlertSeverity.Critical, signal.Severity);
        Assert.Equal(40m, signal.DetectedValue);
        Assert.Equal(100m, signal.BaselineValue);
        Assert.Contains("11:00-13:00", signal.ContextJson);
    }

    [Fact]
    public async Task Al03_requires_key_sku_and_uses_on_hand_minus_reserved_boundaries()
    {
        await using var fixture = await SignalFixture.CreateAsync();
        fixture.AddInventory(fixture.AddProduct("ZERO", true), 4, 4, 10);
        fixture.AddInventory(fixture.AddProduct("HALF", true), 9, 4, 10);
        fixture.AddInventory(fixture.AddProduct("SAFETY", true), 12, 2, 10);
        fixture.AddInventory(fixture.AddProduct("NONKEY", false), 0, 0, 10);
        await fixture.Db.SaveChangesAsync();

        var signals = await fixture.Store.GetDangerousStockSignalsAsync(fixture.Rule("AL-03", 30), fixture.Now, default);

        Assert.Equal(3, signals.Count);
        Assert.Contains(signals, x => x.Severity == AlertSeverity.Critical && x.DetectedValue == 0);
        Assert.Contains(signals, x => x.Severity == AlertSeverity.High && x.DetectedValue == 5);
        Assert.Contains(signals, x => x.Severity == AlertSeverity.Medium && x.DetectedValue == 10);
        Assert.DoesNotContain(signals, x => x.ContextJson.Contains("NONKEY", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Al04_uses_branch_baseline_absolute_threshold_and_minimum_sample()
    {
        await using var fixture = await SignalFixture.CreateAsync();
        var observedFrom = fixture.Now.AddDays(-1);
        fixture.AddOrder(observedFrom.AddDays(-2), OrderStatus.Cancelled);
        fixture.AddOrder(observedFrom.AddDays(-2).AddHours(1), OrderStatus.Completed);
        for (var i = 0; i < 5; i++)
            fixture.AddOrder(observedFrom.AddHours(i + 1), i < 4 ? OrderStatus.Cancelled : OrderStatus.Completed, fixture.EmployeeId);
        for (var i = 0; i < 10; i++)
            fixture.AddOrder(observedFrom.AddHours(i + 1), i < 6 ? OrderStatus.Cancelled : OrderStatus.Completed, fixture.OtherEmployeeId);
        await fixture.Db.SaveChangesAsync();

        var signals = await fixture.Store.GetEmployeeCancellationSignalsAsync(fixture.Rule("AL-04", 1_440), fixture.Now, default);

        Assert.Equal(2, signals.Count);
        Assert.Contains(signals, x => x.EntityKey == fixture.EmployeeId.ToString("N") && x.Severity == AlertSeverity.Medium);
        Assert.Contains(signals, x => x.EntityKey == fixture.OtherEmployeeId.ToString("N") && x.Severity == AlertSeverity.High);
        Assert.All(signals, x => Assert.Equal(50m, x.BaselineValue));
    }

    [Fact]
    public async Task Al05_evaluates_discount_and_floor_price_per_order_item()
    {
        await using var fixture = await SignalFixture.CreateAsync();
        var critical = fixture.AddProduct("CRITICAL", false);
        var discounted = fixture.AddProduct("DISCOUNT", false);
        var belowFloor = fixture.AddProduct("FLOOR", false, 120m);
        fixture.AddOrder(fixture.Now.AddHours(-1), OrderStatus.Completed, productId: critical, discount: 61m);
        fixture.AddOrder(fixture.Now.AddHours(-1), OrderStatus.Completed, productId: discounted, discount: 50m);
        fixture.AddOrder(fixture.Now.AddHours(-1), OrderStatus.Completed, productId: belowFloor);
        await fixture.Db.SaveChangesAsync();

        var signals = await fixture.Store.GetPriceDiscountSignalsAsync(fixture.Rule("AL-05", 1_440), fixture.Now, default);

        Assert.Equal(3, signals.Count);
        Assert.Contains(signals, x => x.EntityKey == critical.ToString("N") && x.Severity == AlertSeverity.Critical);
        Assert.Contains(signals, x => x.EntityKey == discounted.ToString("N") && x.Severity == AlertSeverity.High);
        Assert.Contains(signals, x => x.EntityKey == belowFloor.ToString("N") && x.Severity == AlertSeverity.High && x.BaselineValue == 120m);
    }

    private sealed class SignalFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private int _sequence;

        private SignalFixture(SqliteConnection connection, HoscoDbContext db)
        {
            _connection = connection;
            Db = db;
            Store = new AlertSignalDataStore(db, new KpiCalculator(), new KpiSnapshotStore(db), new VietnamBusinessTime());
        }

        public HoscoDbContext Db { get; }
        public AlertSignalDataStore Store { get; }
        public Guid TenantId { get; } = Guid.NewGuid();
        public Guid BranchId { get; } = Guid.NewGuid();
        public Guid EmployeeId { get; } = Guid.NewGuid();
        public Guid OtherEmployeeId { get; } = Guid.NewGuid();
        public Guid DefaultProductId { get; } = Guid.NewGuid();
        public DateTimeOffset Now { get; } = new(2026, 7, 8, 5, 0, 0, TimeSpan.Zero);

        public static async Task<SignalFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var db = new HoscoDbContext(new DbContextOptionsBuilder<HoscoDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            var fixture = new SignalFixture(connection, db);
            var created = fixture.Now.AddDays(-30);
            db.Add(new Tenant { Id = fixture.TenantId, Code = $"T-{fixture.TenantId:N}", Name = "Test", CreatedAt = created, UpdatedAt = created });
            db.Add(new Branch { Id = fixture.BranchId, TenantId = fixture.TenantId, Code = "B1", Name = "Branch", CreatedAt = created, UpdatedAt = created });
            db.AddRange(
                new Employee { Id = fixture.EmployeeId, TenantId = fixture.TenantId, BranchId = fixture.BranchId, EmployeeCode = "E1", DisplayName = "Employee 1", CreatedAt = created, UpdatedAt = created },
                new Employee { Id = fixture.OtherEmployeeId, TenantId = fixture.TenantId, BranchId = fixture.BranchId, EmployeeCode = "E2", DisplayName = "Employee 2", CreatedAt = created, UpdatedAt = created });
            db.Add(new Product { Id = fixture.DefaultProductId, TenantId = fixture.TenantId, Sku = "DEFAULT", Name = "Default", CurrentPrice = 100m, CurrentCost = 40m, Currency = "VND", CreatedAt = created, UpdatedAt = created });
            await db.SaveChangesAsync();
            return fixture;
        }

        public AlertRule Rule(string code, int windowMinutes) => new()
        {
            Id = Guid.NewGuid(), TenantId = TenantId, BranchId = BranchId, Code = code, Name = code,
            Description = code, Severity = AlertSeverity.Medium, IsEnabled = true,
            Threshold = code switch { "AL-01" => 30m, "AL-02" => 70m, "AL-03" => 50m, "AL-04" => 15m, _ => 40m },
            Baseline = code switch { "AL-01" or "AL-02" => 50m, "AL-05" => 60m, _ => null },
            WindowMinutes = windowMinutes, CooldownMinutes = 60,
            ConfigJson = AlertRuleConfiguration.DefaultJson(code), CreatedAt = Now, UpdatedAt = Now
        };

        public Guid AddProduct(string sku, bool isKeySku, decimal? floorPrice = null)
        {
            var id = Guid.NewGuid();
            Db.Products.Add(new Product
            {
                Id = id, TenantId = TenantId, Sku = sku, Name = sku, CurrentPrice = 100m, CurrentCost = 40m,
                FloorPrice = floorPrice, Currency = "VND", IsKeySku = isKeySku, CreatedAt = Now, UpdatedAt = Now
            });
            return id;
        }

        public void AddInventory(Guid productId, int onHand, int reserved, int safety) => Db.Inventories.Add(new Inventory
        {
            Id = Guid.NewGuid(), TenantId = TenantId, BranchId = BranchId, ProductId = productId,
            QuantityOnHand = onHand, ReservedQuantity = reserved, SafetyStock = safety, CreatedAt = Now, UpdatedAt = Now
        });

        public void AddOrder(DateTimeOffset at, OrderStatus status, Guid? employeeId = null, decimal lineTotal = 100m,
            Guid? productId = null, decimal discount = 0m)
        {
            var orderId = Guid.NewGuid();
            Db.Orders.Add(new Order
            {
                Id = orderId, TenantId = TenantId, BranchId = BranchId, EmployeeId = employeeId ?? EmployeeId,
                OrderNumber = $"O-{++_sequence:000}", Status = status, OrderedAt = at,
                Subtotal = 100m, DiscountAmount = discount, TotalAmount = lineTotal, Currency = "VND",
                CreatedAt = at, UpdatedAt = at
            });
            Db.OrderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(), TenantId = TenantId, OrderId = orderId, ProductId = productId ?? DefaultProductId,
                Quantity = 1, UnitPrice = 100m, UnitCostAtSale = 40m, DiscountAmount = discount,
                LineTotal = lineTotal, CreatedAt = at, UpdatedAt = at
            });
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
