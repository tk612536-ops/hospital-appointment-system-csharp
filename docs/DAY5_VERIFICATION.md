# Day 5 — reliability and demo readiness

Verified **30 October 2026**, Europe/Istanbul, using SDK **10.0.403**. Starting checkpoint: `8c9b367503b23968c0276b703c5dd65f90d50d2d`. The owner authorized a focused quality improvement and one genuine commit/push on existing `main`, without another development stage.

## Selected gap

Reviewed the booking time/availability rules, demo seeders and startup transactions, existing authorization/input tests, restart tests and Windows rehearsal instructions. `BookingPersistenceTests.RenamedSeedAndDecidedAppointmentSurviveRestartWithoutMovingDates` restarts with the same injected clock; account and manager workflow restart tests also do not advance beyond the fixture horizon. They do not establish that an expired demo retains history without silently repopulating its calendar, or that the documented management extension is sufficient to resume the presentation. The existing timezone test uses an instant when UTC and Istanbul share a date; it does not test first-install seeding across that date boundary.

Added two integration cases in `tests/HospitalAppointments.IntegrationTests/DemoLifecycleTests.cs`:

| Test | Executed evidence |
| --- | --- |
| `FreshDemoSeedsTomorrowInIstanbulWhenUtcDateIsStillYesterday` | Start an isolated, migrated Demo host at synthetic **7 January 2030 22:30 UTC**, already **8 January 01:30 in Istanbul**. The complete distinct availability-date sequence is **9–22 January**, all starts are future, and the sample appointment is on 9 January. A real patient login and booking GET show the expected 10:30 candidate. Using the UTC date for “tomorrow” would fail this test. |
| `AgedDemoRestartPreservesHistoryAndManagerCanExtendWithoutReset` | Create and approve an appointment, stop the host, and restart the same file at synthetic **1 February 2030**, after every original availability interval has expired. Strict comparisons of all public saved appointment/availability values retain IDs, intervals, snapshots, state, version and audit; seven accounts, two facilities and one `booking-v1` marker remain. A future date has no starts and a forged booking POST receives **409**. The patient still sees the historical Approved appointment. A real manager form adds **2 February 09:00–11:00** availability; the patient books **10:30–11:00**, assigned staff approves, and the patient sees Approved. Fresh database queries confirm the new UTC interval/status, exactly one additional availability row and unchanged old appointment/windows. |

Both tests use injected time, real application hosts, Identity cookies, antiforgery forms and migrated temporary file-backed SQLite. The second test disposes the initial host before reopening its file. There are no sleeps, calendar-date dependencies on the execution day, mocked persistence or manually bypassed booking/configuration commands.

No production defect was found. No production source, schema/snapshot, migrations, dependencies or lock files changed. All existing test sources are untouched. The SQLite immediate-writer protocol is unchanged. The Windows guide now gives the tested no-reset extension steps and replaces its stale two-commit/Stage-4-publication status with instructions to inspect actual history. README, TEST_PARITY and AGENTS record the current evidence/limited authorization.

## Executed verification

```powershell
dotnet --version
dotnet test tests/HospitalAppointments.IntegrationTests --no-restore --filter FullyQualifiedName~DemoLifecycleTests --logger "trx;LogFilePrefix=day5-demo-focused" --results-directory TestResults
dotnet restore HospitalAppointments.sln --locked-mode
dotnet build HospitalAppointments.sln --no-restore
dotnet test HospitalAppointments.sln --no-build --no-restore --logger "trx;LogFilePrefix=day5-full" --results-directory TestResults
dotnet ef migrations has-pending-model-changes --no-build --project src/HospitalAppointments.Infrastructure --startup-project src/HospitalAppointments.Web
```

- SDK: **10.0.403**.
- Focused run: **2 passed, 0 failed, 0 skipped**.
- Locked restore: successful, all projects up to date.
- Solution build: successful, **0 warnings, 0 errors**.
- Full suite: **202 passed, 0 failed, 0 skipped**: **31 Domain, 26 Application, 145 Integration**. Includes the existing SQLite races/cross-process and Day 4 rollback tests.
- Compared actual Day 5 full-run TRX case names with the retained Day 4 full reports: **200 previous cases present, 0 missing, exactly 2 new**.
- EF consistency: **“No changes have been made to the model since the last migration.”** The existing three migrations and snapshot are unchanged; no new migration was introduced.

Ignored local evidence: `TestResults/day5-demo-focused_net10.0_20261030223305.trx`; full reports `day5-full_net10.0_20261030223402.trx` (Application), `20261030223403.trx` (Domain), `20261030223447.trx` (Integration). Test databases/reports remain local generated artifacts. No Java reference operation was performed.

This is automated host/HTTP evidence on the current Windows computer. No visual browser rehearsal, Release publish/archive refresh, second-computer execution, SABIS submission or production reliability claim is added today. Existing Stage 4 evidence remains historical. Restart preserves an aged calendar deliberately; future availability must be added explicitly rather than silently moving saved appointments.

## Checkpoint boundary

The checkpoint contains only the new test file and related documentation. Verify the actual Git transport identity `tk612536-ops`, unchanged remote parent, exclusions and sensitive-content scan before committing. Use the actual Git timestamp, one normal push and no amendment/force-push. The resulting commit SHA/time, remote file verification and clean-tree status are reported after the commit exists; no timestamp is predicted or overridden. Stop after this checkpoint.
