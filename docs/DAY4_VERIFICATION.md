# Day 4 — appointment transaction failure regressions

Verified **29 October 2026**, Europe/Istanbul, using SDK **10.0.403**. Scope: a small testing improvement after accepted Stages 0–4, followed by one authorized checkpoint on existing `main`. Starting commit: `22f326ca741d2c964774511cb806a072b3416b1e`. No further development stage is included.

## Gap and implementation

Reviewed appointment booking/decision services, access rules, existing conflict/concurrency/persistence tests, manager history/schedule tests, and invalid-input/state coverage. Existing tests already cover role/facility ownership, forged resources, snapshots after configuration changes, invalid decisions, competing writers and a manually constructed uncommitted INSERT. However, `UncommittedSavedReservationRollsBackAndFreshBookingSucceeds` directly exercises the transaction adapter; it does not prove `AppointmentService.BookAsync` or `DecideAsync` disposes its scope on an exception after a successful save. Decision UPDATE/audit rollback had no corresponding failure-path regression.

Added `tests/HospitalAppointments.IntegrationTests/AppointmentRollbackTests.cs` with three executed cases:

| Test | Evidence |
| --- | --- |
| `BookingFailureAfterInsertRollsBackAndReleasesBothPatientAndEmployee` | Run the real booking service with migrated file-backed SQLite; query the inserted Pending row inside the native transaction; inject an IOException before commit. A separate verification scope sees zero appointments. Fresh bookings by the original patient with another employee and another patient with the original employee both succeed at the failed interval, proving neither retains a phantom reservation. Exactly those two rows persist. |
| `DecisionFailureAfterUpdateRestoresPendingAuditAndReservation(Approved)` | Save the actual approval UPDATE and decision audit/version, then fail before commit. Independent reads recover the exact original patient detail, Pending state, version 1 and null decision audit. A competing booking still conflicts. A fresh assigned-staff decision succeeds with its own actor/time and version 2. |
| `DecisionFailureAfterUpdateRestoresPendingAuditAndReservation(Rejected)` | The same rollback assertions cover rejection, especially preventing an uncommitted rejection from releasing a reserved interval. The later authorized decision commits normally. |

Every failure also leaves `CurrentTransaction` null, the scoped change tracker empty and the connection closed. The test-only decorator delegates acquisition, SaveAsync and disposal to the real `IWriteTransaction`; only CommitAsync is replaced by a deterministic failure after an untracked SQL verification read. Repositories, Identity, domain behavior, EF and SQLite remain real. No mocked database/locking or timing sleeps are used.

No production defect was found in these cases. No production source, transaction protocol, package/lock file or migration was changed. The tests supplement existing independent-connection/cross-process concurrency evidence; fault injection before commit does **not** simulate a disk failure, process crash, cancellation or an uncertain outcome after a durable commit.

## Commands actually executed

```powershell
dotnet --version
dotnet test tests/HospitalAppointments.IntegrationTests --no-restore --filter FullyQualifiedName~AppointmentRollbackTests --logger "trx;LogFilePrefix=day4-rollback-focused" --results-directory TestResults
dotnet restore HospitalAppointments.sln --locked-mode
dotnet build HospitalAppointments.sln --no-restore
dotnet test HospitalAppointments.sln --no-build --no-restore --logger "trx;LogFilePrefix=day4-full" --results-directory TestResults
dotnet ef migrations has-pending-model-changes --no-build --project src/HospitalAppointments.Infrastructure --startup-project src/HospitalAppointments.Web
```

- SDK: 10.0.403.
- Focused run: **3 passed, 0 failed, 0 skipped**.
- Locked restore: successful, all projects up to date.
- Build: successful, **0 warnings, 0 errors**.
- Full suite: **200 passed, 0 failed, 0 skipped**: Domain 31, Application 26, Integration 143. Includes existing separate-connection/configuration races and cross-process booking tests.
- Compared full-run TRX test names to all three retained `stage4-final` reports: **197 baseline cases present, 0 missing, exactly 3 new**. No previous test source was modified.
- EF model check: **“No changes have been made to the model since the last migration.”** All three existing migrations and the snapshot remain unchanged; no new migration file was created.

Ignored local TRX evidence: `TestResults/day4-rollback-focused_net10.0_20261029140703.trx`; full reports `day4-full_net10.0_20261029140730.trx` (Domain), `20261029140731.trx` (Application), `20261029140814.trx` (Integration). These generated reports/databases remain excluded from Git. Stage 4 publish/browser/restart evidence remains historical; those checks were not rerun for this test-only change. The Java reference was not touched or executed.

## Checkpoint scope

Only the new regression test file and current authorization/verification documentation are intended for this checkpoint. README and TEST_PARITY link this evidence; historical stage reports retain their original dates/results. Git transport account, remote parent, staged file exclusions and sensitive-content checks must pass before the single normal commit/push. Actual commit SHA/time and remote verification are reported after Git creates the commit; no timestamp is predicted or overridden. No amendments, force-pushes or additional commits are authorized.
