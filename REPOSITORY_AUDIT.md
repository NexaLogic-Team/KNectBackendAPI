# Repository Audit

## Summary

The repository was empty when the audit started: there was no `.csproj`, no existing ASP.NET solution, no legacy migration history, and no SQL Server-specific provider configuration to preserve. The working tree only contained a Visual Studio solution file and a `.gitignore` in the repository root. The environment has .NET SDK 10.0.302 installed, so the project target framework can safely stay on .NET 10.

## Findings

- Solution baseline: `KNectBackendAPI.sln` existed but no actual project entries beyond a blank solution shell.
- Runtime: .NET 10.0.302 SDK installed, runtime 10.0.10 present.
- Existing codebase: none; no `src/`, `tests/`, or `migrations/` directories existed before scaffolding.
- Database provider: no SQL Server or PostgreSQL code existed to migrate; therefore there is no deployed database to preserve.
- Technical risk: there is no legacy schema or seed data to recover. The safest and least risky path is a greenfield PostgreSQL-first implementation using EF Core + Npgsql.

## SQL Server Specific Dependencies

No SQL Server dependencies were found in the repository. There were no `UseSqlServer` calls, `Microsoft.EntityFrameworkCore.SqlServer` packages, SQL Server identity strategies, or SQL Server-only migrations. Because the project was effectively empty, there is no need for a destructive migration from SQL Server to Neon PostgreSQL.

## Recommended Migration Strategy

1. Treat the repository as a greenfield PostgreSQL-first project.
2. Keep the target framework at .NET 10.
3. Define all domain entities and EF Core mappings in a PostgreSQL-compatible manner.
4. Use `UseNpgsql` in the application DbContext and generate an initial migration using Npgsql providers.
5. Keep all date/time values UTC at the application layer and record any business dates as `DateOnly` or explicit local-date fields when needed.
6. Store money using `decimal` and restrict precision at the database level.
7. Use UUIDs for critical keys where a distributed system or externally referenced identifiers are likely.
8. Keep connection strings in environment variables and require TLS in production for Neon.

## Conclusion

Because no database had been deployed and no existing migration history was present, the safest transition is to create a fresh PostgreSQL-compatible schema rather than converting a legacy SQL Server migration. This avoids risking data loss and keeps the stack aligned with the stated production target.
