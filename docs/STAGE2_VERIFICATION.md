# Stage 2 — OOP model, booking and approval verification

Executed on **26 October 2026**, Windows x64, SDK **10.0.403**, runtime/EF Core **10.0.14**, SQLite provider, xUnit v3 **3.2.2**. Stage 2 alone was authorized. No manager configuration screens, commit or push were created.

## Implemented classes and responsibilities

Domain: Facility/FacilityType, Branch, Department, OpeningWindow, HealthcareService, abstract StaffMember, Doctor, HealthcareStaff, Specialty, StaffOffering, AvailabilityWindow, Patient, Appointment/AppointmentStatus, ManagerFacilityGrant and immutable TimeInterval. Existing AccountProfile/AccountRole remain credential-free. DomainRules centralizes local text/fee/UTC guards. No domain class references MVC/EF or calls a repository.

Encapsulation is demonstrated by private state, validating constructors, named updates/deactivation and atomic decision/term validation. Doctor and HealthcareStaff inherit genuine shared employment/identity state and override ProfessionalDescription with specialty/job title. BookingPageService and appointment detail generation invoke that override through StaffMember. Roles/grants/offerings use composition. Small repository/security/transaction ports and constructor injection provide useful abstractions without a universal CRUD framework.

Application: ClinicTime converts configured local dates/times to UTC; AvailabilityService shares full-duration/grid/future/continuous-coverage/conflict checks between candidates and submission. BookingPageService prepares cascade choices/read DTOs. BookingAccessService enforces patient identity, assigned employee and manager facility grants. AppointmentService owns booking, one-way approval/rejection, transactions and snapshots. Controllers bind inputs and return views/responses; repositories perform scoped SQL queries. No scheduling or EF logic exists in controllers.

Infrastructure: BookingCatalogRepository, ScheduleRepository, BookingAccessRepository, AppointmentRepository and SqliteWriteTransaction all use the same scoped ApplicationDbContext. DemoBookingSeeder creates an atomic synthetic graph; its stable marker is infrastructure-only. Booked names, duration, fee/currency and interval are preserved; decision actor FK/time and versions are stored.

## Persistence and actual transaction protocol

Mappings use TPH staff inheritance, required FKs, unique offering/account/grant relationships, subtype checks, positive interval/state/audit checks, restrictive history deletion, UTC-tick converters and decimal-to-integer-minor-unit converters. SQLite's INTEGER affinity alone does not prohibit fractional REAL values, so explicit typeof checks enforce integer fees/durations. Tests reject fractional direct SQL fees, invalid intervals/audit/subtypes and deletion of an appointment's offering.

Migrations applied:

1. `20261025214521_InitialIdentity` (Stage 1).
2. `20261025221816_BookingDomain`.
3. `20261025222819_ExactMinorUnits`.

EF's migration identifiers use UTC; no timestamps/history were fabricated. ExactMinorUnits rebuilt tables to change SQLite checks. EF emitted a warning that the foreign-key PRAGMA cannot run inside a transaction; the migration completed. This local migration limitation does not weaken the booking/decision transaction protocol, but migrations are not claimed to be fully atomic. A model-consistency check reported no pending model changes.

Booking performs an untracked authorization precheck, begins native `BeginTransaction(deferred: false)`, enlists that exact transaction using EF `UseTransactionAsync`, and only then loads authoritative catalog/hours/shifts/reservations. A clean tracker avoids stale previously loaded terms. It revalidates actor/patient and catalog version, applies shared scheduling rules, adds Pending snapshots, saves and commits before returning success. Decisions precheck ownership then use the same writer scope, reload current state and decide exactly once.

The connection has a five-second busy timeout; error code 5 is translated to DatabaseBusyException. HTTP booking returns 503 with Retry-After: 1 and retained selections. Uncommitted scopes roll back and dispose both EF/native transaction state; no automatic business replay occurs. Read-only candidates remain advisory. Versions are additional stale-update protection, not the insert-race mechanism. There is no in-memory booking lock or unique-start shortcut.

## Executed commands and results

| Command | Actual result |
| --- | --- |
| `dotnet restore HospitalAppointments.sln --locked-mode` | Successful; seven locked project graphs, no added packages. |
| `dotnet build HospitalAppointments.sln --no-restore` | Successful, **0 warnings / 0 errors**. |
| `dotnet ef migrations add BookingDomain --project src/HospitalAppointments.Infrastructure --startup-project src/HospitalAppointments.Web --output-dir Migrations` | Generated model migration. |
| `dotnet ef migrations add ExactMinorUnits --project src/HospitalAppointments.Infrastructure --startup-project src/HospitalAppointments.Web --output-dir Migrations` | Generated integer-type constraint hardening. |
| `dotnet ef database update --project src/HospitalAppointments.Infrastructure --startup-project src/HospitalAppointments.Web` | Both Stage 2 migrations applied successfully. |
| `dotnet ef migrations has-pending-model-changes --no-build --project src/HospitalAppointments.Infrastructure --startup-project src/HospitalAppointments.Web` | No model changes since the last migration. |
| `dotnet test HospitalAppointments.sln --no-restore --logger "trx;LogFilePrefix=stage2-accepted" --results-directory TestResults` | **113 passed, 0 failed, 0 skipped**. |

| Test project | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Domain.Tests | 25 | 0 | 0 |
| Application.Tests | 20 | 0 | 0 |
| IntegrationTests | 68 | 0 | 0 |
| **Total** | **113** | **0** | **0** |

Reports/logs are ignored under TestResults. The 57 original foundation cases remain (8 + 13 + 36). Their obsolete stage-boundary assertions were updated transparently: all known migrations must be applied, foundation-only fixtures have no appointment rows (rather than no appointment entity type), and the unavailable booking URL case now checks a deferred Stage 3 URL. The test factory explicitly disables booking fixtures for foundation tests. No original test method/theory case was removed; 56 new cases exercise Stage 2.

The 118 Java cases remain historical reference evidence, not the C# count. Stage 3 management tests remain deferred. TEST_PARITY.md maps meaningful equivalents/adaptations without claiming every original case has been reimplemented.

## Concurrency and reliability proof

All relevant tests use actual migrated SQLite files, separate scopes/contexts/connections and bounded synchronization before writer acquisition. None uses EF InMemory or an enclosing rollback-only test transaction.

- Same staff, overlapping different starts with no existing appointments: one commit.
- Same patient/different staff: one commit.
- Same doctor across branches: one commit, using an explicitly labeled defensive overlapping-shift fixture.
- Back-to-back intervals: both commit and persisted end/start boundaries match.
- Competing staff approval and manager rejection: one transition with consistent audit.
- An acquired writer scope has an enlisted native transaction on the same EF connection and blocks another writer even before any domain read/write.
- A saved INSERT without commit disappears on scope disposal; a fresh booking subsequently succeeds.
- A held writer causes bounded busy failure, no partial reservation and a successful fresh retry after release.
- A previously tracked catalog cannot bypass a separately committed terms change because the writer scope refreshes state.
- HTTP busy handling returns actual 503/Retry-After and preserved date/selection without a saved appointment.
- **Two real Kestrel child processes**, separate ports/PIDs, one shared SQLite file, real Identity cookies/forms and synchronized conflicting POSTs: one redirect success, one 409 conflict, one final appointment. Child processes are stopped by the test. The known-localhost test certificate callback changes no machine/browser trust settings.

Other evidence covers exact 250.25 TRY → 25025 integer storage, UTC round trips, full interval/gap/boundary checks, empty catalog recovery, forged input ignored, CSRF on decisions, role/object/facility isolation, positive scoped manager lists, non-doctor booking, rejection release, stale terms, deactivation/history and seed/name/decision persistence across separate host lifetimes.

Stage 3 schedule/catalog management command races are not implemented or claimed. Direct database edits in fixtures simulate catalog changes or defensive imported inconsistencies; they are not a manager configuration UI.

## Live Kestrel workflow

Used the real Demo application, persisted local file, real form tokens and cookie sessions at https://localhost:7143:

1. First Stage 2 startup seeded facilities, services, staff, patients, grants, fourteen future dates and one example Pending appointment. Initial dates began **27 October 2026**.
2. Signed in as `patient`, selected the 30-minute North/Kuzey examination with Dr. Deniz, and submitted **27 October 2026, 10:30**.
3. Detail `/appointments/2` showed **Onay Bekliyor**, **10:30–11:00**, **30 minutes**, **600.00 TRY**. This observed ID is specific to this demo file.
4. Signed in separately as `staff`, opened the same detail and submitted **Onayla** with its real antiforgery token.
5. Refreshed as `patient`: detail and history showed **Onaylandı** with the original interval/duration/fee. The full HTTP integration test additionally verifies client-forged terms and another patient's denial.

The last source changes were cleanup/error handling and view styling; the server was restarted afterward against the same database. Local smoke scripts use SkipCertificateCheck only for the known localhost certificate, which is not trusted on this machine. Browser users must run `dotnet dev-certs https --trust` themselves; no browser warning was bypassed. This is HTTP form verification, not a full visual/mobile/accessibility audit.

## Limits and checkpoint

Single timezone/currency, same-day windows, fifteen-minute starts, dated shifts and manual approval remain deliberate. Pending reservations have no automatic expiry. No cancellation/rescheduling, travel/room/equipment scheduling, clinical data, public registration or production deployment is claimed. Old demo dates require an explicit reset/extension; restarts never move history. Resource ownership links are immutable; future configuration must use the shared writer protocol and preserve reservations.

Manager configuration screens and their mutation policies are **Stage 3**, requiring approval. The Java reference remains read-only and unchanged. Git retains the single original Stage 0 commit `48d924fb90b8a5d549c46bec1491c1b9b301cc7e`; Stages 1–2 are local/uncommitted and no push occurred. Stop after Stage 2; neither passing tests nor this report authorizes a new stage or commit.
