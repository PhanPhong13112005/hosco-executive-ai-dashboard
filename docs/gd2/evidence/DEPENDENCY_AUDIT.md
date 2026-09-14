# Dependency Audit

Audit date: 2026-09-15. NuGet sources used were `https://api.nuget.org/v3/index.json` and the installed Microsoft SDK package source.

## Vulnerabilities

`dotnet list Hosco.slnx package --vulnerable --include-transitive --no-restore` exited 0. All six projects reported no vulnerable packages given the current sources.

Status: `VERIFIED` at the time of the query. Advisory results are time- and feed-dependent.

## Outdated packages

`dotnet list Hosco.slnx package --outdated --include-transitive --no-restore` exited 0 and reported update candidates. No package was updated.

Top-level candidates:

| Project | Packages |
|---|---|
| `Hosco.Api` | `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.11 -> 10.0.12; `Microsoft.EntityFrameworkCore.Design` 10.0.11 -> 10.0.12 |
| `Hosco.Infrastructure` | EF Core Design/InMemory/Sqlite/SqlServer 10.0.11 -> 10.0.12; `System.Security.Cryptography.Xml` 10.0.11 -> 10.0.12 |
| `Hosco.UnitTests` | `coverlet.collector` 6.0.4 -> 10.0.1; `Microsoft.NET.Test.Sdk` 17.14.1 -> 18.10.0; `xunit.runner.visualstudio` 3.1.4 -> 4.0.0 |
| `Hosco.IntegrationTests` | Same three test-tool candidates as unit tests |

The command also listed transitive updates including Azure.Identity/Core, Microsoft.Data.SqlClient, EF Core, IdentityModel, Roslyn/MSBuild, SQLitePCLRaw, and test-platform packages. Major-version differences are not an instruction to update; compatibility and release notes require separate review.

