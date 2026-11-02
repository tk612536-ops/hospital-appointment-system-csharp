# Day 7 — stale decision form reliability

Verified **2 November 2026**, Europe/Istanbul, SDK **10.0.403**. Starting checkpoint: `ba172425d74298ae9e121bf3b8bce12fcc8bdaf7`. Scope: one regression scenario, two cases, and one authorized checkpoint using the actual clock (including 3 November if necessary).

Existing domain/service tests reject invalid second decisions; the concurrency test checks one winner and non-null audit. They do not prove that replaying an old real HTTP form leaves the winning actor/time/version unchanged. Same-clock checks could miss an audit timestamp rewrite.

Added `DecisionReplayTests.StaleDecisionFormCannotRewriteCommittedStateOrAudit`, parameterized for **Approved** and **Rejected**:

- Assigned staff opens a real Pending decision form and retains its antiforgery token. A separately authenticated, facility-authorized manager commits the decision.
- A fresh SQLite read verifies the manager's account ID, decision instant and version 2.
- Advance injected time by five minutes before each replay. Using the original staff token, submit the same decision and then the opposite decision. Both receive **409** with the already-decided message.
- After each failed POST, a separate scope strictly compares the entire saved appointment, including snapshots, original audit and version. `SingleAsync` also rejects unexpected extra rows. Refreshed staff details hide decision controls; the patient still sees the committed state.

No production defect was found. Production code, existing tests, ownership rules, dependencies, migrations and SQLite immediate-writer protocol are unchanged. All fixtures use isolated migrated SQLite files; the owner's Demo data and Java reference were untouched. No new idempotency workflow or feature was introduced.

## Executed checks

```powershell
dotnet restore HospitalAppointments.sln --locked-mode
dotnet build HospitalAppointments.sln --no-restore
dotnet test HospitalAppointments.sln --no-restore --logger "trx;LogFilePrefix=day7-full" --results-directory TestResults
dotnet ef migrations has-pending-model-changes --no-build --project src/HospitalAppointments.Infrastructure --startup-project src/HospitalAppointments.Web
```

Focused command: `dotnet test tests/HospitalAppointments.IntegrationTests --no-restore --filter FullyQualifiedName~DecisionReplayTests --logger "trx;LogFilePrefix=day7-replay-focused" --results-directory TestResults` — **2 passed, 0 failed/skipped**.

Locked restore succeeded. Build succeeded with **0 warnings/errors**. Full suite: **205 passed, 0 failed/skipped** — **31 Domain, 26 Application, 148 Integration**. TRX name comparison retains **all 203 baseline cases**, with exactly two additions. EF reports **“No changes have been made to the model since the last migration.”** The existing published-installation and concurrency regressions also passed unchanged.

Ignored evidence: `TestResults/day7-replay-focused_net10.0_20261102234303.trx`; full reports `day7-full_net10.0_20261102234921.trx` (Application), `20261102234922.trx` (Domain), `20261102235018.trx` (Integration). Reports, test databases and generated publish output remain excluded from Git.

This verifies sequential stale-form replay after a committed decision; earlier tests retain the separate simultaneous-decision/concurrency evidence. README and earlier reports remain dated historical evidence. Before the single normal commit/push, verify transport account, parent, staged exclusions and sensitive-content checks. Report actual SHA/time, remote verification and working-tree status afterward; never override timestamps, amend or force-push. Stop after the checkpoint.
