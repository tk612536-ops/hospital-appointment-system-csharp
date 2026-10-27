# Stage 3 — Hospital/clinic management verification

Verified on **26 October 2026**, Europe/Istanbul. Stages 0–2 were accepted; the owner authorized Stage 3 only. **Stage 3 is implemented and ready for its checkpoint. No commit or push was made. Stage 4 was not started.**

The professor's C# instruction remains binding. The separate Java reference was read only; its application/build/tests were not run. The original assignment DOCX still hashes to `E636C9E18DC27A995A69E7C59800C3CB7506FB17EDC0884F5D563D6F67593226`.

## Implemented behavior

- Managers create a hospital or clinic together with its first branch and their own grant in one transaction. They can rename/change its type, add/rename branches and add/rename branch departments. One branch and multiple branches use the same model.
- Services are created in an owned department/branch. Managers edit names, whole-minute duration and exact TRY price, and deactivate/reactivate services. Domain validation rejects invalid values atomically. Turkish fee inputs accept comma decimals without thousands grouping; dot/grouped/scientific values and fractional kuruş are rejected. SQLite still stores integer minor units, never floating-point fees.
- Both doctors and non-doctor healthcare personnel can be registered/edited. Doctors have a primary specialty; other workers have a job title. Specialties form a shared immutable dictionary; matching names are reused. Staff/service offerings define qualification and branch assignment; duplicate assignment reuses/reactivates the existing offering. Managers can deactivate/reactivate staff and offerings. Reactivating an offering requires active staff and service.
- Staff registration optionally creates a **new STAFF-only Identity account**. Both credentials may be omitted. A username/password pair is required when used; existing accounts cannot be attached or elevated. User, role and domain profile are saved on the same scoped context/transaction. Profile failure after an Identity save rolls back the account and role too. Passwords are hashed by Identity and are never returned in read DTOs/views.
- Branch opening hours support multiple same-day periods, lunch gaps, adjacency and closed weekdays. Replacement affects only the selected branch/weekday. Future unbooked shifts are protected as well as future/ongoing reserved appointments.
- Managers create dated, strictly future staff availability, edit its date/interval, or safely remove it. Full opening coverage and an active offering at that branch are required. A staff member cannot have overlapping shifts at any branch. Staff/branch links remain immutable.
- Shrink, move and removal preserve every future/ongoing Pending or Approved interval using its **booked duration** and the union of remaining matching shifts. Rejected and completed appointments do not freeze the calendar. Deactivation blocks new bookings while preserving rows, snapshots and authorized approval/rejection of existing appointments.

Turkish Razor/Bootstrap pages are available at `/manager/facilities` (also `/manager/config`), `/manager/facilities/{id}`, and its `/services`, `/staff`, `/schedule` pages. All displayed create/edit/activate/deactivate/remove actions have functioning POST handlers. Hours use a branch/weekday selector; availability is grouped by date using native details elements. Shared navigation, labels, responsive columns, local assets and styled errors match the existing application. Homepage/login/workspaces now describe implemented management features.

## Architecture and OOP evidence

| Layer | Stage 3 classes/files |
| --- | --- |
| Domain | Existing StaffMember/Doctor/HealthcareStaff and facility/catalog graph retained. HealthcareService.UpdateDetails validates a whole edit before mutation; AvailabilityWindow.Reschedule preserves resource links. No EF/ASP.NET dependency. |
| Application | ManagementScope; FacilityManagementService, CatalogManagementService, StaffManagementService, ScheduleManagementService; ManagementQueryService; management commands/read records, repository ports and IStaffAccountProvisioner. |
| Infrastructure | FacilityManagementRepository, CatalogManagementRepository, StaffManagementRepository, ScheduleManagementRepository, ManagementReadRepository; StaffAccountProvisioner using UserManager; DI registrations. |
| Web | FacilityManagementController, CatalogManagementController, StaffManagementController, ScheduleManagementController; ManagementInputs, TryFeeModelBinder, ManagementFeedback; management/shared Razor Views and CSS/navigation updates. |
| Tests | ManagementDomainTests, ManagementScopeTests, ManagementIntegrationTests, ManagementScheduleTests, ManagementConcurrencyTests, ManagementHttpTests; reusable file-backed scenarios. |

Controllers handle HTTP/input shape and redirects/views. Application services coordinate ownership, scheduling, domain behavior and transactions. Repository implementations contain EF queries and persistence. Identity is behind a provisioning port. Concrete services use constructor injection rather than introducing an interface per class or a generic CRUD framework.

Encapsulation is visible in private state, named mutations, atomic invalid edits and immutable ownership links. Inheritance/abstraction use the real abstract StaffMember contract. Doctor and HealthcareStaff override ProfessionalDescription; mixed management lists, patient choices and details use the shared contract. Subtype-specific editors handle real specialty/job-title fields; common booking has no staff-type switch. Focused services/ports demonstrate single responsibility, interface segregation and dependency inversion. Both staff types remain substitutable in booking. Composition supplies facility grants, Identity roles and staff offerings.

Booked service/staff names, duration, fee/currency and instants remain immutable snapshots. **Facility/branch names and professional descriptions are current relational labels**, as in the approved model; Stage 3 does not add further snapshot columns or silently imply they were historically captured.

## Authorization and transaction behavior

All management services, including queries, require the current enabled MANAGER account. For existing facilities, ManagementScope checks the grant before waiting and again after acquiring the writer. Child resources are then freshly loaded and their actual facility is compared to the requested facility. This rejects forged branch/department/service/staff/offering/availability IDs even when the manager owns the URL's facility. Patients/staff/unrelated managers cannot bypass the service by calling it directly.

MVC role policies supplement these checks. All unsafe actions use global antiforgery validation. New account role/identity attachment inputs are not accepted; forged role/account fields are ignored and the resulting new user has STAFF only. Razor encodes manager-entered text. Known validation/conflict/busy errors show Turkish messages without passwords, SQL, stack traces or submitted sensitive values. Busy returns 503/Retry-After; safe configuration conflicts return 409.

**The Stage 2 SQLite protocol is preserved.** Management writes call the same SqliteWriteTransaction: clear stale tracked reads, acquire `BeginTransaction(deferred: false)` before mutable reads, enlist EF on that connection, reauthorize/revalidate, save and commit. No process lock, new locking mechanism or unique-start shortcut was introduced. The only adapter extension translates SQLite constraint failures during SaveAsync into a safe application conflict. Uncommitted writes still roll back on disposal.

No new package or migration was needed. The existing InitialIdentity, BookingDomain and ExactMinorUnits migrations already contain the approved management relationships. `has-pending-model-changes` reports no model changes; startup/tests apply those real migrations. Stage 2's provider table-rebuild warning remains documented in its historical report.

## Commands and exact test results

Installed SDK: **10.0.403**. Existing EF Core/Identity/SQLite packages remain **10.0.14** with unchanged locked dependency versions.

Executed from the solution root:

```powershell
dotnet restore HospitalAppointments.sln --locked-mode
dotnet build HospitalAppointments.sln --no-restore
dotnet test HospitalAppointments.sln --no-restore --logger 'trx;LogFilePrefix=stage3-verified' --results-directory TestResults
dotnet ef migrations has-pending-model-changes --no-build --project src/HospitalAppointments.Infrastructure --startup-project src/HospitalAppointments.Web
git diff --check
```

Restore succeeded. Build succeeded with **0 warnings and 0 errors**. Model check: **No changes have been made to the model since the last migration.** Final full-suite result:

| Project | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| HospitalAppointments.Domain.Tests | 31 | 0 | 0 |
| HospitalAppointments.Application.Tests | 26 | 0 | 0 |
| HospitalAppointments.IntegrationTests | 134 | 0 | 0 |
| **Total** | **191** | **0** | **0** |

All **113 Stage 1/2 cases** remain; **78 new Stage 3 cases** were added. The homepage assertion now expects implemented management copy; the two previously unmapped manager-route inputs now test deferred payment/medical-record routes. No tests were removed/skipped. Development runs caught and corrected a Razor DTO-label mismatch and stale test expectations (homepage text, denial-route spelling, and treating current facility labels as snapshots); the counts above are the final executed results, not those intermediate runs.

Ignored evidence under TestResults: `stage3-restore.log`, `stage3-verified-build.log`, `stage3-verified-test.log`, and `stage3-verified_net10.0_20261026021352.trx`, `...021353.trx`, `...021435.trx`. These are local outputs, not version-controlled artifacts. TEST_PARITY.md contains the 118-row historical Java inventory and actual C# destinations. All 28 management rows are mapped; a full rejected-booking selection audit remains explicitly partial for the later final review. No claim that Java's 118 tests ran in C# is made.

## Concurrency and regression evidence

Ten new race cases run real application services in independent scopes/contexts/physical SQLite connections against the same migrated file, synchronized before writer acquisition. Fresh verification reads assert opening/shift coverage, no cross-branch employee shift overlap, no patient/employee appointment overlap and coherent booked terms:

- Booking versus incompatible availability shrink: exactly one command succeeds.
- Booking versus safe/unsafe opening-hour replacement: compatible operations both succeed; unsafe hours are rejected because the existing future shift is protected even before booking.
- Booking versus service deactivation: deactivation commits; a preceding booking is retained, while a later booking is rejected.
- Catalog edit versus booking: either the old coherent snapshot is saved, or the stale displayed version is rejected.
- Overlapping shift creation within one branch and across branches: one winner in each case.
- Hours replacement versus new incompatible availability: one winner, valid final schedule.
- Competing valid edits of one shift: both serialize and preserve the reservation; the last valid edit is stored.
- Competing staff registrations with one username: one account/profile, STAFF only; no takeover/duplicate.

Existing separate-connection booking/decision tests and **two real Kestrel processes sharing one SQLite file** still pass. New tests additionally cover manager denial before writer acquisition and account/grant revocation while waiting, invalid child relationships, full lunch-gap coverage, adjacency, future/ongoing Pending/Approved protection, Rejected release, snapshots after service edits, subtype safety, exact storage, optional-account rollback, strict Turkish fees, all manager POST antiforgery restrictions, role/foreign-facility HTTP denial, Razor encoding, and configuration 503/no partial account followed by a successful fresh retry.

## Verified workflows and persistence

The automated real-cookie/form test creates a new clinic/branch/department/service/specialty/doctor login/offering/hours/availability, books as patient, logs in as the newly created doctor, approves, and verifies the patient's Onaylandı result. An unsafe shift shrink returns 409. A separate WebApplicationFactory lifetime reopens the same file and verifies configuration, Identity login/roles, original interval/fee and Approved status.

An additional live **Kestrel** rehearsal used https://localhost:7143, real antiforgery tokens/cookie sessions and the existing local Demo database:

1. Manager created **Gösterim Yönetim Kliniği 021005**, facility **3**, branch **4**, department, 30-minute service **6**, and specialty.
2. Registered synthetic doctor **4**, login `stage3doctor-021005` (the documented public demo password), and offering **6**.
3. Added opening periods 09:00–12:00 and 13:00–17:00; created availability **71** for **28 October 2026, 09:00–12:00**.
4. Patient booked `/appointments/3`, **10:00–10:30**, **775.50 TRY**, initially Onay Bekliyor.
5. The new doctor logged in and approved. Patient saw **Onaylandı** and **775,50 TRY**.
6. Manager attempted an end time of 10:15: **409**, no saved shrink.
7. Stopped the rehearsal host and started a new process against the same database. Re-login verified the facility, doctor credentials, Approved booking and exact fee. The unsafe edit remained rejected. The Stage 2 approved `/appointments/2` was also preserved.

Local evidence is in ignored `TestResults/stage3-live-result.json` and smoke/host logs. The server was left running on https://localhost:7143 after verification. The smoke script allowed only its known localhost development certificate; it changed no browser/machine trust setting. Browser users must trust their development certificate themselves with `dotnet dev-certs https --trust`. This is live HTTP form verification, not a completed visual/mobile/accessibility audit.

## Limits and checkpoint

- Single timezone/currency, same-local-day schedules and 15-minute booking starts remain deliberate. No overnight/holiday/travel-buffer/room scheduling, payments or medical records.
- No appointment cancellation/rescheduling/automatic Pending expiry. No public registration, recovery or manager/role/grant administration screens. Optional staff login creation is at initial registration; attaching a login later is not offered.
- Staff/service/offering links cannot be relocated; historical resources are retained instead of hard deletion. Deactivation does not cancel existing reservations or disable the login needed to decide them.
- Read pages can become stale; every write revalidates. Safe competing configuration edits use last-valid-write behavior; no stale-form merge UI is claimed. Failed management form entries are not automatically restored.
- Initial demo dates do not move on restart. Managers can extend schedules; deliberate synthetic reset is documented. The local SQLite deployment has one writer and no hospital-scale capacity claim.
- A full final parity/accessibility review, Release packaging and Turkish presentation guide remain Stage 4, requiring explicit approval. Course submission/attendance/group/deadline requirements remain external obligations.

Git retains the original initial commit **48d924fb90b8a5d549c46bec1491c1b9b301cc7e** on **main** with origin `https://github.com/tk612536-ops/hospital-appointment-system-csharp.git`. There are no new commits/pushes/staged changes. Stages 1–3 source/tests and this report are local uncommitted work; build outputs, SQLite files, smoke scripts/logs and other local artifacts are ignored. No timestamps or history were fabricated. Work on one actual development date does not satisfy the separate ten-day history requirement.

**Stop after Stage 3.** Recommended next increment, only after approval: Stage 4 final parity/security/OOP review, responsive/accessibility rehearsal, Release packaging and presentation preparation.
