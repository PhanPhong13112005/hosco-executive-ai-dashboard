# GD2 Technical Data Dictionary

No BA Business Data Dictionary was present during implementation. This technical dictionary must be reconciled with, and must not overwrite, the latest BA dictionary when it becomes available. Unless noted, every entity with `Id` also has non-null `CreatedAt` and `UpdatedAt` (`datetimeoffset`) for traceability.

## Tenant

| Column | SQL type | Null | Key | Description / rule |
|---|---|---:|---|---|
| Id | uniqueidentifier | No | PK | Tenant identifier |
| Code | nvarchar(50) | No | UK | Stable tenant code |
| Name | nvarchar(200) | No | | Display name |
| IsActive | bit | No | | Tenant enabled flag |

## Branch

| Column | SQL type | Null | Key | Description / rule |
|---|---|---:|---|---|
| Id | uniqueidentifier | No | PK | Branch identifier |
| TenantId | uniqueidentifier | No | FK Tenant | Mandatory security partition |
| Code | nvarchar(max) | No | UK with TenantId | Tenant-local branch code |
| Name | nvarchar(max) | No | | Display name |
| Address | nvarchar(max) | Yes | | Optional address |
| IsActive | bit | No | | Branch enabled flag |

## AppUser

| Column | SQL type | Null | Key | Description / rule |
|---|---|---:|---|---|
| Id | uniqueidentifier | No | PK | Authenticated subject |
| TenantId | uniqueidentifier | No | FK Tenant | Tenant claim source |
| Email | nvarchar(320) | No | UK | Normalized lower-case login |
| DisplayName | nvarchar(max) | No | | Non-sensitive display name |
| PasswordHash | nvarchar(500) | No | | PBKDF2 encoded hash, never plaintext |
| IsActive | bit | No | | Login enabled flag |

## Role, UserRole, UserBranch

| Table.Column | SQL type | Null | Key | Description / rule |
|---|---|---:|---|---|
| Role.Id | uniqueidentifier | No | PK | Role ID |
| Role.Name | int | No | UK | `Owner`, `BranchManager`, `ChainManager`, `SystemAdmin` enum |
| UserRole.UserId | uniqueidentifier | No | PK/FK AppUser | Assigned user |
| UserRole.RoleId | uniqueidentifier | No | PK/FK Role | Assigned role |
| UserBranch.UserId | uniqueidentifier | No | PK/FK AppUser | Scoped user |
| UserBranch.BranchId | uniqueidentifier | No | PK/FK Branch | Permitted branch |

## Customer

| Column | SQL type | Null | Key | Description / rule |
|---|---|---:|---|---|
| Id | uniqueidentifier | No | PK | Customer ID |
| TenantId | uniqueidentifier | No | FK Tenant | Tenant partition |
| Name | nvarchar(max) | No | | Customer name; avoid logging |
| Email | nvarchar(max) | Yes | index with TenantId | Optional PII; avoid logging |
| PhoneMasked | nvarchar(max) | Yes | | Masked value only in demo seed |

## Employee

| Column | SQL type | Null | Key | Description / rule |
|---|---|---:|---|---|
| Id | uniqueidentifier | No | PK | Employee/cashier ID |
| TenantId | uniqueidentifier | No | FK Tenant | Tenant partition |
| BranchId | uniqueidentifier | No | FK Branch | Home branch |
| EmployeeCode | nvarchar(max) | No | UK with TenantId | Stable employee code |
| DisplayName | nvarchar(max) | No | | Display name |
| IsActive | bit | No | | Employment/system active flag |

## Product

| Column | SQL type | Null | Key | Description / rule |
|---|---|---:|---|---|
| Id | uniqueidentifier | No | PK | Product ID |
| TenantId | uniqueidentifier | No | FK Tenant | Tenant partition |
| Sku | nvarchar(max) | No | UK with TenantId | Stock keeping unit |
| Name | nvarchar(max) | No | | Product name |
| CurrentPrice | decimal(18,2) | No | | Current list price; not historical sale price |
| CurrentCost | decimal(18,2) | No | | Current cost; not used blindly for historical GP |
| Currency | nvarchar(max) | No | | ISO-style currency code, seed uses VND |
| IsActive | bit | No | | Catalog active flag |

## Inventory

| Column | SQL type | Null | Key | Description / rule |
|---|---|---:|---|---|
| Id | uniqueidentifier | No | PK | Inventory row ID |
| TenantId | uniqueidentifier | No | FK Tenant | Tenant partition |
| BranchId | uniqueidentifier | No | FK Branch, UK group | Stock location |
| ProductId | uniqueidentifier | No | FK Product, UK group | Stocked product |
| QuantityOnHand | int | No | | Current on-hand units |
| SafetyStock | int | No | | Configured threshold input; BA dangerous-stock rule pending |

## Order

| Column | SQL type | Null | Key | Description / rule |
|---|---|---:|---|---|
| Id | uniqueidentifier | No | PK | Order ID |
| TenantId | uniqueidentifier | No | FK Tenant | Mandatory query predicate |
| BranchId | uniqueidentifier | No | FK Branch | Mandatory branch predicate |
| CustomerId | uniqueidentifier | Yes | FK Customer | Optional customer |
| EmployeeId | uniqueidentifier | No | FK Employee | Processing cashier |
| OrderNumber | nvarchar(max) | No | UK with TenantId | Human-readable number |
| Status | int | No | | Pending/Completed/Cancelled/Returned/PartiallyReturned |
| OrderedAt | datetimeoffset | No | indexed | Business event time |
| Subtotal | decimal(18,2) | No | | Pre-order-discount amount |
| DiscountAmount | decimal(18,2) | No | | Order discount |
| TotalAmount | decimal(18,2) | No | | Stored charged total; KPI interpretation awaits BA |
| Currency | nvarchar(max) | No | | Currency code |

## OrderItem

| Column | SQL type | Null | Key | Description / rule |
|---|---|---:|---|---|
| Id | uniqueidentifier | No | PK | Line ID |
| TenantId | uniqueidentifier | No | FK Tenant | Defense-in-depth partition |
| OrderId | uniqueidentifier | No | FK Order | Parent order |
| ProductId | uniqueidentifier | No | FK Product | Sold product |
| Quantity | int | No | | Sold units |
| UnitPrice | decimal(18,2) | No | | Price captured at transaction time |
| UnitCostAtSale | decimal(18,2) | No | | Cost snapshot for historical COGS |
| DiscountAmount | decimal(18,2) | No | | Line discount |
| LineTotal | decimal(18,2) | No | | Stored line total |

## Payment

| Column | SQL type | Null | Key | Description / rule |
|---|---|---:|---|---|
| Id | uniqueidentifier | No | PK | Payment ID |
| TenantId | uniqueidentifier | No | FK Tenant | Tenant partition |
| OrderId | uniqueidentifier | No | FK Order | Paid order |
| Method | int | No | | Cash/Card/BankTransfer/EWallet |
| Status | int | No | | Pending/Paid/Failed/Refunded/PartiallyRefunded |
| Amount | decimal(18,2) | No | | Payment amount |
| PaidAt | datetimeoffset | No | | Payment event time |

## Refund

| Column | SQL type | Null | Key | Description / rule |
|---|---|---:|---|---|
| Id | uniqueidentifier | No | PK | Refund/return financial record |
| TenantId | uniqueidentifier | No | FK Tenant | Tenant partition |
| BranchId | uniqueidentifier | No | indexed | Branch partition |
| OrderId | uniqueidentifier | No | FK Order | Original order |
| Status | int | No | | Requested/Approved/Rejected/Completed |
| Amount | decimal(18,2) | No | | Refund amount |
| ReasonCode | nvarchar(max) | No | | Non-free-text reason code |
| RequestedAt | datetimeoffset | No | indexed | Request time |
| CompletedAt | datetimeoffset | Yes | | Completion time |

## Alert

| Column | SQL type | Null | Key | Description / rule |
|---|---|---:|---|---|
| Id | uniqueidentifier | No | PK | Alert ID |
| TenantId | uniqueidentifier | No | FK Tenant | Tenant partition |
| BranchId | uniqueidentifier | Yes | logical FK Branch | Optional branch scope |
| Type | nvarchar(max) | No | | Versionable alert type code |
| Severity | int | No | | Info/Warning/Critical |
| Status | int | No | indexed | Open/Acknowledged/Resolved |
| Title | nvarchar(max) | No | | Display title |
| PayloadJson | nvarchar(max) | No | | Extensible metadata, no secrets |
| DetectedAt | datetimeoffset | No | indexed | Detection/event time |
| AcknowledgedAt | datetimeoffset | Yes | | Future GD3 action time |
| ResolvedAt | datetimeoffset | Yes | | Future GD3 action time |

## AuditLog

| Column | SQL type | Null | Key | Description / rule |
|---|---|---:|---|---|
| Id | uniqueidentifier | No | PK | Audit event ID |
| TenantId | uniqueidentifier | No | indexed | Tenant partition |
| UserId | uniqueidentifier | Yes | logical FK AppUser | Acting identity |
| BranchId | uniqueidentifier | Yes | logical FK Branch | Effective/requested branch |
| Action | nvarchar(max) | No | | e.g. `reporting.query`, future alert actions |
| ResourceType | nvarchar(max) | No | | Audited resource category |
| ResourceId | nvarchar(max) | Yes | | Optional resource identifier |
| QueryId | nvarchar(max) | Yes | | Allow-listed query identifier |
| CorrelationId | nvarchar(max) | No | | Request trace link |
| MetadataJson | nvarchar(max) | No | | Sanitized structured metadata |
| OccurredAt | datetimeoffset | No | indexed | Audit event time |
