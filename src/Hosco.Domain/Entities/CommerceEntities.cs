using Hosco.Domain.Common;
using Hosco.Domain.Enums;

namespace Hosco.Domain.Entities;

public sealed class Customer : TenantEntity
{
    public required string Name { get; set; }
    public string? Email { get; set; }
    public string? PhoneMasked { get; set; }
}

public sealed class Employee : TenantEntity
{
    public Guid BranchId { get; set; }
    public required string EmployeeCode { get; set; }
    public required string DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public Branch Branch { get; set; } = null!;
}

public sealed class Product : TenantEntity
{
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal CurrentCost { get; set; }
    public required string Currency { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class Inventory : TenantEntity
{
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public int QuantityOnHand { get; set; }
    public int SafetyStock { get; set; }
    public Branch Branch { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public sealed class Order : TenantEntity
{
    public Guid BranchId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid EmployeeId { get; set; }
    public required string OrderNumber { get; set; }
    public OrderStatus Status { get; set; }
    public DateTimeOffset OrderedAt { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public required string Currency { get; set; }
    public Branch Branch { get; set; } = null!;
    public Customer? Customer { get; set; }
    public Employee Employee { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}

public sealed class OrderItem : TenantEntity
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCostAtSale { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public sealed class Payment : TenantEntity
{
    public Guid OrderId { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public DateTimeOffset PaidAt { get; set; }
    public Order Order { get; set; } = null!;
}

public sealed class Refund : TenantEntity
{
    public Guid OrderId { get; set; }
    public Guid BranchId { get; set; }
    public RefundStatus Status { get; set; }
    public decimal Amount { get; set; }
    public required string ReasonCode { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Order Order { get; set; } = null!;
}
