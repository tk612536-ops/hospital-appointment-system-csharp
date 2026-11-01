# Day 6 — fresh installation and deployment reliability

Verified **1 November 2026**, Europe/Istanbul, SDK **10.0.403**. Baseline: `74ac7f0833af65379bec154d13306b1a12497b3e`, 202 passing cases. Scope is one focused quality improvement and one authorized checkpoint on existing `main`.

## Gap and improvement

Reviewed Program startup, DatabaseInitializer, both Demo seeders, the deployment guide, and foundation/restart/demo/cross-process tests. Foundation and demo lifecycle tests initialize through WebApplicationFactory. The existing cross-process test starts the source Debug/Release build with a database previously initialized by that fixture. Stage 4 has historical manual published-app evidence, but there was no automated regression for first launch of the published output from a different working directory against a nonexistent database.

Added one end-to-end case: `PublishedInstallationTests.PublishedReleaseInitializesItsOwnDatabaseAndPreservesChangesAcrossProcessRestart`.

1. Publish the real Web application in Release/framework-dependent mode, without dependency restore, into a unique `TestResults/published/<guid>/published app` directory. A space in the path also exercises argument handling. Assert that no database is packaged.
2. Start a real Kestrel child process from a separate `launcher` directory, supplying the absolute DLL/content root, Demo environment, a relative `data/clinic.db` path and a free loopback HTTPS port. No source/user database is supplied or modified.
3. Verify the new database appears under the published content root and all three migration IDs are applied, with none pending. Assert complete expected seed counts: seven accounts, three roles, seven role associations, two facilities, three branches, three employees, five offerings, seventy availability rows, forty-two opening periods, one seed marker and the initial sample appointment.
4. Compare HTTP responses byte-for-byte with the published Bootstrap and application CSS files. Real patient/staff/manager logins verify packaged Demo settings and authentication. Book and approve an appointment through the real antiforgery-protected forms on the last seeded future date; capture saved appointments and availability.
5. Stop the first process, change only the isolated patient's password hash with ASP.NET Core PasswordHasher and a newly generated synthetic password, then start another process against the same published root/file. The old password is rejected; the changed one authenticates. Verify the same account ID/hash, exact appointment/availability values, migration history and seed counts. The patient still sees Approved. No database is created under the unrelated launcher directory.

The helper drains output asynchronously, bounds publish/startup/exit waits, disables build-server reuse during publish and stops only its owned child processes. The restart uses process termination, so this is also an abrupt-stop/reopen check rather than a graceful-shutdown claim. It adds no production endpoint/framework/dependency or locking mechanism.

The initial focused harness attempt was stopped before app launch: reusable MSBuild child nodes kept redirected output open after publish exited. No test result was claimed for that attempt. The helper was corrected with `--disable-build-servers` and bounded stream waits; the subsequent focused run and full suite passed. No production defect was found.

## Executed verification

```powershell
dotnet restore HospitalAppointments.sln --locked-mode
dotnet build HospitalAppointments.sln --no-restore
dotnet test HospitalAppointments.sln --no-restore --logger "trx;LogFilePrefix=day6-full" --results-directory TestResults
dotnet ef migrations has-pending-model-changes --no-build --project src/HospitalAppointments.Infrastructure --startup-project src/HospitalAppointments.Web
```

The successful focused command was `dotnet test tests/HospitalAppointments.IntegrationTests --no-restore --filter FullyQualifiedName~PublishedInstallationTests --logger "trx;LogFilePrefix=day6-published-focused" --results-directory TestResults`.

Each successful test invocation actually executes `dotnet publish src/HospitalAppointments.Web -c Release --no-restore --disable-build-servers --self-contained false -o <unique-published-directory>` and launches/restarts that published DLL. Both publishes and all four app launches completed successfully; no existing artifacts/release folder or user Demo database was reset.

- Locked restore: successful, all projects up to date.
- Solution build: successful, **0 warnings, 0 errors**.
- Focused regression: **1 passed, 0 failed, 0 skipped** (9 seconds reported).
- Full suite: **203 passed, 0 failed, 0 skipped**: **31 Domain, 26 Application, 146 Integration**. Includes the existing concurrency/cross-process, Day 4 rollback and Day 5 demo lifecycle cases.
- TRX name comparison against the retained Day 5 full reports: **202 previous cases present, 0 missing, exactly 1 new**. Existing test sources are unchanged.
- EF: **“No changes have been made to the model since the last migration.”** No migration/snapshot, production source, package or lock file changed. The SQLite immediate-writer protocol remains unchanged.

Ignored evidence: `TestResults/day6-published-focused_net10.0_20261101221546.trx`; full reports `day6-full_net10.0_20261101221659.trx` (Domain), `20261101221700.trx` (Application), `20261101221747.trx` (Integration). Isolated published output/databases remain under TestResults; they are not delivery archives or tracked files.

## Documentation and limits

RUN_ON_WINDOWS had an inaccurate instruction that Stage 4 was still unpublished. It now directs readers to current GitHub main and identifies old archives as dated artifacts. It also provides the tested explicit-content-root launch command for shortcuts/other working directories, explaining relative database resolution. README/TEST_PARITY/AGENTS record the current verification and limited authorization; historical stage reports remain dated evidence.

This was executed on the current Windows computer with the installed SDK/runtime and localhost development certificate. The automated loopback HTTP client accepts only its localhost test host without changing machine trust; it is not browser trust verification. Another physical computer, IIS, self-contained packaging, a fresh OS/runtime installation, visual UI checks and SABIS submission were not tested today. The regression needs the SDK to publish; the app itself remains framework-dependent. No Java reference operation was performed.

Before the single normal commit/push, verify the actual Git transport account, parent, staged diff and exclusions/sensitive-content scan. Do not override dates, amend or force-push. Report the resulting SHA/time and remote file/tree/clean-working-tree verification after the commit exists, then stop.
