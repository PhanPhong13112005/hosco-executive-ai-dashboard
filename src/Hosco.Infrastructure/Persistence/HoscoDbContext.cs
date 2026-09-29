using Hosco.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hosco.Infrastructure.Persistence;

public sealed class HoscoDbContext(DbContextOptions<HoscoDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserBranch> UserBranches => Set<UserBranch>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<RefundItem> RefundItems => Set<RefundItem>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Tenant>(e => { e.HasIndex(x => x.Code).IsUnique(); e.Property(x => x.Name).HasMaxLength(200); e.Property(x => x.Code).HasMaxLength(50); });
        b.Entity<Branch>(e => { e.HasIndex(x => new { x.TenantId, x.Code }).IsUnique(); e.HasOne(x => x.Tenant).WithMany(x => x.Branches).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict); });
        TenantFk<AppUser>(b); TenantFk<Customer>(b); TenantFk<Employee>(b); TenantFk<Product>(b);
        TenantFk<Inventory>(b); TenantFk<Order>(b); TenantFk<OrderItem>(b); TenantFk<Payment>(b);
        TenantFk<Refund>(b); TenantFk<RefundItem>(b); TenantFk<AlertRule>(b); TenantFk<Alert>(b); TenantFk<AuditLog>(b);
        b.Entity<AppUser>(e => { e.HasIndex(x => x.Email).IsUnique(); e.Property(x => x.Email).HasMaxLength(320); e.Property(x => x.PasswordHash).HasMaxLength(500); });
        b.Entity<Role>(e => e.HasIndex(x => x.Name).IsUnique());
        b.Entity<UserRole>(e => { e.HasKey(x => new { x.UserId, x.RoleId }); e.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId); e.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId); });
        b.Entity<UserBranch>(e => { e.HasKey(x => new { x.UserId, x.BranchId }); e.HasOne(x => x.User).WithMany(x => x.UserBranches).HasForeignKey(x => x.UserId); e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<Customer>(e => e.HasIndex(x => new { x.TenantId, x.Email }));
        b.Entity<Employee>(e => { e.HasIndex(x => new { x.TenantId, x.EmployeeCode }).IsUnique(); e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<Product>(e => { e.HasIndex(x => new { x.TenantId, x.Sku }).IsUnique(); Money(e.Property(x => x.CurrentPrice)); Money(e.Property(x => x.CurrentCost)); Money(e.Property(x => x.FloorPrice)); });
        b.Entity<Inventory>(e => { e.HasIndex(x => new { x.TenantId, x.BranchId, x.ProductId }).IsUnique(); e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict); });
        b.Entity<Order>(e => { e.HasIndex(x => new { x.TenantId, x.OrderNumber }).IsUnique(); e.HasIndex(x => new { x.TenantId, x.BranchId, x.OrderedAt }); e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict); e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull); e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict); Money(e.Property(x => x.Subtotal)); Money(e.Property(x => x.DiscountAmount)); Money(e.Property(x => x.TotalAmount)); });
        b.Entity<OrderItem>(e => { e.HasIndex(x => new { x.TenantId, x.OrderId }); e.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId); e.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict); Money(e.Property(x => x.UnitPrice)); Money(e.Property(x => x.UnitCostAtSale)); Money(e.Property(x => x.DiscountAmount)); Money(e.Property(x => x.LineTotal)); });
        b.Entity<Payment>(e => { e.HasOne(x => x.Order).WithMany(x => x.Payments).HasForeignKey(x => x.OrderId); Money(e.Property(x => x.Amount)); });
        b.Entity<Refund>(e => { e.HasIndex(x => new { x.TenantId, x.BranchId, x.RequestedAt }); e.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict); Money(e.Property(x => x.Amount)); });
        b.Entity<RefundItem>(e =>
        {
            e.HasIndex(x => new { x.TenantId, x.RefundId, x.OrderItemId }).IsUnique();
            e.HasOne(x => x.Refund).WithMany(x => x.Items).HasForeignKey(x => x.RefundId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.OrderItem).WithMany().HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.Restrict);
            Money(e.Property(x => x.ReturnedValue));
        });
        b.Entity<AlertRule>(e =>
        {
            e.HasIndex(x => new { x.TenantId, x.BranchId, x.Code }).IsUnique();
            e.HasIndex(x => new { x.TenantId, x.BranchId, x.IsEnabled });
            e.Property(x => x.Code).HasMaxLength(32);
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.ConfigJson).HasMaxLength(4000);
            Money(e.Property(x => x.Threshold));
            Money(e.Property(x => x.Baseline));
        });
        b.Entity<Alert>(e =>
        {
            e.HasIndex(x => new { x.TenantId, x.Status, x.DetectedAt });
            e.HasIndex(x => new { x.TenantId, x.BranchId, x.Severity, x.DetectedAt });
            e.HasIndex(x => new { x.TenantId, x.DedupKey, x.DetectedAt });
            e.HasIndex(x => x.RuleId);
            e.Property(x => x.Type).HasMaxLength(64);
            e.Property(x => x.RuleCode).HasMaxLength(32);
            e.Property(x => x.Title).HasMaxLength(250);
            e.Property(x => x.Message).HasMaxLength(2000);
            e.Property(x => x.DedupKey).HasMaxLength(450);
            e.Property(x => x.ResolutionNote).HasMaxLength(2000);
            Money(e.Property(x => x.DetectedValue));
            Money(e.Property(x => x.ThresholdValue));
            Money(e.Property(x => x.BaselineValue));
            e.HasOne(x => x.Rule).WithMany(x => x.Alerts).HasForeignKey(x => x.RuleId).OnDelete(DeleteBehavior.SetNull);
        });
        b.Entity<AuditLog>(e => e.HasIndex(x => new { x.TenantId, x.OccurredAt }));
    }

    private static void Money(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<decimal> property) =>
        property.HasPrecision(18, 2);

    private static void Money(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<decimal?> property) =>
        property.HasPrecision(18, 2);

    private static void TenantFk<T>(ModelBuilder builder) where T : Hosco.Domain.Common.TenantEntity =>
        builder.Entity<T>().HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
}
