# Project instructions

## Authorization and reference protection

- Build the university Hospital/Clinic Appointment System in C#/.NET 10. Read REQUIREMENTS.md, docs/PLAN.md, docs/CONCURRENCY.md and docs/TEST_PARITY.md.
- The design is approved, but **Stage 0 only is currently authorized**: save planning documents, initialize local Git, add .gitignore, verify the designated repository/new GitHub account, review staged content, and create/push **one genuine initial planning commit** at the actual current time.
- Stop after Stage 0 verification/reporting. Do not scaffold ASP.NET Core, generate solution/project files, restore application packages, implement tests/application code or proceed to Stage 1 without explicit approval.
- Each later stage requires an explicit checkpoint approval. All later commits/pushes require explicit authorization; this single-commit permission does not persist beyond Stage 0.
- The Java project at `C:\Users\muhammed\Desktop\hospital-appointment-system` is a read-only reference. Never edit, delete, reset, migrate, run its application/build/tests, or execute any operation that writes within that directory.
- Keep C# work under `C:\Users\muhammed\Desktop\hospital-appointment-system-csharp`.
- Target remote: `https://github.com/tk612536-ops/hospital-appointment-system-csharp.git`. Verify the Git transport account is `tk612536-ops`, not the old account, before committing/pushing. Do not overwrite remote history or force-push.
- Never fabricate timestamps, backdate commits or imply development occurred on dates when it did not. Preserve meaningful work across at least ten actual development days; only Stage 0 is currently authorized.
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
- Port behavioral Java coverage and explain provider-specific replacements in docs/TEST_PARITY.md. No C# tests exist during Stage 0.
- Report only commands/results actually executed. Never describe Java's historical 118-test result as C# coverage or a newly rerun result.
- Seed future synthetic dates idempotently in explicit demo mode. Restart must never silently move or duplicate saved appointments. Document deliberate reset/extension of an aged demo.
- Keep local assets, diagrams, requirement coverage, setup and presentation explanations consistent with implemented code.
- Finish each authorized stage with evidence, limitations and a clear checkpoint. Administrative submission/attendance/history requirements are separate from passing tests.
