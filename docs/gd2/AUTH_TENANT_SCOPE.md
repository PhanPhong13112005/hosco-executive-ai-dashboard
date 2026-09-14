# Authentication, Tenant and Branch Scope

## Claims

Development JWTs contain:

| Claim | Meaning |
|---|---|
| `sub` / NameIdentifier | immutable user ID |
| `role` | one or more `Owner`, `BranchManager`, `ChainManager`, `SystemAdmin` |
| `tenant_id` | immutable tenant selected from the stored identity |
| `branch_id` | zero or more assigned branch IDs |

`TenantId` is deliberately absent from every reporting filter contract. An unknown query parameter named `tenantId` is ignored by model binding and cannot affect the scope derived from JWT claims.

## Rules

| Actor | Effective scope |
|---|---|
| Owner | all active branches in own tenant |
| Branch Manager | assigned branches only; omission of `branchId` remains restricted |
| Chain Manager | all active branches in own tenant |
| System Admin | technical role, still constrained to its own tenant |

For an explicit `branchId`, `BranchScopeValidator` first verifies tenant ownership and then role/assignment. Foreign tenant and unassigned branch requests return 403. Every reporting query independently includes `TenantId == currentUser.TenantId` and the resolved branch predicate.

## Evidence

Unit tests cover scope policy. HTTP integration tests cover 401, assigned branch, unassigned branch, cross-tenant branch, chain manager, omitted-branch tenant isolation, and attempted `tenantId` override.
