namespace Hosco.Domain.Enums;

public enum SystemRole { Owner, BranchManager, ChainManager, SystemAdmin }
public enum OrderStatus { Pending, Completed, Cancelled, Returned, PartiallyReturned }
public enum PaymentStatus { Pending, Paid, Failed, Refunded, PartiallyRefunded }
public enum PaymentMethod { Cash, Card, BankTransfer, EWallet }
public enum AlertStatus { Open, Acknowledged, Resolved }
public enum AlertSeverity { Info, Warning, Critical }
public enum RefundStatus { Requested, Approved, Rejected, Completed }
