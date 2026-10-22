# Knect Backend API

## Overview

Knect is a job recruitment platform backend built with ASP.NET Core, PostgreSQL, and Docker. The repository is PostgreSQL-first and includes the completed authentication, profile, job-management, application, and subscription/payment foundations for the current scope.

## Architecture

- `src/Knect.Domain`: domain entities, enums, and shared base types.
- `src/Knect.Application`: DTOs, interfaces, validators, and application contracts.
- `src/Knect.Infrastructure`: EF Core persistence, security logic, profile services, and file-storage abstraction.
- `src/Knect.API`: Web API startup, controllers, and authorization policy enforcement.
- `tests`: unit and integration tests.

## Prerequisites

- .NET 10 SDK
- PostgreSQL 16 or Neon PostgreSQL access for real DB-backed testing
- Docker support for local deployment validation
- Valid runtime secrets for JWT, Google, and mail configuration

## Local development

```bash
dotnet restore

dotnet build --nologo -p:NuGetAudit=false -v minimal

dotnet test --nologo -p:NuGetAudit=false --verbosity minimal
```

## Required environment variables

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Database=knect;Username=postgres;Password=postgres;Port=5432;SSL Mode=Require"
export KNECT_TEST_CONNECTION_STRING="Host=localhost;Database=knect_test;Username=postgres;Password=postgres;Port=5432;SSL Mode=Disable"
export Jwt__Issuer="https://api.example.com"
export Jwt__Audience="knect-api"
export Jwt__Key="replace-with-a-long-random-secret-key"
export Google__ClientId="replace-with-google-client-id"
```

## Implemented endpoints

### Authentication

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/logout`
- `POST /api/v1/auth/otp/request`
- `POST /api/v1/auth/otp/verify`
- `POST /api/v1/auth/password/reset`
- `POST /api/v1/auth/google`
- `GET /api/v1/auth/admin-check` (AdminOnly)

### Profiles

- `GET /api/v1/users/me`
- `PUT /api/v1/users/me`
- `GET /api/v1/users/{userId}`
- `GET /api/v1/employers/me`
- `PUT /api/v1/employers/me`
- `GET /api/v1/employers/{userId}`

### Job management

- `GET /api/v1/admin/job-categories`
- `POST /api/v1/admin/job-categories`
- `PUT /api/v1/admin/job-categories/{categoryId}`
- `DELETE /api/v1/admin/job-categories/{categoryId}`
- `GET /api/v1/admin/job-experiences`
- `POST /api/v1/admin/job-experiences`
- `PUT /api/v1/admin/job-experiences/{experienceId}`
- `DELETE /api/v1/admin/job-experiences/{experienceId}`
- `GET /api/v1/employers/jobs`
- `POST /api/v1/employers/jobs`
- `GET /api/v1/employers/jobs/{jobId}`
- `PUT /api/v1/employers/jobs/{jobId}`
- `PATCH /api/v1/employers/jobs/{jobId}/status`
- `DELETE /api/v1/employers/jobs/{jobId}/draft`
- `GET /api/v1/jobs`
- `GET /api/v1/jobs/{jobId}`

### Job applications

- `POST /api/v1/jobs/{jobId}/applications`
- `GET /api/v1/users/me/applications`
- `GET /api/v1/users/me/applications/{applicationId}`
- `DELETE /api/v1/users/me/applications/{applicationId}`
- `GET /api/v1/employers/applications`
- `GET /api/v1/employers/applications/{applicationId}`
- `PATCH /api/v1/employers/applications/{applicationId}/status`

### Subscriptions and manual payments

- `GET /api/v1/admin/subscription-plans`
- `GET /api/v1/admin/subscription-plans/{planId}`
- `POST /api/v1/admin/subscription-plans`
- `PUT /api/v1/admin/subscription-plans/{planId}`
- `PATCH /api/v1/admin/subscription-plans/{planId}/active`
- `GET /api/v1/employers/subscriptions/plans`
- `GET /api/v1/employers/subscriptions`
- `POST /api/v1/employers/subscriptions`
- `GET /api/v1/employers/payments`
- `GET /api/v1/admin/payments/pending`
- `GET /api/v1/admin/payments/{paymentId}`
- `PATCH /api/v1/admin/payments/{paymentId}/review`

## Profile layer details

- Job seeker profiles support personal information, location, professional summary, skills, work experience, education, and file metadata.
- Employer profiles support company details, business metadata, address, and website/logo information.
- Current-user operations use the authenticated user ID from trusted JWT claims.
- Public profile reads are intentionally limited and private fields remain protected.

## File storage

- `IFileStorageService` is defined in the application layer.
- A memory-backed implementation is used in local development and tests.
- Production should replace this with an S3-compatible provider and private bucket setup.
- CVs are not publicly exposed by default; access is restricted to the current owner or authorized flows.

## Security baseline

- Passwords are hashed with ASP.NET Core `PasswordHasher`.
- JWTs are validated for issuer, audience, expiry, and signature.
- Refresh tokens are hashed and rotated; replay/reuse is rejected.
- OTPs are stored as hashes and enforce expiry, attempt limits, and single-use behavior.
- Admin sessions are enforced via the JWT `sid` claim and a server-side `AdminSession` check.
- Placeholder configuration value strings are rejected at startup.

## Background jobs and lifecycle automation

- A lightweight .NET `BackgroundService` is registered in the API startup and processes subscription expiry and reminder checks on a configurable interval.
- The worker is disabled by setting `BackgroundJobs:SubscriptionLifecycle:Enabled=false` and can be tuned via interval, batch size, reminder windows, and retry settings.
- Expired active subscriptions are transitioned to `Expired`, their jobs are closed, and the employer is sent a subscription-expired notification.
- Active subscriptions are compared against the configured reminder windows before expiry and deduplicated by a notification key to avoid repeat reminders during repeated scheduler passes.
- The publish endpoint and background policy share the same subscription-eligibility rules: a subscription must be active and not expired, and the employer must not exceed `MaxJobPosts`.

## Running the API

```bash
dotnet run --project src/Knect.API/Knect.API.csproj
```

## Running tests

```bash
dotnet test --nologo -p:NuGetAudit=false --verbosity minimal
```

## PostgreSQL integration status

Real PostgreSQL-backed auth/profile tests are implemented for dedicated environments using `KNECT_TEST_CONNECTION_STRING` or `ConnectionStrings__TestConnection`. The current environment did not provide a dedicated PostgreSQL test database, so DB-backed verification remains environment-gated and is not claimed as passed here.

## Production posture

This implementation is PostgreSQL-first and has startup validation for required secrets. Google OAuth, SMTP, and S3 storage are not considered production-ready until real environment values are configured and externally validated.
