# API Endpoints

This document lists the implemented and planned versioned API endpoints for the Knect backend.

## Authentication

- `POST /api/v1/auth/register` — register a new job seeker or employer account.
  - Auth: none
  - Validates first/last name, email, password, and public role restrictions.

- `POST /api/v1/auth/login` — authenticate with email and password.
  - Auth: none
  - Returns access token and refresh token.

- `POST /api/v1/auth/refresh` — rotate refresh tokens.
  - Auth: none
  - Rejects reused, expired, or revoked refresh tokens.

- `POST /api/v1/auth/logout` — revoke refresh token and current admin session if applicable.
  - Auth: none

- `POST /api/v1/auth/google` — complete Google OIDC sign-in using a server-validated credential.
  - Auth: none
  - Requires verified email from the upstream JWT.

- `POST /api/v1/auth/otp/request` — request a six-digit email OTP.
  - Auth: none
  - Generic response to avoid account enumeration.

- `POST /api/v1/auth/otp/verify` — verify an email OTP.
  - Auth: none
  - Single-use and expiry enforced.

- `POST /api/v1/auth/password/reset` — reset a password after a valid OTP challenge.
  - Auth: none

- `GET /api/v1/auth/admin-check` — admin authorization check.
  - Auth: JWT required
  - Authorization: `AdminOnly`

## Profiles

- `GET /api/v1/users/me` — get the authenticated user's full profile.
  - Auth: JWT required
  - Returns the private view of the job seeker's current user record.

- `PUT /api/v1/users/me` — create or update the authenticated user's profile.
  - Auth: JWT required
  - Supports personal details, location, summary, skills, experience, education, and file metadata.

- `GET /api/v1/users/{userId}` — retrieve a public user profile.
  - Auth: JWT required
  - Public profile is intentionally limited to non-private attributes and does not expose CV contents or private metadata.

- `GET /api/v1/employers/me` — get the authenticated employer's company profile.
  - Auth: JWT required
  - Access is restricted to the owner's company profile.

- `PUT /api/v1/employers/me` — create or update the authenticated employer company profile.
  - Auth: JWT required
  - Enforces a one-company-per-user business rule via the unique `UserId` relationship.

- `GET /api/v1/employers/{userId}` — retrieve a public employer profile.
  - Auth: JWT required
  - Public view excludes private contact details unless the caller is the owner.

## File storage

- The platform uses a file-storage abstraction (`IFileStorageService`) with a memory-backed implementation for local development/testing.
- Production should replace the implementation with an S3-compatible provider and private-bucket configuration.
- CVs and profile/logo metadata remain private and are not publicly exposed unless the caller is the owner.

## Job management

### Admin reference data

- `GET /api/v1/admin/job-categories` — list active/inactive job categories.
- `POST /api/v1/admin/job-categories` — create a category.
- `PUT /api/v1/admin/job-categories/{categoryId}` — update a category.
- `DELETE /api/v1/admin/job-categories/{categoryId}` — delete a category.
- `GET /api/v1/admin/job-experiences` — list experience levels.
- `POST /api/v1/admin/job-experiences` — create an experience level.
- `PUT /api/v1/admin/job-experiences/{experienceId}` — update an experience level.
- `DELETE /api/v1/admin/job-experiences/{experienceId}` — delete an experience level.

### Employer job operations

- `GET /api/v1/employers/jobs` — list jobs owned by the authenticated employer.
- `POST /api/v1/employers/jobs` — create a job post.
- `GET /api/v1/employers/jobs/{jobId}` — retrieve a job owned by the employer.
- `PUT /api/v1/employers/jobs/{jobId}` — update a job post.
- `PATCH /api/v1/employers/jobs/{jobId}/status` — transition a job between valid statuses.
- `DELETE /api/v1/employers/jobs/{jobId}/draft` — delete a draft job only when status is `Draft`.

### Public job discovery

- `GET /api/v1/jobs` — search and paginate published jobs.
  - query parameters: `search`, `categoryId`, `experienceId`, `location`, `employmentType`, `minSalary`, `maxSalary`, `sort`, `page`, `pageSize`
- `GET /api/v1/jobs/{jobId}` — retrieve a published job's public details.

## Job applications

### Job seeker application operations

- `POST /api/v1/jobs/{jobId}/applications` — apply to a published job.
- `GET /api/v1/users/me/applications` — list the authenticated candidate's applications with pagination.
- `GET /api/v1/users/me/applications/{applicationId}` — retrieve a candidate's application details.
- `DELETE /api/v1/users/me/applications/{applicationId}` — withdraw a non-final application.

### Employer candidate management

- `GET /api/v1/employers/applications` — list candidate applications for employer-owned jobs.
- `GET /api/v1/employers/applications/{applicationId}` — retrieve candidate details for an employer-owned application.
- `PATCH /api/v1/employers/applications/{applicationId}/status` — move an application through permitted statuses.

## Subscriptions and manual payments

### Admin plan management

- `GET /api/v1/admin/subscription-plans` — list active plans or include inactive plans for admin review.
- `GET /api/v1/admin/subscription-plans/{planId}` — retrieve a plan by ID.
- `POST /api/v1/admin/subscription-plans` — create a subscription plan.
- `PUT /api/v1/admin/subscription-plans/{planId}` — update a plan's price, duration, currency, and limits.
- `PATCH /api/v1/admin/subscription-plans/{planId}/active` — activate or deactivate a plan.

### Employer subscription flow

- `GET /api/v1/employers/subscriptions/plans` — list currently active purchaseable plans.
- `GET /api/v1/employers/subscriptions` — list the authenticated employer's subscriptions.
- `POST /api/v1/employers/subscriptions` — submit a plan selection and manual payment reference for review.
- `GET /api/v1/employers/payments` — list payment submissions and statuses for the employer.

### Admin payment review

- `GET /api/v1/admin/payments/pending` — list pending manual payments with pagination.
- `GET /api/v1/admin/payments/{paymentId}` — retrieve a payment record and proof metadata.
- `PATCH /api/v1/admin/payments/{paymentId}/review` — approve or reject a pending payment with reason.

## Background lifecycle and notifications

- `GET /health/live` — liveness check.
- `GET /health/ready` — readiness check for the database and application dependencies.
- The subscription lifecycle worker runs as a hosted background service and checks for expired subscriptions and reminder windows at a configured interval.
- Reminder configuration is read from `BackgroundJobs:SubscriptionLifecycle` and defaults to a 5-minute polling interval, batch size 200, 7-day reminder, and 1-day final reminder.
- Notification records are deduplicated via an audit-log notification key and therefore do not re-send the same reminder on repeated scheduler passes.

## Health

- `GET /api/v1/health/live` — liveness check.
- `GET /api/v1/health/ready` — readiness check that includes dependency checks.

## Notes

- The application uses a single versioned API prefix (`/api/v1`).
- Authorization enforcement is server-side only; frontend hiding is not sufficient.
- Sensitive account operations return generic messages to reduce data leakage.
