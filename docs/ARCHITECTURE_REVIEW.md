# Stage 4 architecture, security and reliability review

Reviewed on **27 October 2026** against the implemented C# source, migrations, Razor pages, 191 accepted tests, the read-only Java architecture review, and the original assignment. The final suite has 197 passing cases. This is a focused local classroom application review, not an independent production security certification.

## Findings and changes

| Finding | Consequence | Targeted correction and evidence |
| --- | --- | --- |
| decimal.TryParse can round very long fractional text before exact-kuruş validation. | A fee such as `1,000000000000000000000000000001` could be accepted as 1 TRY. | TryFeeModelBinder checks the number of fractional digits before conversion. Three new regression cases failed against the checkpoint and pass after the fix, including zero and maximum-boundary values. Domain/integer storage remains unchanged. |
| Management validation replaced all errors with one generic paragraph. Some default attributes were English. | Users could not identify the missing/invalid field. | Turkish attribute messages, explicit specialty/job-title shape errors, distinct safe messages, credential-safe handling and a recovery link to the relevant management section. New real-form test checks useful messages and no password echo/account creation. |
| Patient selectors submitted immediately on change. | Arrow-key selection unexpectedly navigated and changed focus. | Explicit update button with instructions; no onchange navigation. Browser rehearsal verifies arrow selection stays on the page until Enter activates the update button. No JavaScript dependency added. |
| Role pages still used foundation-stage copy; statuses and narrow tables lacked consistent treatment. | Less clear workflow and keyboard/mobile use. | Actual use-case shortcuts, textual status badges, current management section, role-appropriate return links, focusable horizontally scrollable tables, table header scope and narrow-width wrapping. |
| Conflict-page selection had only partial historical parity evidence. | Offering/service/branch/date recovery was not fully established. | New real competing-booking HTTP regression proves a 409 retains all four selectors and date and saves no extra appointment. J068 is now resolved in TEST_PARITY.md. |

No mandatory missing application feature or defect requiring an architectural rewrite was found. No new package, domain table or migration was introduced. The SQLite transaction adapter and all application scheduling/security use cases are unchanged.

## Layer and OOP review

| Concept / area | Exact implementation and assessment |
| --- | --- |
| Encapsulation | Domain entities expose private setters; Appointment.Approve/Reject validate the transition/audit before mutation; HealthcareService.UpdateDetails validates the whole edit; AvailabilityWindow.Reschedule keeps employee/branch links. Failed domain mutations are tested. Cross-object invariants belong to services. |
| Inheritance | Abstract StaffMember contains genuinely shared employment, identity and activation state. Sealed Doctor requires Specialty; sealed HealthcareStaff has JobTitle. EF TPH mapping is external to Domain. |
| Abstraction | StaffMember.ProfessionalDescription is an abstract contract. Focused repository/security/transaction ports describe useful boundaries; AccountProfile carries no credentials. |
| Polymorphism / substitution | Doctor returns specialty, HealthcareStaff returns job title. BookingPageService, AppointmentService and ManagementQueryService call the shared property. Non-doctor and doctor bookings use the same flow. Editors appropriately distinguish subtype-specific fields, without a booking type switch. |
| Single responsibility | Controllers adapt HTTP/form shape. Application services own use cases, grants, scheduling and transactions. Repositories query/persist. Domain owns local invariants; Web presentation helpers format Turkish labels. |
| Open/closed | Shared booking can use either concrete subtype without changing its algorithm. Adding a genuinely new subtype would still require persistence/UI mapping; no exaggerated zero-change claim. |
| Interface segregation | Separate catalog, schedule, appointment, access and management ports; no universal CRUD service or interface per concrete use case. |
| Dependency inversion / DI | Application references Domain only; Infrastructure implements Application ports; Web is the composition root. Constructor injection supplies repositories, transaction/auth adapters, ClinicTime and TimeProvider. Controllers do not use DbContext/UserManager/repositories. |
| Composition | Roles are composed onto Identity accounts; grants link managers/facilities. StaffOffering links employee/service and derives branch. Availability is separate from qualification. No artificial Person/Patient/Manager inheritance tree. |
| Domain independence | No ASP.NET Core/EF package/project references. Assembly-reference test verifies the boundary. One domain graph is mapped by external Fluent API, without duplicate persistence entities. |
| Persistence | One IdentityDbContext, TPH staff, restrictive domain FKs, unique offerings/grants and scoped names, UTC ticks and exact integer minor units; three real migrations. No lazy-loading dependency in Razor. |

## Security and reliability review

- Identity uses framework hashing, normalized/bounded usernames, five-failure/five-minute lockout and protected cookies. Disabled users change the security stamp; existing cookie invalidation is tested. New STAFF credentials cannot take over an existing user or grant submitted roles. Public role/account administration is absent.
- Route role policies are complemented by current account/role and patient/staff/facility checks in services. ManagementScope checks the grant before waiting, and again inside the writer. Child IDs are freshly loaded and checked against the requested facility. Role revocation with a previously issued cookie is now an HTTP/direct-service regression.
- Unsafe MVC actions use automatic antiforgery validation. Login/logout and decisions have real-token/cookie tests; every management mutation has a no-token denial check. Return URLs are local. Razor encodes supplied names. Passwords/hashes are absent from read DTOs and error output. Model inputs do not bind domain entities or authoritative fee/actor/status values.
- Booking, decisions and management use the **unchanged native immediate writer** on the scoped EF connection. Reads/rechecks follow acquisition; rollback/disposal and bounded contention remain. Real separate-connection races and two-process booking tests pass. SQLite WAL permits readers, not concurrent writers; no unique-start/process-mutex claim is made.
- The same complete-interval algorithm serves candidate display and submission. It merges adjacent coverage without bridging gaps; conflicts span all branches for an employee and the whole application for a patient. Pending/Approved reserve; Rejected releases. Decisions transition once with consistent audit.
- Availability edits preserve future/ongoing reservations using booked duration and remaining coverage. Opening-hour edits also preserve future unbooked shifts. Deactivation prevents new reservations, retaining historical rows and decision access. Catalog changes bump a version and preserve existing snapshots.
- Explicit Demo flags control registration of idempotent synthetic seeders. Stable seed marker survives renames; restart never moves history. Non-demo absence and file persistence are tested. Database and generated outputs remain outside wwwroot and source control.

## Rehearsal and limitations

See STAGE4_VERIFICATION.md for actual restore/build/test/model/publish commands, released-process restart evidence and browser widths/keyboard observations. The Windows guide distinguishes source builds, framework-dependent publish files, local certificate setup, deliberate demo reset and external course obligations.

Deliberate limits remain: local single-writer SQLite, one timezone/currency, same-day windows, no holiday/travel/room scheduling, no automatic Pending expiry/cancellation/rescheduling, no clinical records/payments, no public signup/recovery/grant editor. Staff login creation occurs at registration. Immutable ownership links are retained; deactivation replaces destructive deletion. Safe configuration edits use last-valid-write behavior rather than a stale-form merge UI. Facility/branch/professional labels are current links; service/staff names and commercial terms are snapshots. Failed management entries are not automatically restored. A full screen-reader certification or independent second-computer deployment is not claimed.
