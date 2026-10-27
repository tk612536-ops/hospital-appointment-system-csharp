# Project instructions

## Authorization and reference protection

- Build the university Hospital/Clinic Appointment System in C#/.NET 10. Read REQUIREMENTS.md, docs/PLAN.md, docs/CONCURRENCY.md and docs/TEST_PARITY.md.
- Stages 0–3 are accepted. On 27 October 2026 the owner authorized **one genuine Stage 1–3 checkpoint commit and push to existing main** after reviewing Git hygiene and verifying Git transport account/remote. Use the actual time, preserve initial history, never force-push. If push fails, stop and explain.
- **Only after that checkpoint is successfully pushed and verified**, Stage 4 is authorized: teacher-requirements/OOP/architecture/security review, necessary fixes, Turkish responsive/accessibility improvements, final tests and Release publish, Turkish presentation guide and Windows delivery documentation. Preserve all 191 existing tests. Do not implement unrelated features.
- No additional commit or push is authorized beyond that single checkpoint. Stop after Stage 4 verification/reporting. Preserve the existing initial commit and its actual timestamp.
- The Java project at `C:\Users\muhammed\Desktop\hospital-appointment-system` is a read-only reference. Never edit, delete, reset, migrate, run its application/build/tests, or execute any operation that writes within that directory.
- Keep C# work under `C:\Users\muhammed\Desktop\hospital-appointment-system-csharp`.
- Target remote: `https://github.com/tk612536-ops/hospital-appointment-system-csharp.git`. Verify the Git transport account is `tk612536-ops`, not the old account, before committing/pushing. Do not overwrite remote history or force-push.
- Never fabricate timestamps, backdate commits or imply development occurred on dates when it did not. Preserve meaningful work across at least ten actual development days.
- Use synthetic demonstration data only. Exclude real credentials, personal/clinical data and local databases from version control.

## Architecture and implementation when authorized

- Use ASP.NET Core MVC, EF Core SQLite, ASP.NET Core Identity, Razor Views, local Bootstrap, xUnit and built-in dependency injection with stable pinned compatible packages.
- Preserve Controller → Service → Repository → Database and a separate framework-independent Domain project. Application depends on Domain; Infrastructure implements its ports; Web composes DI.
- Controllers handle HTTP/input/view adaptation. Services own business rules, authorization and transaction boundaries. Repositories own queries/persistence. Domain methods protect local invariants and state transitions.
- Separate HTTP input models and read DTOs from entities; keep passwords/hashes out of read models and logs.
- Preserve abstract StaffMember and Doctor/HealthcareStaff subclasses. Demonstrate polymorphism through shared staff use cases, without booking type switches.
- Use private state, validating constructors/named behavior and atomic failed mutations. Compose roles and grants rather than inventing a Person/Patient/Manager hierarchy.
- Use focused interfaces where substitution is useful. Avoid generic CRUD frameworks, artificial patterns and duplicate domain/persistence graphs.
- Use Turkish UI/presentation notes and English identifiers/technical documentation. Keep implementation jargon out of patient flows.
- Pin dependencies and update lock files intentionally. Do not treat planned compatibility as an executed restore/build result.

## Scheduling, persistence and security when authorized

- Check the complete interval, including opening/shift gaps. Use half-open boundaries, cross-branch employee conflicts and application-wide patient conflicts.
- Pending and Approved reserve time; Rejected releases it. Derive booked terms/actor identity on the server and preserve snapshots. Reject stale displayed terms.
- Acquire the SQLite immediate writer transaction before reading mutable booking/configuration state, revalidate, persist and confirm commit. Use the same protocol for decisions and relevant configuration writes.
- Repositories and participating Identity writes share one scoped context/transaction. Do not carry stale tracked entities into a command or hold a transaction across user interaction.
- Follow docs/CONCURRENCY.md. Never claim rowversion, a unique start or a process-local mutex prevents competing appointment inserts. Bound contention; roll back failures and never report unconfirmed success.
- Protect future/ongoing reservations on schedule edits. Retain historical relationships; deactivate referenced staff/services/offerings rather than cascading deletion.
- Use EF migrations, TPH staff mapping, exact integer minor-unit money storage, UTC-tick instants, Europe/Istanbul display and injected TimeProvider. Tests use the real migrated SQLite provider.
- Enforce role policies plus service-level ownership/facility grants and child-resource scope. Derive actor/patient from authentication; reject forged identity/role/term inputs.
- Keep antiforgery, Identity password hashing/lockout, local return URLs, Razor encoding and safe cookies. New staff credentials may not take over an existing account or grant submitted roles.

## Verification and teaching

- Add meaningful tests with each authorized implementation stage. Test actual concurrency with independent connections/transactions against one SQLite file, including cross-process verification.
- Port behavioral Java coverage and explain provider-specific replacements in docs/TEST_PARITY.md. Stage 1 has actual foundation tests; do not confuse their count with the historical 118 Java cases.
- Report only commands/results actually executed. Never describe Java's historical 118-test result as C# coverage or a newly rerun result.
- Seed future synthetic dates idempotently in explicit demo mode. Restart must never silently move or duplicate saved appointments. Document deliberate reset/extension of an aged demo.
- Keep local assets, diagrams, requirement coverage, setup and presentation explanations consistent with implemented code.
- Finish each authorized stage with evidence, limitations and a clear checkpoint. Administrative submission/attendance/history requirements are separate from passing tests.

## Current checkpoint evidence

- Stage 3 is accepted. docs/STAGE3_VERIFICATION.md records the actual 191 passing tests (31 domain, 26 application, 134 integration), live manager-created booking/approval and restart evidence. Preserve every existing case.
- At checkpoint preparation Stages 1–3 are local/uncommitted and the sole Stage 0 commit is preserved. Verify the authorized checkpoint before starting Stage 4; keep all Stage 4 changes uncommitted.
