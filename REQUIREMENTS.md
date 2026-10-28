# Hospital/Clinic Appointment System — C# requirements

Status: Stages 0–3 accepted; **Stage 4 final review and presentation preparation authorized and implemented**, 27 October 2026, Europe/Istanbul. The single authorized Stage 1–3 checkpoint was pushed as abebdb2d0142ea883af83aa1475b385874d22e79. Stage 4 changes remain local/uncommitted; no further commit/push is authorized. Actual final results are in docs/STAGE4_VERIFICATION.md.

## Sources and authority

The read-only Java reference is `C:\Users\muhammed\Desktop\hospital-appointment-system`. The independent C# project is `C:\Users\muhammed\Desktop\hospital-appointment-system-csharp`.

Reviewed reference material: `docs/ProjeOdevi_Son.docx`, `REQUIREMENTS.md`, `docs/PLAN.md`, the embedded Mermaid UML diagrams, `docs/ARCHITECTURE_REVIEW.md`, `docs/PRESENTATION_GUIDE.md`, source/domain/repository/service/controller code, schema and all eleven test suites. The original DOCX SHA-256 is `E636C9E18DC27A995A69E7C59800C3CB7506FB17EDC0884F5D563D6F67593226`.

The written assignment permits any language/platform. **The professor subsequently explicitly required C#; the owner relayed that instruction, and it governs this rebuild.** Do not represent Java as satisfying the current language requirement. The new implementation preserves behavior and architectural quality, not Java frameworks or H2 locking syntax.

Classification: **T** = written teacher requirement; **R** = teacher recommendation; **U** = owner instruction (including the relayed C# requirement); **A** = approved engineering choice; **Q** = unresolved external fact. Paragraph locators P1–P50 follow the Java audit: main-body Word XML order, including blank paragraphs, not page numbers.

## Mandatory application requirements

| ID | Written requirement and source | Planned C# acceptance evidence |
| --- | --- | --- |
| T01 | OOP Hospital/Clinic Appointment System; Option A, P4 | Runnable ASP.NET Core MVC application and explainable domain model. |
| T02 | Support one site and organizations with multiple branches; P4 | Functional one-branch clinic and two-branch hospital, using one model. |
| T03 | Concrete class structure, inheritance, polymorphism and encapsulation; P4 | Abstract StaffMember, Doctor/HealthcareStaff overrides, common staff flow, invariant-protecting methods. |
| T04 | Authorized hospital/clinic definition; P12 | Managers create facilities and administer only granted facilities. |
| T05 | Opening hours and departments; P13 | Branch department management and weekly opening periods, including gaps. |
| T06 | Detailed services with duration and fee; P14 | Create/edit service terms; positive whole-minute duration and exact nonnegative money. |
| T07 | Register doctors and healthcare personnel; P16 | Functional registration of both concrete staff types. |
| T08 | Define doctor specialty and provided services; P17 | Primary specialty plus explicit service offerings; specialty alone grants no service. |
| T09 | Define doctor availability and restrict booking to it; P18 | Dated branch availability; full-duration coverage checks for both staff types. |
| T10 | Choose suitable employee and procedure to book; P20 | Patient selects an eligible offering and a valid start; non-doctor bookings work. |
| T11 | Detect conflicts on creation; P21 | Patient and employee overlaps rejected, including concurrent requests and cross-branch employee conflicts. |
| T12 | Save procedure, duration, fee, employee and date/time; P22 | Persist/reload details and immutable booked snapshots. |
| T13 | Appointment approval mechanism; P23 | Assigned staff or scoped manager approves/rejects pending bookings, with decision audit. |

The table defines acceptance targets. Stage 3 coverage is summarized below. General examination, radiology and laboratory are examples in the source, not a prescribed exhaustive catalog.

## Delivery and presentation obligations

| ID | Obligation and source | Status / handling |
| --- | --- | --- |
| T14 | Individual or group of at most two; P10/P40 | Actual contributors/group status must be confirmed by the owner. |
| T15 | Share through GitHub and upload to SABIS; P41/P43 | Separate C# repository designated below. SABIS submission remains a human/course obligation. |
| T16 | Minimum ten GitHub commits made on different days; P42 | Owner requires meaningful commits across at least ten actual development days. No backdating or fabricated history. |
| T17 | SABIS upload before final deadline; P43 | Exact deadline and upload format are not confirmed. |
| T18 | Each student presents and attends for project points; P44/P45/P49 | Prepare Turkish presentation notes; attendance is not established by software tests. |
| T19 | Files must be uploaded before presentation participation; P50 | Confirm submission receipt with the course. |
| T20 | Record project selection by 13 October 2026; P46/P47 | Date has passed; selection/extension status is unknown. Do not assume compliance. |

The source mentions both the last four weeks and presentations starting in week seven. Follow the course announcement rather than inventing dates. Option B's paper-review, SCI/SCIE, foundational-source and six-page-report rules do not apply to this Option A software project.

R01: the teacher **recommends** at least three role groups in P25. PATIENT, STAFF and MANAGER are mandatory for this project through the owner's instructions. Doctors and other healthcare personnel share STAFF; roles are composed, not subclasses.

## Binding owner instructions

- U01: use C#/.NET 10, ASP.NET Core MVC, EF Core, SQLite, Razor Views/Bootstrap, xUnit, ASP.NET Core Identity and built-in DI with pinned stable compatible dependencies.
- U02: maintain Controller → Service → Repository → Database with a separate domain project; services own cross-object rules, authorization and transactions.
- U03: demonstrate encapsulation, inheritance, abstraction, polymorphism, SOLID and composition genuinely, without artificial interfaces or enterprise frameworks.
- U04: preserve the Java functional baseline and meaningful test scenarios. Check entire service duration, not just a free starting instant.
- U05: prove booking concurrency on SQLite; Java/H2 results are not SQLite evidence.
- U06: Turkish UI and presentation notes; English C# identifiers and technical documentation.
- U07: synthetic data only, professional accessible local UI, reproducible classroom demo and future initial demo dates.
- U08: incremental stages with explicit approval at every checkpoint. Deliver core booking and approval before complete manager configuration.
- U09: never modify, delete, overwrite or run write-producing operations in the Java reference directory.
- U10: Stages 0–3 are accepted, with 191 passing tests. On 27 October the owner authorized one genuine Stage 1–3 checkpoint commit/push, then Stage 4 only after verification: teacher/OOP/architecture/security audit, necessary fixes, Turkish UI/accessibility improvements, final tests/migration check/Release publish, Turkish presentation and Windows delivery instructions. All 191 cases are retained. The checkpoint succeeded. **No additional commit or push is authorized. Stop after Stage 4.**

Target remote: `https://github.com/tk612536-ops/hospital-appointment-system-csharp.git`. Verify that the authenticated Git transport account is `tk612536-ops` before committing/pushing; the old account is not acceptable.

## Approved behavioral decisions

| ID | Decision |
| --- | --- |
| A01 | Facility represents a hospital/clinic organization. Each has at least one Branch; departments, weekly hours and services are branch-scoped. First branch and creator grant are created atomically. |
| A02 | Appointment intervals are half-open [start, end). Entire duration must fit continuous opening and branch-specific employee availability; adjacent windows merge, positive gaps do not. |
| A03 | Employee conflicts span all branches; patient conflicts span the application. Pending and Approved reserve time; Rejected releases it. |
| A04 | Pending → Approved or Pending → Rejected only. Assigned employee or facility-granted manager decides once, recording actor and time. No cancellation, expiry or rescheduling workflow. |
| A05 | Derive fee/duration/end and actor identity on the server. Preserve service/staff names, duration, fee and currency snapshots. Reject stale displayed catalog terms. Facility/branch names and professional descriptions remain current relational labels, not additional historical snapshots. |
| A06 | One employing facility per employee, one primary specialty per doctor; immutable offering links define employee/service eligibility and branch assignment. |
| A07 | Dated staff availability must fit opening hours and must not overlap other shifts for that employee, including across branches. |
| A08 | Schedule edits protect future/ongoing Pending and Approved booked intervals using snapshot duration. Opening edits also protect future shifts. Deactivate referenced staff/services/offerings instead of deleting history. |
| A09 | Europe/Istanbul, TRY, 15-minute candidate-start grid, positive service durations 1–1439 minutes (not necessarily grid multiples), strictly future starts, same-local-day intervals. |
| A10 | Identity manages passwords, login/lockout, roles and cookies. Public signup, recovery and role/grant administration screens are deferred. Optional new staff credentials may be provisioned by scoped managers. |
| A11 | SQLite immediate writer transactions coordinate booking, decisions and relevant configuration changes; revalidate after acquiring the writer position. See docs/CONCURRENCY.md. |
| A12 | Four production projects, three test projects, external EF mappings, TPH staff inheritance, exact integer minor-unit money storage, UTC-tick instant storage, EF migrations. |
| A13 | Explicit demo mode; idempotent synthetic fixtures initially span fourteen future dates. Restart never duplicates or moves saved bookings; resetting an aged demo is explicit. Assets are local. |

## Exclusions and unresolved facts

Payments, medical records/diagnosis, prescriptions, messaging, public registration, rooms/equipment scheduling, travel buffers, holiday/overnight calendars, multiple timezones/currencies and cloud deployment are outside initial scope. No Java database or credential migration is planned; create fresh synthetic C# data.

External questions remain: submission/presentation dates, SABIS format, selection status, group members and any further course instructions. They do not authorize invented dates or block Stage 0 documentation.

See [approved implementation plan](docs/PLAN.md), [SQLite implementation](docs/CONCURRENCY.md), [test parity](docs/TEST_PARITY.md), [teacher audit](docs/REQUIREMENTS_AUDIT.md), [Stage 4 evidence](docs/STAGE4_VERIFICATION.md) and [agent instructions](AGENTS.md).

Stages 1–4 implement and audit T01–T13: manager-created single/multi-branch facilities, departments, exact service terms, both staff types, specialties, offerings, multiple opening periods and dated availability feed patient booking and staff approval. Services enforce grants/child scope and the unchanged immediate-writer protocol; schedule changes protect future/ongoing Pending/Approved intervals. Deactivation retains history/decisions. The 197-test suite, published restarted workflow, targeted browser/keyboard checks and new audit/presentation/delivery documents provide evidence. Administrative submission, attendance, contributor/selection status and ten genuine development days remain external obligations, not established by passing tests.
