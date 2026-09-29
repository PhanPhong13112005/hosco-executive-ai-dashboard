using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hosco.Application.Services;
using Hosco.Domain.Entities;
using Hosco.Domain.Enums;
using Hosco.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Hosco.Infrastructure.Persistence;

public static class DemoSeedIds
{
    public static readonly Guid TenantA = Id("tenant-a");
    public static readonly Guid TenantB = Id("tenant-b");
    public static readonly Guid BranchA1 = Id("tenant-a-branch-1");
    public static readonly Guid BranchA2 = Id("tenant-a-branch-2");
    public static readonly Guid BranchB1 = Id("tenant-b-branch-1");
    public static readonly Guid BranchB2 = Id("tenant-b-branch-2");
    public static Guid Id(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes[..16]);
    }
}

public static class DemoSeed
{
    public const string DemoPassword = "HoscoDemo!2026";
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static async Task SeedAsync(HoscoDbContext db, CancellationToken ct = default)
    {
        if (await db.Tenants.AnyAsync(ct))
        {
            await EnsureFinalBusinessFixturesAsync(db, ct);
            return;
        }
        var now = Start;
        var tenants = new[]
        {
            new Tenant { Id = DemoSeedIds.TenantA, Code = "HOSCO-A", Name = "HOSCO Demo Retail", CreatedAt = now, UpdatedAt = now },
            new Tenant { Id = DemoSeedIds.TenantB, Code = "HOSCO-B", Name = "Isolation Fixture Retail", CreatedAt = now, UpdatedAt = now }
        };
        var branches = new[]
        {
            Branch(DemoSeedIds.BranchA1, DemoSeedIds.TenantA, "A-HCM", "HOSCO Hồ Chí Minh"),
            Branch(DemoSeedIds.BranchA2, DemoSeedIds.TenantA, "A-HN", "HOSCO Hà Nội"),
            Branch(DemoSeedIds.BranchB1, DemoSeedIds.TenantB, "B-DN", "Fixture Đà Nẵng"),
            Branch(DemoSeedIds.BranchB2, DemoSeedIds.TenantB, "B-CT", "Fixture Cần Thơ")
        };
        var roles = Enum.GetValues<SystemRole>().Select(x => new Role { Id = DemoSeedIds.Id($"role-{x}"), Name = x, CreatedAt = now, UpdatedAt = now }).ToArray();
        db.AddRange(tenants); db.AddRange(branches); db.AddRange(roles);

        var users = new[]
        {
            User("owner@hosco.local", "Demo Owner", DemoSeedIds.TenantA, SystemRole.Owner, [DemoSeedIds.BranchA1]),
            User("branch.manager@hosco.local", "Demo Branch Manager", DemoSeedIds.TenantA, SystemRole.BranchManager, [DemoSeedIds.BranchA1]),
            User("chain.manager@hosco.local", "Demo Chain Manager", DemoSeedIds.TenantA, SystemRole.ChainManager, []),
            User("admin@hosco.local", "Demo System Admin", DemoSeedIds.TenantA, SystemRole.SystemAdmin, [DemoSeedIds.BranchA1]),
            User("owner@fixture.local", "Other Tenant Owner", DemoSeedIds.TenantB, SystemRole.Owner, [DemoSeedIds.BranchB1])
        };
        foreach (var seeded in users)
        {
            db.Users.Add(seeded.User);
            db.UserRoles.Add(new UserRole { UserId = seeded.User.Id, RoleId = DemoSeedIds.Id($"role-{seeded.Role}") });
            foreach (var branchId in seeded.Branches) db.UserBranches.Add(new UserBranch { UserId = seeded.User.Id, BranchId = branchId });
        }

        foreach (var tenant in tenants)
        {
            var tenantBranches = branches.Where(x => x.TenantId == tenant.Id).ToArray();
            var products = Enumerable.Range(1, 8).Select(i => new Product
            {
                Id = DemoSeedIds.Id($"{tenant.Code}-product-{i}"),
                TenantId = tenant.Id,
                Sku = $"{tenant.Code}-SKU-{i:000}",
                Name = $"Demo Product {i}",
                CurrentPrice = 25_000m + i * 7_500m,
                CurrentCost = 15_000m + i * 4_000m,
                FloorPrice = 20_000m + i * 5_000m,
                Currency = "VND",
                IsKeySku = i <= 3,
                CreatedAt = now,
                UpdatedAt = now
            }).ToArray();
            db.Products.AddRange(products);
            var customers = Enumerable.Range(1, 16).Select(i => new Customer
            {
                Id = DemoSeedIds.Id($"{tenant.Code}-customer-{i}"),
                TenantId = tenant.Id,
                Name = $"Customer {i}",
                Email = $"customer{i}@{tenant.Code.ToLowerInvariant()}.local",
                PhoneMasked = $"***{i:0000}",
                CreatedAt = now,
                UpdatedAt = now
            }).ToArray();
            db.Customers.AddRange(customers);
            var employees = tenantBranches.SelectMany((branch, bi) => Enumerable.Range(1, 3).Select(i => new Employee
            {
                Id = DemoSeedIds.Id($"{branch.Code}-employee-{i}"),
                TenantId = tenant.Id,
                BranchId = branch.Id,
                EmployeeCode = $"{branch.Code}-E{i:00}",
                DisplayName = $"Cashier {bi + 1}-{i}",
                CreatedAt = now,
                UpdatedAt = now
            })).ToArray();
            db.Employees.AddRange(employees);

            foreach (var branch in tenantBranches)
                foreach (var (product, index) in products.Select((p, i) => (p, i)))
                    db.Inventories.Add(new Inventory
                    {
                        Id = DemoSeedIds.Id($"inventory-{branch.Id}-{product.Id}"),
                        TenantId = tenant.Id,
                        BranchId = branch.Id,
                        ProductId = product.Id,
                        QuantityOnHand = index < 2 && branch.Id == tenantBranches[0].Id ? 2 + index : 35 + index,
                        ReservedQuantity = index < 2 && branch.Id == tenantBranches[0].Id ? 1 + index : index % 3,
                        SafetyStock = 8,
                        CreatedAt = now,
                        UpdatedAt = new DateTimeOffset(2026, 6, 30, 18, 0, 0, TimeSpan.Zero)
                    });

            var sequence = 0;
            for (var day = 0; day < 181; day++)
                foreach (var branch in tenantBranches)
                {
                    var date = Start.AddDays(day);
                    var count = date.Month == 5 && date.Day is >= 10 and <= 20 ? 1 : 3;
                    for (var n = 0; n < count; n++)
                    {
                        sequence++;
                        var employee = employees.Where(x => x.BranchId == branch.Id).ElementAt(sequence % 3);
                        var status = date.Month == 6 && date.Day >= 20 && sequence % 3 == 0 ? OrderStatus.Cancelled
                            : sequence % 37 == 0 ? OrderStatus.PartiallyReturned
                            : sequence % 29 == 0 ? OrderStatus.Returned
                            : sequence % 5 == 0 ? OrderStatus.Delivered
                            : OrderStatus.Completed;
                        var orderId = DemoSeedIds.Id($"{tenant.Code}-order-{sequence}");
                        var discount = date.Month == 4 && date.Day is >= 12 and <= 14 ? 40_000m : sequence % 11 == 0 ? 5_000m : 0m;
                        var p1 = products[sequence % products.Length]; var p2 = products[(sequence + 3) % products.Length];
                        var item1 = Item(tenant.Id, orderId, p1, 1 + sequence % 2, 0, 1, now);
                        var item2 = Item(tenant.Id, orderId, p2, 1, discount, 2, now);
                        var subtotal = item1.UnitPrice * item1.Quantity + item2.UnitPrice * item2.Quantity;
                        var total = subtotal - discount;
                        var orderedAt = date.AddHours(8 + n * 3);
                        var order = new Order
                        {
                            Id = orderId,
                            TenantId = tenant.Id,
                            BranchId = branch.Id,
                            CustomerId = customers[sequence % customers.Length].Id,
                            EmployeeId = employee.Id,
                            OrderNumber = $"{tenant.Code}-{sequence:000000}",
                            Status = status,
                            OrderedAt = orderedAt,
                            Subtotal = subtotal,
                            DiscountAmount = discount,
                            TotalAmount = total,
                            Currency = "VND",
                            CreatedAt = orderedAt,
                            UpdatedAt = orderedAt
                        };
                        db.Orders.Add(order); db.OrderItems.AddRange(item1, item2);
                        db.Payments.Add(new Payment
                        {
                            Id = DemoSeedIds.Id($"payment-{orderId}"),
                            TenantId = tenant.Id,
                            OrderId = orderId,
                            Method = (PaymentMethod)(sequence % 4),
                            Status = status == OrderStatus.Cancelled ? PaymentStatus.Refunded : PaymentStatus.Paid,
                            Amount = total,
                            PaidAt = orderedAt.AddMinutes(2),
                            CreatedAt = orderedAt,
                            UpdatedAt = orderedAt
                        });
                        if (status is OrderStatus.Returned or OrderStatus.PartiallyReturned)
                        {
                            var refundId = DemoSeedIds.Id($"refund-{orderId}");
                            var partial = status == OrderStatus.PartiallyReturned;
                            var returnedValue = partial ? decimal.Round(item1.LineTotal / item1.Quantity, 2) : total;
                            db.Refunds.Add(new Refund
                            {
                                Id = refundId,
                                TenantId = tenant.Id,
                                BranchId = branch.Id,
                                OrderId = orderId,
                                Status = RefundStatus.Completed,
                                Amount = returnedValue,
                                ReasonCode = "DEMO_RETURN",
                                RequestedAt = orderedAt.AddDays(2),
                                CompletedAt = orderedAt.AddDays(3),
                                CreatedAt = orderedAt.AddDays(2),
                                UpdatedAt = orderedAt.AddDays(3)
                            });
                            db.RefundItems.Add(new RefundItem
                            {
                                Id = DemoSeedIds.Id($"refund-item-{refundId}-{item1.Id}"), TenantId = tenant.Id,
                                RefundId = refundId, OrderItemId = item1.Id,
                                Quantity = partial ? 1 : item1.Quantity,
                                ReturnedValue = partial ? returnedValue : item1.LineTotal,
                                CreatedAt = orderedAt.AddDays(2), UpdatedAt = orderedAt.AddDays(3)
                            });
                            if (!partial)
                                db.RefundItems.Add(new RefundItem
                                {
                                    Id = DemoSeedIds.Id($"refund-item-{refundId}-{item2.Id}"), TenantId = tenant.Id,
                                    RefundId = refundId, OrderItemId = item2.Id, Quantity = item2.Quantity,
                                    ReturnedValue = item2.LineTotal,
                                    CreatedAt = orderedAt.AddDays(2), UpdatedAt = orderedAt.AddDays(3)
                                });
                        }
                    }
                }
        }

        AddAnomaly(db, DemoSeedIds.TenantA, DemoSeedIds.BranchA1, "cancellation-spike", "Cancellation rate spike fixture", new DateTimeOffset(2026, 6, 20, 0, 0, 0, TimeSpan.Zero));
        AddAnomaly(db, DemoSeedIds.TenantA, DemoSeedIds.BranchA1, "revenue-drop", "Revenue drop fixture", new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero));
        AddAnomaly(db, DemoSeedIds.TenantA, DemoSeedIds.BranchA1, "low-stock", "Low stock fixture", new DateTimeOffset(2026, 6, 30, 0, 0, 0, TimeSpan.Zero));
        AddAnomaly(db, DemoSeedIds.TenantA, DemoSeedIds.BranchA1, "employee-cancellation", "Employee cancellation fixture", new DateTimeOffset(2026, 6, 20, 0, 0, 0, TimeSpan.Zero));
        AddAnomaly(db, DemoSeedIds.TenantA, DemoSeedIds.BranchA2, "abnormal-discount", "Abnormal discount fixture", new DateTimeOffset(2026, 4, 12, 0, 0, 0, TimeSpan.Zero));
        AddAlertRules(db, branches);
        await db.SaveChangesAsync(ct);
    }

    private static Branch Branch(Guid id, Guid tenantId, string code, string name) => new() { Id = id, TenantId = tenantId, Code = code, Name = name, CreatedAt = Start, UpdatedAt = Start };
    private static (AppUser User, SystemRole Role, Guid[] Branches) User(string email, string name, Guid tenantId, SystemRole role, Guid[] branches)
    {
        var id = DemoSeedIds.Id($"user-{email}");
        return (new AppUser { Id = id, TenantId = tenantId, Email = email, DisplayName = name, PasswordHash = Pbkdf2PasswordService.HashForSeed(DemoPassword, id), CreatedAt = Start, UpdatedAt = Start }, role, branches);
    }
    private static OrderItem Item(Guid tenantId, Guid orderId, Product product, int quantity, decimal discount, int index, DateTimeOffset now) => new()
    {
        Id = DemoSeedIds.Id($"item-{orderId}-{index}"),
        TenantId = tenantId,
        OrderId = orderId,
        ProductId = product.Id,
        Quantity = quantity,
        UnitPrice = product.CurrentPrice,
        UnitCostAtSale = product.CurrentCost,
        DiscountAmount = discount,
        LineTotal = product.CurrentPrice * quantity - discount,
        CreatedAt = now,
        UpdatedAt = now
    };
    private static void AddAnomaly(HoscoDbContext db, Guid tenantId, Guid branchId, string type, string title, DateTimeOffset at) =>
        db.Alerts.Add(new Alert
        {
            Id = DemoSeedIds.Id($"alert-{tenantId}-{type}"),
            TenantId = tenantId,
            BranchId = branchId,
            Type = type,
            RuleId = DemoSeedIds.Id($"alert-rule-{tenantId}-{branchId}-{RuleCode(type)}"),
            RuleCode = RuleCode(type),
            Severity = AlertSeverity.High,
            Status = AlertStatus.Open,
            Title = title,
            Message = $"GD3 deterministic fixture for {type}.",
            PayloadJson = JsonSerializer.Serialize(new { fixture = true, expectedWindowStart = at }),
            DetectedAt = at,
            DedupKey = $"{tenantId:N}:{branchId:N}:{RuleCode(type)}:fixture",
            CreatedAt = at,
            UpdatedAt = at
        });

    private static string RuleCode(string type) => type switch
    {
        "cancellation-spike" => "AL-01",
        "revenue-drop" => "AL-02",
        "low-stock" => "AL-03",
        "employee-cancellation" => "AL-04",
        "abnormal-discount" => "AL-05",
        _ => "LEGACY"
    };

    private static async Task EnsureFinalBusinessFixturesAsync(HoscoDbContext db, CancellationToken ct)
    {
        var assignments = new[]
        {
            ("owner@hosco.local", DemoSeedIds.BranchA1),
            ("branch.manager@hosco.local", DemoSeedIds.BranchA1),
            ("admin@hosco.local", DemoSeedIds.BranchA1),
            ("owner@fixture.local", DemoSeedIds.BranchB1)
        };
        foreach (var (email, branchId) in assignments)
        {
            var userId = await db.Users.Where(x => x.Email == email).Select(x => (Guid?)x.Id).SingleOrDefaultAsync(ct);
            if (userId.HasValue && !await db.UserBranches.AnyAsync(x => x.UserId == userId.Value && x.BranchId == branchId, ct))
                db.UserBranches.Add(new UserBranch { UserId = userId.Value, BranchId = branchId });
        }

        var products = await db.Products.OrderBy(x => x.Sku).ToListAsync(ct);
        foreach (var tenantProducts in products.GroupBy(x => x.TenantId))
        {
            var index = 0;
            foreach (var product in tenantProducts)
            {
                product.IsKeySku = index++ < 3;
                product.FloorPrice ??= decimal.Round(product.CurrentPrice * 0.75m, 0);
            }
        }
        var inventories = await db.Inventories.OrderBy(x => x.ProductId).ToListAsync(ct);
        foreach (var inventory in inventories)
            if (inventory.ReservedQuantity == 0)
                inventory.ReservedQuantity = inventory.QuantityOnHand <= inventory.SafetyStock ? Math.Min(1, inventory.QuantityOnHand) : 0;

        var refunds = await db.Refunds.Include(x => x.Items).Include(x => x.Order).ThenInclude(x => x.Items)
            .Where(x => x.Status == RefundStatus.Completed).ToListAsync(ct);
        foreach (var refund in refunds.Where(x => x.Items.Count == 0))
        {
            var sourceItems = refund.Order.Items.OrderBy(x => x.Id).ToList();
            if (sourceItems.Count == 0) continue;
            var partial = refund.Order.Status == OrderStatus.PartiallyReturned;
            foreach (var item in partial ? sourceItems.Take(1) : sourceItems)
                db.RefundItems.Add(new RefundItem
                {
                    Id = DemoSeedIds.Id($"refund-item-{refund.Id}-{item.Id}"), TenantId = refund.TenantId,
                    RefundId = refund.Id, OrderItemId = item.Id, Quantity = partial ? 1 : item.Quantity,
                    ReturnedValue = partial ? decimal.Round(item.LineTotal / item.Quantity, 2) : item.LineTotal,
                    CreatedAt = refund.CreatedAt, UpdatedAt = refund.UpdatedAt
                });
        }

        var branches = await db.Branches.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);
        var existingRules = await db.AlertRules.ToListAsync(ct);
        foreach (var tenantBranches in branches.GroupBy(x => x.TenantId))
        {
            foreach (var code in RuleCodes)
            {
                var global = existingRules.FirstOrDefault(x => x.TenantId == tenantBranches.Key && x.Code == code && x.BranchId == null);
                foreach (var (branch, branchIndex) in tenantBranches.Select((branch, index) => (branch, index)))
                {
                    var rule = existingRules.FirstOrDefault(x => x.TenantId == tenantBranches.Key && x.Code == code && x.BranchId == branch.Id);
                    if (rule is null && branchIndex == 0 && global is not null)
                    {
                        rule = global;
                        rule.BranchId = branch.Id;
                    }
                    if (rule is null)
                    {
                        rule = CreateRule(tenantBranches.Key, branch.Id, code);
                        db.AlertRules.Add(rule);
                        existingRules.Add(rule);
                    }
                    ApplyRuleDefaults(rule);
                }
            }
        }

        foreach (var alert in await db.Alerts.Where(x => x.BranchId != null).ToListAsync(ct))
        {
            var targetRule = existingRules.FirstOrDefault(x => x.TenantId == alert.TenantId && x.BranchId == alert.BranchId && x.Code == alert.RuleCode);
            if (targetRule is not null) alert.RuleId = targetRule.Id;
        }
        await db.SaveChangesAsync(ct);
    }

    private static readonly string[] RuleCodes = ["AL-01", "AL-02", "AL-03", "AL-04", "AL-05"];

    private static void AddAlertRules(HoscoDbContext db, IEnumerable<Branch> branches)
    {
        foreach (var branch in branches)
            foreach (var code in RuleCodes)
                db.AlertRules.Add(CreateRule(branch.TenantId, branch.Id, code));
    }

    private static AlertRule CreateRule(Guid tenantId, Guid branchId, string code)
    {
        var rule = new AlertRule
        {
            Id = DemoSeedIds.Id($"alert-rule-{tenantId}-{branchId}-{code}"),
            TenantId = tenantId,
            BranchId = branchId,
            Code = code, Name = code, Description = code, ConfigJson = "{}",
            CreatedAt = Start,
            UpdatedAt = Start
        };
        ApplyRuleDefaults(rule);
        return rule;
    }

    private static void ApplyRuleDefaults(AlertRule rule)
    {
        var values = rule.Code switch
        {
            "AL-01" => ("Tỷ lệ hủy đơn bất thường", "So sánh tỷ lệ hủy 1 ngày với baseline 7 ngày cùng chi nhánh.", AlertSeverity.High, 30m, (decimal?)50m, 1_440, 240),
            "AL-02" => ("Doanh thu giờ cao điểm giảm", "So sánh khung 11:00–13:00 và 17:00–20:00 UTC+7 với 7 ngày trước.", AlertSeverity.High, 70m, (decimal?)50m, 180, 120),
            "AL-03" => ("SKU quan trọng tồn kho thấp", "IsKeySku với Available = OnHand - Reserved so với SafetyStock.", AlertSeverity.Medium, 50m, (decimal?)null, 30, 360),
            "AL-04" => ("Nhân viên có hủy/hoàn bất thường", "So sánh Rate_NV 1 ngày với baseline chi nhánh 7 ngày hoặc ngưỡng tuyệt đối.", AlertSeverity.Medium, 15m, (decimal?)null, 1_440, 1_440),
            "AL-05" => ("Giá bán hoặc giảm giá bất thường", "Kiểm tra discount và FloorPrice theo SKU.", AlertSeverity.High, 40m, (decimal?)60m, 1_440, 60),
            _ => throw new InvalidOperationException($"Unknown alert rule {rule.Code}.")
        };
        rule.Name = values.Item1;
        rule.Description = values.Item2;
        rule.Severity = values.Item3;
        rule.Threshold = values.Item4;
        rule.Baseline = values.Item5;
        rule.WindowMinutes = values.Item6;
        rule.CooldownMinutes = values.Item7;
        rule.IsEnabled = true;
        rule.ConfigJson = AlertRuleConfiguration.DefaultJson(rule.Code);
    }
}
