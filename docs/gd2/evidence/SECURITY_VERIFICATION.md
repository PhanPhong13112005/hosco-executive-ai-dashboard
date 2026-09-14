# Security Verification

## Implementation

- JWT validation checks issuer, audience, lifetime, signing key, and a 30-second clock skew.
- Issued claims are `sub`, name identifier, `tenant_id`, email, `jti`, one or more role claims, and zero or more `branch_id` claims.
- The reporting policy requires authentication and one of `Owner`, `BranchManager`, `ChainManager`, or `SystemAdmin`.
- `CurrentUser` is the claim-to-identity boundary. `ReportingScopeFactory` always sources tenant from the authenticated principal, never from a query parameter.
- `BranchScopeValidator` first verifies branch ownership by tenant, then verifies assigned-branch access unless the role is tenant-wide.
- Password verification uses PBKDF2-HMAC-SHA256 with 100,000 iterations and fixed-time comparison. SQL verification found 5/5 stored hashes in the encoded PBKDF2 format.

## Requirement-to-test traceability

| Requirement | Exact test evidence | Actual |
|---|---|---|
| Unauthenticated reporting request returns 401 | `ReportingApiTests.Reporting_without_login_returns_401` | Passed; manual SQL-backed request also returned 401 |
| Branch manager can read assigned branch | `BranchScopeTests.Branch_manager_can_access_assigned_branch`; `ReportingApiTests.Branch_manager_can_read_assigned_branch_only` | Passed |
| Branch outside assignment returns 403 | `BranchScopeTests.Branch_manager_cannot_access_unassigned_branch`; `ReportingApiTests.Branch_manager_is_forbidden_from_other_assigned_tenant_branch` | Passed; manual SQL-backed request returned 403 |
| Tenant-wide manager can read same-tenant branch | `BranchScopeTests.Chain_manager_can_access_any_branch_in_same_tenant`; `ReportingApiTests.Chain_manager_can_read_branch_in_own_tenant` | Passed |
| Cross-tenant branch is blocked | `BranchScopeTests.Cross_tenant_branch_is_always_forbidden`; `ReportingApiTests.Cross_tenant_branch_is_forbidden` | Passed |
| Result data never crosses tenant | `ReportingApiTests.Reporting_never_leaks_other_tenant_data` | Passed |
| Client cannot override tenant claim | `ReportingApiTests.Client_tenantId_query_parameter_cannot_override_claim` | Passed |
| Login and authenticated reporting work | Authenticated integration tests plus manual SQL-backed login/order request | Login 200; order request 200 |

No token or password value is stored in this evidence. The manual token was kept only in process memory.

Verdict: `VERIFIED` for the tested authentication and tenant/branch isolation paths.

