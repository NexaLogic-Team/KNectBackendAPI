# Security Guide

## Secrets and configuration

- Never commit live secrets or private configuration to the repository.
- JWT signing keys, Google OAuth client IDs/secrets, email credentials, and database passwords must be sourced from environment variables or a secrets manager.
- The current startup validation rejects placeholder values such as `replace-me` or `replace-with-a-long-random-development-key`.

## Token policies

- JWT access tokens are short-lived and signed with the configured HMAC secret.
- Refresh tokens are hashed before persistence and rotated on each successful refresh.
- Reused, expired, revoked, and invalid refresh tokens are rejected and logged as replay/reuse attempts.
- Admin session identifiers must match the current server-side `AdminSession` value for protected admin endpoints.

## Admin sessions

Admin sessions are single-active. A new successful admin login invalidates older active admin sessions server-side and requires the JWT `sid` claim to match the current DB record. Requests lacking a valid current admin session are denied.

## OTP and reset flows

- OTP values are generated with a secure random source and stored as a server-side hash.
- OTPs expire quickly, are single-use, and enforce attempt limits.
- Resend requests are throttled to prevent abuse.
- Password-reset flows reuse the same OTP-style validation with expiry and replay protection.

## Google OAuth

- Google sign-in is only accepted when the provided credential is validated server-side against the configured Google client ID.
- The server does not trust client-supplied email or subject values without verification.
- Google OAuth is not enabled unless the environment is configured with a valid client ID.

## Logging and sensitive data

- Do not log raw passwords, refresh tokens, access tokens, OTP values, or password hashes.
- Email-sending logic should redact any recipient details in production logs.
- Security-sensitive actions such as login, refresh, password reset, and admin session changes should remain auditable without exposing secrets.

## Database safety

- PostgreSQL is the only active production database path in this repository.
- The migration chain is PostgreSQL-compatible and no destructive database resets are performed.
- Real PostgreSQL-backed integration tests are expected to use a dedicated test database connection string and must not connect to production data.

## Job management protections

- Job categories and experience levels are admin-only administrative reference data.
- Public job discovery only exposes jobs already published and not deleted.
- Employer operations require the authenticated employer role and ownership validation against the authenticated user’s employer profile.
- Draft jobs are the only jobs permitted to be deleted; published or archived jobs cannot be deleted via the employer API.
- Job status transitions are validated through `Job.IsTransitionAllowed`; invalid transitions are rejected.
- Search inputs and pagination are validated to prevent oversized lists and unsafe query patterns.

## Job application protections

- Candidate applications derive the applicant ID from trusted JWT claims; client-supplied applicant IDs are ignored.
- Applications are rejected for unpublished, closed, expired, or deleted jobs, and duplicate applications are prevented at the service layer and via the database unique-index design.
- Submitted CV references must match the authenticated applicant’s stored CV key and cannot be used from another user’s file metadata.
- Employers can only access applications for jobs owned by their own company profile.
- Application status transitions reject invalid changes using `JobApplication.IsTransitionAllowed`.
- Candidate withdrawals are limited to non-final states so a job seeker cannot reverse accepted or rejected outcomes.

## Subscription and payment protections

- Subscription plans are admin-managed and only active plans are shown in employer purchase flows.
- Payment amounts, plan limits, and employer ownership are derived from trusted server-side records, never from client-supplied values.
- Manual payment submissions require a valid employer identity and a linked subscription plan.
- Review endpoints are restricted to administrators and only pending payments are mutable.
- Payment approval/rejection and subscription activation are executed in a single EF Core transaction to avoid partial state changes.
- Rejected payments do not activate a subscription, and confirmed payments require a valid subscription record before activation.
- Payment proof metadata remains private and is not treated as public content by default.
- Subscription reminders are deduplicated by notification key, and the scheduler does not resend the same reminder on repeated intervals under normal operation.
- Job publication eligibility checks reject inactive or expired subscriptions and enforce the active plan’s `MaxJobPosts` cap consistently in both API and background-worker flows.
