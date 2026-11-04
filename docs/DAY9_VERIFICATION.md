# Day 9 — live demonstration and presentation reliability

Verified **4 November 2026**, Europe/Istanbul, SDK **10.0.403**. Starting checkpoint: `50845c27080dd7433032ae1a95e207e78bf5e687`. Scope: classroom preparation, an isolated demonstration and one authorized checkpoint.

## Review and meaningful documentation changes

Read PRESENTATION_GUIDE_TR, RUN_ON_WINDOWS, REQUIREMENTS_AUDIT and README, comparing their instructions with current controller routes, Razor labels/forms, domain methods, booking/scheduling services and the SQLite adapter. The presentation guide still cited 197 tests, two commits and unpublished Stage 4 work. Its generic live route omitted the explicit **Saatleri göster** refresh, exact form actions/results, a timed short route, reliable expired-calendar recovery and concrete startup troubleshooting. Corrected its description of SqliteWriteTransaction as an implementation of IWriteTransaction and clarified that password input forms carry the secret over HTTPS, while read DTOs do not expose it.

Updated the Turkish guide with an 8–10 minute speaking plan, exact manager/patient/new-doctor actions and expected results, future date/weekday calculation, a separate Demo database command, same-file restart instructions and a five-minute fallback. New doctor account creation is explicitly required for this particular demonstration. Existing UML/architecture diagrams remain; OOP examples link to actual C# files and methods. Added accurate Q&A for current test evidence, grant revocation, repeated decisions and SQLite limits, plus a troubleshooting table. Human interaction timing is a target, not a measured browser result. README links the current rehearsal; earlier dated audit/verification documents remain historical.

## Actual demonstration executed

Executed the ignored local script `TestResults/day9-live/rehearse.ps1` against the **current Debug source build in a real Kestrel process**, using normal HTTPS certificate validation at **https://localhost:7249**. Each run creates a new GUID database path. No browser/UI automation, visual screenshots or keyboard check was performed.

| Step | Actual result |
| --- | --- |
| Fresh startup and manager login | Isolated Demo host became ready; real Identity login and antiforgery-protected forms succeeded. |
| Clinic, first branch, department, service | Created **Sunum Kliniği 170851**, facility **3**, branch **4 / Merkez Sunum**, **Genel Bakım**, service **6 / Sunum muayenesi**, **30 minutes / 775.50 TRY**. |
| Doctor/specialty/offering | Created **Dr. Sunum Deniz**, doctor **4**, login **sunum-doktor-170851**, new synthetic specialty and offering **6**. No password is recorded in this report. |
| Hours and dated availability | **6 November 2026, Friday**, opening periods **09:00–12:00 / 13:00–17:00**, doctor window **71 / 09:00–12:00**. |
| Patient booking | Logged out as manager, logged in as patient and booked **10:00–10:30**; `/appointments/2` showed **Onay Bekliyor**. |
| Assigned doctor decision and patient return | Logged out/in as the new doctor, approved through the form, then logged out/in as patient. Same appointment showed **Onaylandı**, 775,50 TRY and the expected end time. |
| Unsafe schedule change | Manager attempted an end of **10:15**. Received **409**; a fresh schedule GET retained **12:00**. |
| Process restart and persistence | Stopped owned process **10360**, started separate process **19960** on the same file. Patient still saw the same Approved appointment/terms; manager still saw the clinic and original shift. Unsafe edit again returned **409**; the new doctor's login/details still worked. |

The second host was stopped in the script's finally block. Local evidence: `TestResults/day9-live/f12961a5592943ba8d9ad02a1afa9f05/result.json`, `host-1.log`, `host-2.log` and isolated `clinic.db`. These are ignored generated artifacts. IDs/date belong to this run, not to the owner's database or a future classroom run. All demonstration data was synthetic.

## Executed verification

```powershell
dotnet restore HospitalAppointments.sln --locked-mode
dotnet build HospitalAppointments.sln --no-restore
dotnet test HospitalAppointments.sln --no-restore --logger "trx;LogFilePrefix=day9-full" --results-directory TestResults
dotnet ef migrations has-pending-model-changes --no-build --project src/HospitalAppointments.Infrastructure --startup-project src/HospitalAppointments.Web
```

- Locked restore succeeded. Build succeeded with **0 warnings, 0 errors**.
- Full suite: **207 passed, 0 failed, 0 skipped** — **31 Domain, 26 Application, 150 Integration**. Actual TRX name comparison with Day 8: **207 retained, zero additions/removals**.
- EF: **“No changes have been made to the model since the last migration.”** All existing migrations/snapshot remain unchanged.
- The unchanged PublishedInstallationTests case executed its Release/framework-dependent publish and child-process restart check successfully as part of this full suite. No deployment/packaging code changed; no additional standalone publish or archive refresh was needed.
- All code links in the revised Turkish guide resolve to real repository files. Form labels/method examples were checked against source.

Full TRX evidence remains local: `TestResults/day9-full_net10.0_20261104171114.trx` (Domain), `20261104171115.trx` (Application), `20261104171204.trx` (Integration). No real application defect was found, so no production code or tests were changed or duplicated. Architecture, dependencies and SQLite concurrency guarantees remain unchanged. The Java reference and owner's Demo database were untouched.

## Limits and checkpoint boundary

This is an actual HTTP-form rehearsal on the current Windows computer, not a visual/browser rehearsal or a measured classroom presentation. The expired-calendar fallback uses existing behavior already covered by Day 5's aged-restart regression; it was not repeated with a deliberately aged database today. A second computer, SABIS submission and attendance are separate obligations. Old Stage 4 archives are not refreshed by source commits.

Only presentation/current-status documentation is intended for this checkpoint. Verify the actual Git transport account, staged diff, exclusions/sensitive-content scan and remote parent before one normal commit/push. Use the actual clock without amendments, date overrides or force-push. Report the resulting SHA/time, complete remote tree and clean working-tree status after verification, then stop.
