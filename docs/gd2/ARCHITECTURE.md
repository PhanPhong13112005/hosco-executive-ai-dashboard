# GD2 Architecture

## Component view

```mermaid
flowchart TD
    Web[Web / Dashboard - GD3] --> API[Reporting API v1]
    Bot[Future Chatbot - GD4] --> API
    API --> Auth[JWT + RBAC + Tenant/Branch Scope]
    API --> Semantic[Semantic Layer / Metric Catalog]
    API --> Catalog[Allow-list Query Catalog]
    Semantic --> Store[Parameterized EF Core Reporting Store]
    Catalog --> Store
    Store --> DB[(SQL Server)]
    API --> Audit[(AuditLog)]

    Bot -. prohibited .-> NoDB[No direct database access]
    NoDB -.x DB
```

## Dependency direction

`Hosco.Api -> Hosco.Application -> Hosco.Domain`; `Hosco.Infrastructure` implements Application abstractions and depends on Domain/Application. Domain has no framework dependency.

## Request flow

1. Correlation middleware validates/reuses `X-Correlation-ID` or generates one.
2. JWT middleware authenticates and policy authorization validates a reporting role.
3. `CurrentUser` exposes `UserId`, `TenantId`, roles, and branch claims in one place.
4. `ReportingScopeFactory` rejects a foreign branch and constrains branch managers even when `branchId` is omitted.
5. A versioned, allow-listed reporting handler executes an EF Core query with mandatory tenant and branch predicates.
6. Responses include query/correlation/last-updated metadata; sensitive reporting queries can create an `AuditLog`.

## Extension boundaries

- Add approved KPI handlers behind `IReportingDataStore` and update the catalogs only after BA approval.
- GD3 Alert Engine can consume the same catalog and persist `Alert`; acknowledge/resolve can use `IAuditWriter`.
- GD4 AI Orchestrator receives a token and calls `/api/v1/reporting/...`; it cannot receive a database connection string or arbitrary SQL capability.
- External services can add retry policies at their typed HTTP client boundary; current SQL retry is capped at three attempts.
