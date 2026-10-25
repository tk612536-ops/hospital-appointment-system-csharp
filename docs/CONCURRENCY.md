# SQLite booking and configuration concurrency

Status: **approved design; implementation and executable proof not yet authorized**. Stage 0 records the protocol only. Java's H2 tests do not establish SQLite correctness.

## Problem and preserved rules

Two patients can both see a free slot and submit before either insertion commits. A conflict query returning no rows provides no existing appointment to lock. Optimistic versions on separate new appointments, a unique start time, and process-local locks do not prevent arbitrary interval overlap across requests/processes.

Preserve half-open intervals [start, end): overlap is `existing.Start < requested.End && requested.Start < existing.End`. Pending and Approved reserve time, Rejected does not. Check the employee across all offerings/branches and the patient across the application. A free start is insufficient: the whole server-computed service interval must fit both opening hours and branch-specific employee availability, without bridging gaps.

## Chosen mechanism

Use SQLite's single writer position, acquired **before reading mutable scheduling/catalog state**, through `SqliteConnection.BeginTransaction(deferred: false)`. Enlist that transaction in the same EF context with `Database.UseTransaction`. Do not translate H2 SELECT FOR UPDATE or claim SQLite has equivalent row locks.

SQLite serializes writes across connections and processes sharing a local database file. BEGIN IMMEDIATE acquires the writer position before the application validates. Enable WAL for reader/writer coexistence, not additional concurrent writers. Use private caches, foreign keys enabled and no read-uncommitted setting.

The application transaction port exposes begin/save/commit operations; its infrastructure implementation contains provider-specific connection/enlistment/disposal details. Services own the transaction boundary. Every participating repository and Identity store must share one scoped ApplicationDbContext/connection; never use a separate Identity context for staff provisioning within a domain command.

## Command sequence

1. Obtain actor ID from the authenticated server-side adapter. Run a cheap role/object-scope precheck using untracked data; denied callers should not acquire a writer position unnecessarily.
2. Open the scoped connection and start a non-deferred SQLite write transaction. Do not start an earlier read transaction and attempt to upgrade it.
3. Enlist EF using the existing connection/transaction; no nested BEGIN or independent connection.
4. Recheck current account/role/grants and object scope. Load current offering, catalog terms, staff active state, hours, availability and reservations **after acquisition**. Prechecks and candidate displays are not authoritative.
5. Reject stale expected CatalogVersion; derive duration/fee/end and snapshots from current server data.
6. Apply full-duration/future/grid/same-local-day/eligibility checks and both conflict checks. Construct the Pending domain appointment, or perform the authorized state/configuration operation.
7. Persist through repositories/EF in the same scope, then commit. Return success only after confirmed commit. Dispose/roll back on any earlier failure.

Use a fresh command scope without previously tracked business entities. Preflight account/grant queries are untracked. Do not carry catalog/schedule entities from a candidate GET into POST, reuse a stale tracker or hold a transaction while the user considers a form. Materialize queries and keep writes short.

## Why competing insertions cannot both pass

For two conflicting commands A and B, only one acquires the writer position first. If A commits a reservation, B subsequently acquires its position and reads A's committed interval before deciding whether to insert. B therefore rejects the overlap. If A rolls back, B can validate against the unchanged schedule. A timeout is a failed attempt, never a successful booking.

This also covers different staff for the same patient and the same staff across branches, provided overlap queries include the full resource scope. Distinct non-overlapping bookings can both succeed, one after the other. Candidate reads remain advisory and do not reserve anything.

This is a reasoned database protocol, **not a claim of executed correctness**. Stage 2 must prove the actual provider/EF behavior and final persisted outcomes.

## All relevant writers use the protocol

| Command | Required fresh checks inside writer scope |
| --- | --- |
| Book | Actor/patient, eligibility, current terms, continuous hours/availability, global employee/patient conflicts. |
| Approve/reject | Actor scope, assignment/grant, current Pending state; audit actor/time written atomically. |
| Hours replacement | Branch/facility scope, valid periods, preservation of future shifts and future/ongoing reserved intervals. |
| Availability add/edit/remove | Facility/staff/branch scope, opening containment, no overlapping employee shifts, preservation of reserved intervals. |
| Catalog revision | Scope, complete new terms, version increment and unchanged historical snapshots. |
| Staff/offering activation changes | Scope and active eligibility; existing history/decisions remain available. |
| Optional new staff login | New Identity account/STAFF role and domain profile together, no takeover or submitted elevated role. |

For booking versus incompatible schedule edits, the earlier valid transaction determines the later command's validation result. Deactivation may follow an existing reservation without deleting it. Catalog edits may follow a booking, but that booking retains an internally consistent old snapshot. If terms changed first, an old booking form must be rejected for review.

## Timeout, versions and errors

- Set the provider's connection/command timeout to five seconds; its normal busy handling supplies a bounded wait. Do not add an unbounded outer retry loop.
- On exhausted contention, roll back/dispose, return a localized temporary-busy response (HTTP 503 where appropriate), and preserve user selections where possible.
- Do not silently replay an uncertain commit. Do not transform every SQLITE_BUSY into a claimed scheduling conflict. SQLITE_LOCKED/shared-connection misuse requires diagnosis rather than blind retry.
- Any failed multi-object write must leave no partial account, profile, reservation or audit state.
- Application-managed catalog and appointment versions additionally detect stale updates. They are not the insertion-conflict mechanism. SQLite has no database-generated SQL Server rowversion.
- Cancellation and exceptions must dispose transaction/connection state; never reuse a failed command tracker for a retry.

## SQLite mappings and deployment boundary

Fees use decimal in the domain and exact integer minor units in SQLite. UTC DateTime instants convert to integer ticks, preserving exact ordering/duration constraints and UTC kind on reload. Local weekly hours and DateOnly/TimeOnly inputs convert through configured Europe/Istanbul. Query comparisons must translate to SQL; no client-side conflict filtering.

Use a local file, migrations, WAL and foreign keys. Keep the database outside wwwroot/version control. The protocol deliberately serializes all writes, reducing throughput but remaining understandable for a university demonstration. WAL is not intended here for a network-share database. Direct SQL bypassing application invariants is outside the guarantee; no production hospital-scale capacity claim is made.

## Required executable evidence

Use migrated temporary **file-backed SQLite**, independent DbContexts/physical connections, separate DI scopes and committed verification reads. Synchronize before BEGIN IMMEDIATE; putting a barrier after acquisition would deadlock the test because only one writer can pass. Use bounded events/latches rather than arbitrary sleeps. No shared DbContext, EF InMemory, enclosing rollback-only transaction or provider-specific H2 fixture is acceptable proof.

1. Empty appointment set; same staff, overlapping different starts: exactly one commits, the other is a business conflict under normal lock timing.
2. Same patient, different staff: exactly one overlapping reservation commits.
3. Same staff, different branches: exactly one overlapping booking commits. Label deliberately inconsistent shift fixtures used only for defensive testing.
4. Back-to-back appointments: both commit without overlap.
5. Competing approve/reject: one transition succeeds with consistent audit, the second sees already-decided state.
6. Booking against availability shrink/removal, hours changes and deactivation: every permitted final state preserves valid reservation/history semantics.
7. Catalog edit against booking: old coherent snapshot if booking won, or stale-term rejection if the edit won; no mixed terms.
8. Overlapping cross-branch shift creation: at most one incompatible shift is accepted. Hours edits against new shifts cannot leave invalid coverage.
9. Hold writer position on one connection; another times out with zero partial writes; after release a fresh attempt succeeds.
10. Assert the loser reads after the winner's commit; verify EF enlisted the intended transaction and current data was used.
11. Run two actual application processes sharing the same local file and verify conflicting bookings remain protected without a process-local mutex.

Inspect final rows, audit fields and interval invariants, not only response strings. Record versions, synchronization strategy, commands and actual results in future stage verification. Tests do not exist yet.

## Primary references

- [SQLite transaction control and BEGIN IMMEDIATE](https://sqlite.org/lang_transaction.html)
- [SQLite isolation and one-writer behavior](https://sqlite.org/isolation.html)
- [Microsoft.Data.Sqlite transactions](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions)
- [BeginTransaction deferred parameter](https://learn.microsoft.com/en-us/dotnet/api/microsoft.data.sqlite.sqliteconnection.begintransaction?view=msdata-sqlite-10.0.0)
- [EF external transaction enlistment](https://learn.microsoft.com/en-us/ef/core/saving/transactions)
- [SQLite provider limitations](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations)
