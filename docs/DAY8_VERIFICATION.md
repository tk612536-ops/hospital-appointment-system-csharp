# Day 8 — authorization after facility-grant revocation

Verified **3 November 2026**, Europe/Istanbul, SDK **10.0.403**. Starting checkpoint: `0a24d1e0eaf6ed931a0146c5a919c6bae95258e3`. Scope: one security scenario, two cases and one authorized checkpoint.

Existing tests cover patient ownership, unrelated staff/managers, forged child resources, disabled accounts and manager-role removal with an old cookie. ManagementScope unit tests also simulate grant changes. They do not exercise actual SQLite facility-grant removal while an authenticated manager retains the MANAGER role and previously issued appointment form, checking both HTTP and appointment-service authorization/data integrity.

Added `GrantRevocationTests.RevokedFacilityGrantDeniesAppointmentAccessWithoutChangingRoleOrAudit`, parameterized for **Pending** and **Approved** appointments:

- A manager signs in, reads an authorized appointment and obtains a valid form token. Only the matching facility grant is then removed in the isolated fixture, using the existing immediate-writer adapter. No account or role is changed; the existing browser session is retained without another login.
- The same browser still reaches the manager workspace and work-list page with **200**. The direct service listing is empty: the revoked facility's appointment is no longer visible.
- The old detail link and decision POST return the configured access-denied redirect; following it returns **403**. The POST uses the original antiforgery token, so this exercises permission denial rather than missing-token rejection.
- Direct `DetailAsync` and `DecideAsync` calls also throw `AccountAccessDeniedException`, proving controller attributes are insufficient by themselves. Authorization denial precedes any already-decided-state error for the Approved case.
- Fresh SQLite scopes strictly compare the complete appointment after denied HTTP and service requests. State, snapshots, version and null/populated decision audit remain unchanged, with exactly one appointment. The owning patient and assigned staff retain their legitimate access.

The first focused run had **2 fixture-setup failures**: a projection used the computed `StaffOffering.Branch` getter without a loaded graph. Replacing that test lookup with the mapped `Service.Department.Branch` path fixed it. The final focused run and full suite passed. No production security defect was found or production code changed.

## Executed checks

```powershell
dotnet restore HospitalAppointments.sln --locked-mode
dotnet build HospitalAppointments.sln --no-restore
dotnet test HospitalAppointments.sln --no-restore --logger "trx;LogFilePrefix=day8-full" --results-directory TestResults
dotnet ef migrations has-pending-model-changes --no-build --project src/HospitalAppointments.Infrastructure --startup-project src/HospitalAppointments.Web
```

Final focused command: `dotnet test tests/HospitalAppointments.IntegrationTests --no-restore --filter FullyQualifiedName~GrantRevocationTests --logger "trx;LogFilePrefix=day8-grant-focused-final" --results-directory TestResults` — **2 passed, 0 failed/skipped**.

Locked restore succeeded. Build succeeded with **0 warnings/errors**. Full suite: **207 passed, 0 failed/skipped** — **31 Domain, 26 Application, 150 Integration**. TRX name comparison retains **all 205 baseline cases**, with exactly two additions. EF reports **“No changes have been made to the model since the last migration.”** Existing test sources, dependencies, migrations and SQLite concurrency protocol are unchanged.

Ignored evidence: initial `TestResults/day8-grant-focused_net10.0_20261103231202.trx`; successful focused `day8-grant-focused-final_net10.0_20261103231234.trx`; full `day8-full_net10.0_20261103231348.trx` (Application), `20261103231349.trx` (Domain), `20261103231443.trx` (Integration). Test databases, reports and generated publish output remain excluded from Git. The owner's Demo data and Java reference were untouched.

This covers grant revocation between requests, not revocation during an in-flight request or a broader penetration test. Grant removal is an administrative test-fixture operation; no public grant editor or unrelated feature was added. Earlier verification documents remain historical. Before the single normal commit/push, verify transport account, parent, staged exclusions and sensitive-content checks; report actual SHA/time, remote verification and clean working-tree status afterward. Never override timestamps, amend or force-push. Stop after the checkpoint.
