namespace Hosco.Domain.Enums;

public enum SystemRole { Owner = 0, BranchManager = 1, ChainManager = 2, SystemAdmin = 3 }
public enum OrderStatus
{
    Pending = 0,
    Completed = 1,
    Cancelled = 2,
    Returned = 3,
    PartiallyReturned = 4,
    Delivered = 5
}
public enum PaymentStatus { Pending, Paid, Failed, Refunded, PartiallyRefunded }
public enum PaymentMethod { Cash, Card, BankTransfer, EWallet }
public enum AlertStatus { Open, Acknowledged, Resolved }
// Numeric values are persisted. Medium replaces legacy Info (0), High replaces legacy Warning (1).
public enum AlertSeverity { Medium = 0, High = 1, Critical = 2 }
public enum RefundStatus { Requested, Approved, Rejected, Completed }
