# GD2 Entity Relationship Diagram

```mermaid
erDiagram
    TENANT ||--o{ BRANCH : owns
    TENANT ||--o{ APP_USER : contains
    APP_USER ||--o{ USER_ROLE : has
    ROLE ||--o{ USER_ROLE : assigned
    APP_USER ||--o{ USER_BRANCH : scoped_to
    BRANCH ||--o{ USER_BRANCH : grants
    TENANT ||--o{ CUSTOMER : owns
    TENANT ||--o{ PRODUCT : catalogs
    BRANCH ||--o{ EMPLOYEE : employs
    BRANCH ||--o{ INVENTORY : stocks
    PRODUCT ||--o{ INVENTORY : stocked_as
    BRANCH ||--o{ ORDER : receives
    CUSTOMER o|--o{ ORDER : places
    EMPLOYEE ||--o{ ORDER : processes
    ORDER ||--|{ ORDER_ITEM : contains
    PRODUCT ||--o{ ORDER_ITEM : sold_as
    ORDER ||--o{ PAYMENT : paid_by
    ORDER ||--o{ REFUND : refunded_by
    TENANT ||--o{ ALERT : receives
    BRANCH o|--o{ ALERT : concerns
    TENANT ||--o{ AUDIT_LOG : records

    TENANT { uniqueidentifier Id PK string Code UK string Name bit IsActive }
    BRANCH { uniqueidentifier Id PK uniqueidentifier TenantId FK string Code string Name bit IsActive }
    APP_USER { uniqueidentifier Id PK uniqueidentifier TenantId FK string Email UK string PasswordHash bit IsActive }
    ROLE { uniqueidentifier Id PK int Name UK }
    USER_ROLE { uniqueidentifier UserId PK_FK uniqueidentifier RoleId PK_FK }
    USER_BRANCH { uniqueidentifier UserId PK_FK uniqueidentifier BranchId PK_FK }
    CUSTOMER { uniqueidentifier Id PK uniqueidentifier TenantId FK string Name string Email }
    EMPLOYEE { uniqueidentifier Id PK uniqueidentifier TenantId FK uniqueidentifier BranchId FK string EmployeeCode }
    PRODUCT { uniqueidentifier Id PK uniqueidentifier TenantId FK string Sku decimal CurrentPrice decimal CurrentCost }
    INVENTORY { uniqueidentifier Id PK uniqueidentifier TenantId FK uniqueidentifier BranchId FK uniqueidentifier ProductId FK int QuantityOnHand int SafetyStock }
    ORDER { uniqueidentifier Id PK uniqueidentifier TenantId FK uniqueidentifier BranchId FK uniqueidentifier CustomerId FK uniqueidentifier EmployeeId FK int Status datetimeoffset OrderedAt decimal TotalAmount }
    ORDER_ITEM { uniqueidentifier Id PK uniqueidentifier TenantId FK uniqueidentifier OrderId FK uniqueidentifier ProductId FK int Quantity decimal UnitPrice decimal UnitCostAtSale decimal LineTotal }
    PAYMENT { uniqueidentifier Id PK uniqueidentifier TenantId FK uniqueidentifier OrderId FK int Status decimal Amount }
    REFUND { uniqueidentifier Id PK uniqueidentifier TenantId FK uniqueidentifier BranchId FK uniqueidentifier OrderId FK int Status decimal Amount }
    ALERT { uniqueidentifier Id PK uniqueidentifier TenantId FK uniqueidentifier BranchId string Type int Status string PayloadJson }
    AUDIT_LOG { uniqueidentifier Id PK uniqueidentifier TenantId FK uniqueidentifier UserId uniqueidentifier BranchId string Action string QueryId string CorrelationId }
```

All aggregate roots except `Role` carry `TenantId`; branch-bound transactions additionally carry `BranchId`. `OrderItem.UnitCostAtSale` preserves historical COGS input and prevents recalculating past gross profit from `Product.CurrentCost`.
