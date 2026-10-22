# PostgreSQL Migration Plan

## Previous database provider

The repository started empty; there was no deployed SQL Server database or migration history to convert. The current implementation therefore starts as PostgreSQL-first rather than migrating an existing instance.

## Migration changes

- Target framework remains .NET 10.
- EF Core provider is configured with Npgsql.
- Database context is built with `UseNpgsql`.
- Money fields use `decimal(18,2)` or platform-appropriate values.
- Date and time fields are stored in UTC and mapped with `DateTime`/`DateTimeOffset` semantics consistent with app requirements.
- Identity values use GUIDs or PostgreSQL-native UUIDs where appropriate.

## PostgreSQL compatibility decisions

- `UseNpgsql` is the only active provider for production.
- SQL Server-specific features and conventions are intentionally avoided.
- Migrations are created using EF Core Design tooling with the PostgreSQL provider installed.
- Indexes and foreign-key delete behaviors are explicit and PostgreSQL-safe.

## Migration commands

```bash
dotnet ef migrations add Initial_Postgres --project src/Knect.Infrastructure --startup-project src/Knect.API

dotnet ef database update --project src/Knect.Infrastructure --startup-project src/Knect.API
```

## Risk assessment

Because no production database existed in the repo, there is no data migration risk from a legacy provider. The main risk is schema design and data semantics, not conversion.
