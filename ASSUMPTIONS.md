# Assumptions and Product Decisions

## Scope assumptions

- This repository is being created as a greenfield backend for the Knect recruitment platform.
- The business rules below are the default assumptions for a production-ready baseline and may require product-owner sign-off for final production rollout.
- The initial implementation focuses on a secure, extensible backend foundation rather than a complete feature-by-feature replacement of an existing product.

## Business decisions

- A company has one primary employer account, and the employer account manages the company profile and jobs.
- Subscription pricing is set by admins; employers submit payment evidence and admins verify manually.
- The job-post limit is enforced as the maximum number of non-deleted jobs allowed under the active subscription. Drafts are excluded from the count unless the business later specifies otherwise.
- Jobs are paused automatically when a subscription expires or total allowed jobs is exceeded.
- Admin sessions are single-active, with previous server-side sessions invalidated on new successful admin login.
- Refresh tokens rotate on refresh and are revoked on logout or security-sensitive account changes.
- Google authentication uses the stable Google subject identifier and verifies the email claim before linking accounts.
- OTP values are hashed and only used once; attempts and resend frequency are rate-limited.
- File storage uses S3-compatible object storage with private object access and presigned URLs for authorized consumers.

## Open questions and risks

- Whether company names are globally unique or only unique per regional/company-identity policy.
- Whether job posting limits include published jobs only or all non-deleted jobs.
- Whether employers can resubmit payment evidence after rejection without creating duplicate payment records.
- Whether jobs can be reactivated automatically after renewal or remain manual until admin approval.
- Whether candidate profiles may retain CV snapshots after deletion or cancellation.

These decisions should be confirmed by the system owner before production deployment.
