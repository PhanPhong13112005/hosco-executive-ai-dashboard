# Database Verification

## Model and migration

Source inspection found exactly 16 application `DbSet`s/tables:

`Tenant`, `Branch`, `AppUser` (`Users` table), `Role`, `UserRole`, `UserBranch`, `Customer`, `Employee`, `Product`, `Inventory`, `Order`, `OrderItem`, `Payment`, `Refund`, `Alert`, and `AuditLog`.

The generated idempotent SQL contains 17 `CREATE TABLE` statements: 16 application tables plus EF Core's `__EFMigrationsHistory` table. It contains 26 foreign-key declarations.

| Check | Actual result |
|---|---|
| EF CLI | Entity Framework Core tools 10.0.11 |
| Migration artifact | `20260914161316_InitialCreate` found |
| Model drift | `No changes have been made to the model since the last migration.` |
| SQL generation | PASS; `sql/gd2-schema.sql` generated idempotently |
| SQL Server | LocalDB `MSSQLLocalDB` 17.0.4025.3 was running |
| Database apply | PASS; initial migration applied to `HoscoDev` |
| Post-apply list | Migration listed without `(Pending)` |

## Mapping evidence

- Every tenant-owned entity derives from `TenantEntity` and receives a restrictive `TenantId -> Tenants.Id` foreign key.
- Physical branch foreign keys exist for `Employee`, `Inventory`, `Order`, and `UserBranch`.
- `Order` has physical relations to branch, optional customer, and employee. `OrderItem` has relations to order and product; `Payment` and `Refund` relate to order.
- `OrderItem.UnitCostAtSale` is present and mapped as `decimal(18,2)` for historical cost snapshots.
- Money fields use precision 18,2.
- Verified important indexes include tenant/code, tenant/SKU, tenant/employee-code, tenant/branch/product inventory, tenant/order-number, tenant/branch/order-date, tenant/order-item, tenant/branch/refund-date, tenant/status/alert-date, and tenant/audit-date.

## Integrity limitation

`Refund.BranchId`, `Alert.BranchId`, and `AuditLog.BranchId` are columns/index inputs but do not have physical branch foreign keys. Existing separate tenant and branch foreign keys on other tables also do not form a composite constraint proving that both IDs belong to the same tenant. This is recorded in `DEFECTS_FOUND.md`; no model or migration was changed during this audit.

Verdict: `PARTIALLY VERIFIED` because the artifact, generation, and apply are verified, while branch referential integrity is incomplete.

