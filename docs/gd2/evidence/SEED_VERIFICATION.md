# Seed Verification

## Determinism and time range

`DemoSeedIds.Id` derives GUID bytes from SHA-256 of stable labels. The generator uses a fixed UTC start and loops 181 days, producing order timestamps from 2026-01-01 08:00 UTC through 2026-06-30 14:00 UTC. IDs, dates, products, users, and anomaly rules are therefore deterministic for an empty database.

The seed returns immediately when any tenant already exists. This prevents duplicate seeding but does not repair a partially seeded database.

## SQL Server counts after migration and seed

| Entity | Count |
|---|---:|
| Tenants | 2 |
| Branches | 4 |
| Users | 5 |
| Roles | 4 |
| Customers | 32 |
| Employees | 12 |
| Products | 16 |
| Inventories | 32 |
| Orders | 2,084 |
| OrderItems | 4,168 |
| Payments | 2,084 |
| Refunds | 70 |
| Alerts | 4 |

Each tenant has 1,042 orders across two branches. A later API verification request created one audit row; `DemoSeed` itself does not insert audit rows.

## Anomaly and isolation fixtures

Four explicit Tenant-A alert fixtures exist and were queried from SQL Server: `abnormal-discount`, `cancellation-spike`, `low-stock`, and `revenue-drop`. The generator also changes order frequency, cancellation status, discounts, and low-stock quantities in the corresponding windows.

Tenant B, its two branches, owner, products, customers, employees, inventory, and commerce records provide real cross-tenant isolation fixtures.

All five stored password values matched the expected PBKDF2-SHA256 encoded format; no stored value was printed during verification.

Verdict: `VERIFIED` for an empty database and the audited seed implementation; partial-database recovery is a known limitation.

