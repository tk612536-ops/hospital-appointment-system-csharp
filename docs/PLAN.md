# Approved C# implementation plan

Design approved by the owner. **Only Stage 0 is authorized.** Baseline: 26 October 2026 (Europe/Istanbul). This is a new implementation; Java's completed stages do not confer approval on C# implementation stages.

## Grounded baseline

Read-only reference: `C:\Users\muhammed\Desktop\hospital-appointment-system`. New workspace: `C:\Users\muhammed\Desktop\hospital-appointment-system-csharp`. The original teacher DOCX, requirements, plan/UML, architecture review, presentation guide, implementation and eleven test suites were inspected. The original source permits any language; the professor's later explicit C# instruction governs this project.

Installed during planning: .NET SDK **10.0.403**, ASP.NET Core/.NET runtime **10.0.14**, Windows x64. The initial C# directory was empty. Existing Java reports contain 118 successful test executions; no Java tests were rerun and no C# test result is claimed.

Preferences: ASP.NET Core Identity; Turkish user interface and presentation notes; English identifiers and technical documentation. Preserve all T01–T13 application requirements in [REQUIREMENTS.md](../REQUIREMENTS.md), with administrative obligations tracked separately.

## Architecture and project structure

One server-rendered MVC monolith; no separate SPA or public API is needed.

```text
HospitalAppointments.sln
src/
  HospitalAppointments.Domain/
    Facilities/ Catalog/ Staff/ Patients/ Appointments/ Scheduling/
  HospitalAppointments.Application/
    Services/ Contracts/ Repositories/ Security/ Transactions/
  HospitalAppointments.Infrastructure/
    Persistence/ Configurations/ Repositories/ Identity/ Demo/ Migrations/
  HospitalAppointments.Web/
    Controllers/ Models/ Views/ Resources/ wwwroot/
tests/
  HospitalAppointments.Domain.Tests/
  HospitalAppointments.Application.Tests/
  HospitalAppointments.IntegrationTests/
docs/
```

This tree is a future target, not files to generate during Stage 0. Domain has no project/framework dependencies. Application references Domain. Infrastructure references Application and Domain. Web references Application and Infrastructure to compose DI; controllers receive application services, not DbContext or repositories.

```mermaid
flowchart LR
    B[Browser / Turkish Razor Views] --> C[MVC Controllers]
    C --> S[Application Services]
    S --> D[Domain behavior]
    S --> R[Repository contracts]
    R --> E[EF Core implementations]
    E --> DB[(SQLite)]
    I[ASP.NET Core Identity] --> DB
```

| Layer | Responsibility |
| --- | --- |
| Web controllers/models/views | HTTP, form-shape validation, views and redirects; separate inputs/read models, Turkish validation and display. |
| Application services | Use cases, scheduling, object authorization, transactions, snapshots and orchestration. |
| Repository contracts/implementations | Focused scoped queries and persistence; no business decisions in generic CRUD wrappers. |
| Domain | Encapsulated entities, local invariants, state transitions and immutable intervals; no HTTP, Identity or database calls. |
| Infrastructure | EF mappings/migrations, SQLite transaction implementation, Identity adapter, persistence and demo initialization. |

Use constructor injection, acyclic service collaborations and cancellation tokens. Focused concrete services do not each need an interface. Repository/security/transaction interfaces serve real boundary needs. Avoid MediatR, CQRS infrastructure, generic service frameworks, AutoMapper, speculative patterns and duplicate persistence/domain graphs.

## Dependency baseline

Target net10.0. Future Stage 1 pins SDK 10.0.403 with patch-only roll-forward and enables nullable reference types. Use central package versions, lock files and a local tool manifest.

| Package/tool | Pinned version |
| --- | --- |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.14 |
| Microsoft.EntityFrameworkCore.Design (development-only) | 10.0.14 |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.14 |
| Microsoft.AspNetCore.Mvc.Testing (integration tests) | 10.0.14 |
| dotnet-ef local tool | 10.0.14 |
| xunit.v3 | 3.2.2 |
| xunit.runner.visualstudio | 3.1.5 |
| Microsoft.NET.Test.Sdk | 18.0.1 |
| Microsoft.Extensions.TimeProvider.Testing | 10.0.0 |
| Locally packaged Bootstrap CSS/license | 5.3.8 |

Package availability and framework metadata were checked during planning against NuGet's official feed. The SDK does not imply NuGet packages are already restored. Stage 1 must demonstrate locked dependency restore, build and actual test discovery before this baseline is accepted operationally. MVC, Razor and DI use the shared ASP.NET Core framework. First dependency restore needs network access; prepared application assets must work locally without a CDN.

## Java to C# mapping

| Java class/area | C# equivalent |
| --- | --- |
| ClinicApplication | Web Program composition root. |
| Facility, Branch, Department, OpeningWindow | Same domain entities; EF Fluent API mapping outside Domain. |
| HealthcareService | Encapsulated entity with decimal terms and application-managed catalog version. |
| StaffMember, Doctor, HealthcareStaff, Specialty | Preserved abstract hierarchy and required doctor specialty. |
| StaffOffering, AvailabilityWindow | Explicit service eligibility and dated branch availability. |
| Patient, Appointment, AppointmentStatus | Preserved business entities/state flow and snapshots. |
| TimeInterval | Immutable C# value object for overlap, containment and continuous coverage. |
| UserAccount / Role | Infrastructure ApplicationUser plus Identity roles/user-role relationships; no duplicate custom credential system. |
| Account facility associations | Domain ManagerFacilityGrant using scalar account/facility IDs. |
| AccountDetailsService / SecurityConfig | Identity adapter, cookies, authentication and route policies. |
| AccountService / AccessService / ManagementScope | Application account/access and scoped-management use cases. |
| AppointmentService / AvailabilityService | Focused reservation/decision and scheduling services. |
| BookingPageService / ManagementQueryService | Immutable page/read DTO query services. |
| Four management services/controllers | Facility, catalog, staff and schedule application services/MVC controllers. |
| Spring Data repositories | Repository contracts in Application and EF implementations in Infrastructure. |
| @Transactional / row-lock keys | Service-owned transaction boundary with SQLite immediate writer scope; no H2 row-lock translation. |
| Clock | TimeProvider and configured TimeZoneInfo. |
| Java records/forms and Thymeleaf | C# input/read models and Razor Views. |
| Demo initializers | Explicit idempotent synthetic demo initialization. |
| JUnit/Spring tests | xUnit domain/application/integration equivalents. |

## Domain and EF relationships

Use one ApplicationDbContext derived from `IdentityDbContext<ApplicationUser, IdentityRole<long>, long>`. Use long keys. Domain patient/staff/grant/decision references use scalar account IDs; infrastructure defines foreign keys to Identity users. Application access DTOs expose current roles/enabled state/grants without exposing hashes or Identity entities.

```mermaid
classDiagram
    StaffMember <|-- Doctor
    StaffMember <|-- HealthcareStaff
    Facility "1" --> "1..*" Branch
    Facility "1" --> "0..*" StaffMember : employs
    Branch "1" --> "0..*" Department
    Branch "1" --> "0..*" OpeningWindow
    Department "1" --> "0..*" HealthcareService
    Doctor "0..*" --> "1" Specialty
    StaffMember "1" --> "0..*" StaffOffering
    HealthcareService "1" --> "0..*" StaffOffering
    StaffMember "1" --> "0..*" AvailabilityWindow
    Branch "1" --> "0..*" AvailabilityWindow
    Patient "1" --> "0..*" Appointment
    StaffOffering "1" --> "0..*" Appointment
    ApplicationUser "1" --> "0..1" Patient
    ApplicationUser "0..1" --> "0..1" StaffMember
    ApplicationUser "1" --> "0..*" ManagerFacilityGrant
    Facility "1" --> "0..*" ManagerFacilityGrant
    Appointment "0..*" --> "0..1" ApplicationUser : decision actor
    Appointment ..> TimeInterval
```

- Facility creation includes its first branch and creator grant atomically; a foreign key alone cannot enforce minimum child count.
- Service → Department → Branch derives its venue/facility. Similar procedures at different branches have independent catalog entries/terms.
- Staff belong to one facility. Offerings link staff/services uniquely; their resource links cannot later be reassigned. No redundant staff/branch membership table is needed.
- Availability is staff/branch/date-specific and independent of individual services. Active service assignment at that branch is required for new availability.
- Patients require a unique account link. Staff accounts are optional/unique; one account can have both profiles. Identity composes roles.
- Appointment links patient/offering and snapshots booked service/staff names, duration, fee/currency, start/end, creation/status and optional decision actor/time.
- Persist staff with **table-per-hierarchy** and a discriminator, replacing Java JOINED inheritance. Check doctor specialty/healthcare-worker job-title consistency. This simplifies SQLite joins without changing C# OOP semantics.

Use private setters/backing fields, validating constructors and named behavior. Do not bind entities to HTTP. Preserve restrictive deletion and deactivate referenced records. Keep navigation loading explicit; return fully prepared read DTOs rather than relying on lazy loading in Razor.

Database integrity: required FKs; scoped name uniqueness matching the reference; normalized username uniqueness; unique staff/service offerings and manager/facility pairs; valid status/discriminator checks; positive intervals; duration 1–1439; exact nonnegative fees; decision actor/time present exactly when decided. Use indexes for patient/time and offering/time, staff offerings and staff/branch availability. Employee conflict queries traverse all offerings, without a branch filter.

Money remains decimal in C# and converts exactly to SQLite integer minor units. Reject fractional kuruş and amounts beyond DECIMAL(12,2)-equivalent bounds (maximum 9,999,999,999.99 TRY). Do not round silently or use floating point. Local form inputs use DateOnly/TimeOnly and Europe/Istanbul. Appointment/availability/audit instants use UTC DateTime, mapped explicitly to integer ticks with UTC kind restored on materialization. Require server-translated comparisons; calculate query boundaries in application code. Duration checks can compare integer tick differences exactly. Weekly hours retain local time/day semantics.

Use EF migrations from Stage 1; apply identical migrations to integration databases. Do not mix EnsureCreated with migrations. Use application-managed concurrency tokens for catalog and appointment updates, not unsupported database-generated rowversion.

## OOP evidence

| Concept | Concrete implementation and explanation |
| --- | --- |
| Encapsulation | Appointment.Approve/Reject, HealthcareService.ReviseTerms and named activation methods; validate before mutation, including failed-operation atomicity. |
| Inheritance | Doctor and HealthcareStaff share real staff identity/employment/eligibility; subtype fields retain real meaning. |
| Abstraction | Abstract staff professional-description contract and focused repository/security/transaction ports. |
| Polymorphism | Mixed staff lists call the shared description member; doctor supplies specialty and healthcare worker supplies job title. Common booking has no staff-type switch. |
| Single responsibility | HTTP, availability calculation, appointment coordination and queries have separate responsibilities. |
| Open/closed / Liskov | Either real staff subtype works through the same booking contract; no unsupported base operations. No claim that a new subtype requires zero changes everywhere. |
| Interface segregation | Small useful repository/use-case boundaries, no universal CRUD interface. |
| Dependency inversion / DI | Services receive ports and time rather than constructing persistence or reading the wall clock directly. |
| Composition | Identity roles, manager grants, offerings and facility structure are relationships rather than a Person/Patient/Doctor/Manager inheritance explosion. |

Professional descriptions expose stored specialty/job-title text; domain code does not depend on Razor/localization resources. The UI supplies Turkish labels.

## Security, public contracts and UI

`BookAsync(BookAppointmentCommand, CancellationToken)` returns the ID. Input contains OfferingId, local Date/StartTime and expected CatalogVersion only. `DecideAsync(appointmentId, decision, CancellationToken)` accepts approve/reject. Acting account comes from a server-side current-actor adapter; services reload eligibility and grants. Never accept patient, audit actor, roles, duration/fee/end or status as trusted booking inputs.

Route policies enforce PATIENT/STAFF/MANAGER groups. Services additionally enforce patient ownership, assigned staff identity, manager facility grant and every child ID's actual scope. Any manager can create a facility and receive its grant atomically. Manager staff registration can provision a new STAFF identity in the same context/transaction; it cannot attach an existing account or grant submitted elevated roles.

Use Identity hashing and five-failure/five-minute login lockout with lockoutOnFailure. Trim usernames, use invariant Identity normalization and validate raw/normalized bounds. Turkish display names are permitted. Use protected POST login/logout, antiforgery for all unsafe MVC methods, local return URLs, Razor encoding and HttpOnly/Lax cookies with HTTPS for the prepared local demo. Do not expose passwords/hashes. No public signup/recovery/role-admin screens in initial scope.

Turkish screens: login/role workspaces; patient catalog → employee → date → valid starts, terms review and Pending result; own appointments/details; assigned staff decisions; manager configuration and scoped appointment review. Status labels include Onay Bekliyor, Onaylandı and Reddedildi. Use Bootstrap, labels, skip links, focus indicators, readable errors, textual statuses and responsive forms/tables. Preserve selections after conflict; handle empty catalogs/no eligible staff/no starts usefully. Technical locking/ORM details belong in documentation, not patient forms.

Core scheduling and concurrency are specified in [CONCURRENCY.md](CONCURRENCY.md). Bookings are future/same-local-day, half-open intervals, 15-minute starts, full coverage of both opening and matching staff availability, with global patient and cross-branch employee conflicts. Pending/Approved reserve; Rejected releases. Snapshots survive catalog edits and deactivation. Decisions are single transitions and remain available after offering deactivation.

## Stages and checkpoints

| Stage | Deliverable | Exit evidence |
| --- | --- | --- |
| 0 — authorized | Approved documents, active AGENTS.md, .gitignore, local Git, verified new account/remote and one authorized initial commit/push | Review staged content, verify published SHA and branch, report authentication/repository issues; stop. |
| 1 — not authorized | Solution/DI, Identity, initial migrations, local Turkish layout, synthetic role accounts | Locked restore, build and actual test discovery; login/routes/antiforgery/migrations pass. |
| 2 — not authorized | Domain hierarchy, seeded catalog/shifts, booking/details/lists/approval/rejection, SQLite transaction adapter | Patient → staff decision → patient flow; interval/ownership/snapshot tests and real concurrency proof. |
| 3 — not authorized | Scoped facility/branch/department/service/staff/specialty/offering/hours/availability management | Newly configured services become bookable and new staff can approve; configuration races preserve valid state. |
| 4 — not authorized | Parity/security/OOP review, accessibility polish, restart/offline rehearsal and Turkish presentation guide | Requirement evidence, full build/test/publish and packaged demonstration; honest limitations. |

Stop at every stage checkpoint. Passing a stage does not authorize the next stage or further commits/pushes. Stage 2 delivers the first complete booking-and-approval workflow before manager CRUD.

## Test strategy and demo operations

Use [TEST_PARITY.md](TEST_PARITY.md) for per-scenario migration tracking. Domain tests use plain xUnit; application tests use controlled time and small doubles; integration tests use migrated SQLite files and WebApplicationFactory with actual Identity cookies/forms/antiforgery. Never use EF InMemory to prove SQLite behavior. Verify exact fee/timestamp mappings, subtype materialization, constraints, translations, object isolation and snapshots. Provider-specific Java assertions must be replaced, not copied blindly.

Concurrency evidence includes independent contexts/connections and separate processes against one file, deterministic synchronization, final committed invariants and bounded lock failure. Each stage includes relevant checks. Final verification adds full suite, locked restore, dependency review, Release publish, persistent restart and browser/local-assets rehearsal. Record only commands/results actually executed; C# results currently do not exist.

Demo mode is explicit and disabled elsewhere. Seed a one-branch clinic and two-branch hospital, doctor/non-doctor employees, 30/45/20-minute synthetic services, two patients and separate scoped managers. Initial dates start tomorrow and span fourteen days from TimeProvider. Use a stable seed marker independent of editable names; do not recreate or move saved reservations on restart. Document an explicit aged-demo reset/extension procedure.

Keep the database outside wwwroot; resolve its path from content root. Ignore SQLite/WAL/SHM files, logs, temporary outputs and secrets. Rehearse future schedules before class. No Java database/credentials are imported.

## Ten genuine development days and external follow-up

The deadline is unconfirmed. These are relative actual workdays, possibly nonconsecutive, not fabricated commit dates:

| Day | Meaningful increment (with relevant tests) |
| --- | --- |
| 1 | Requirements, architecture and agent instructions. |
| 2 | Foundation, Identity and migrations. |
| 3 | Domain/staff model and interval behavior. |
| 4 | First booking-and-approval flow. |
| 5 | SQLite concurrency/rollback and stale terms. |
| 6 | Facility/branch/department/service management. |
| 7 | Staff/specialty/offering management. |
| 8 | Schedules and configuration races. |
| 9 | Turkish UI, security and regression review. |
| 10 | Parity audit, packaged rehearsal and presentation evidence. |

Only the single Stage 0 initial commit/push is currently authorized. Later workdays and commits require their own authorization. Do not split already completed work later to imply earlier development. Verify SABIS/presentation dates, selection record and group obligations separately.

Sources supporting technical decisions: [SQLite provider limitations](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations), [EF transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions), [xUnit package](https://www.nuget.org/packages/xunit.v3/3.2.2), and the official NuGet flat-container manifests checked during planning. This document describes approved intent; future verification records must distinguish implementation from proposal.
