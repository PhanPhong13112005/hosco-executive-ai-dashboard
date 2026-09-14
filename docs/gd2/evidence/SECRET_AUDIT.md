# Secret and Environment Audit

A case-insensitive tracked-file scan covered password, secret, token, API-key, signing-key, connection-string, and private-key terms. Values were not copied into this evidence.

No production credential was identified. The following tracked development/demo material requires explicit review and must never be promoted as a production secret:

- Potential secret found at `.env.example:5`
- Potential secret found at `src/Hosco.Api/appsettings.json:11`
- Potential secret found at `src/Hosco.Infrastructure/Persistence/DemoSeed.cs:28`
- Potential secret found at `src/Hosco.Api/Hosco.Api.http:17`
- Potential secret found at `tests/Hosco.IntegrationTests/ReportingApiTests.cs:135`
- Potential secret found at `tests/Hosco.IntegrationTests/SeedDatasetTests.cs:13`
- Potential secret found at `README.md:73`
- Potential secret found at `README.md:84`

The SQL Server connection string uses LocalDB integrated security and contains no database password. Settings and README identify committed values as development-only placeholders and direct real local values to user-secrets/environment variables.

## Ignore controls

`.gitignore` was verified to block:

- `.env` and `.env.*` while allowing `.env.example`
- `appsettings.Local.json`
- `*.pfx` and `*.key`
- `secrets.json`
- all `bin/` and `obj/` directories

The reusable verifier reports only file and line locations, never matched values. It does not perform Git-history scanning or entropy-based secret detection; a dedicated CI secret scanner remains recommended.

Verdict: `PARTIALLY VERIFIED` because ignore/config controls exist and no production credential was identified, but a shared development demo credential is tracked in several files.

