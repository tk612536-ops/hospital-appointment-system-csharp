# Stage 1 foundation — implementation and verification

Executed on **26 October 2026**, Windows x64, .NET SDK **10.0.403**, ASP.NET Core/.NET runtime **10.0.14**. Stage 1 alone was authorized. No Stage 2 booking or Stage 3 configuration implementation, Git commit or push was performed.

## Implemented result

All four production projects and three test projects exist in HospitalAppointments.sln, with the approved dependency direction. Domain has no EF/ASP.NET references. AccountProfile is an immutable credential-free read snapshot with composed AccountRole values, not a persisted replacement for Identity. StaffMember/Doctor/HealthcareStaff and the appointment model remain Stage 2 work.

HomeController, AccountController and WorkspaceController adapt HTTP to AccountService. Services enforce current identity and role checks, including direct calls. IAccountRepository is implemented by EF AccountRepository, while IAuthenticationGateway delegates hashing/sign-in/sign-out to Identity. HTTP input cannot supply trusted account identity or roles. HttpCurrentActor reads the authenticated account ID. The Web Program composes DI and validates scopes/services at startup.

The initial migration is `20261025214521_InitialIdentity` (EF's identifier uses UTC). It creates the seven Identity tables, normalized username uniqueness, required account metadata and explicit length checks. It has been applied to the local SQLite database. Application startup also applies pending migrations and initializes PATIENT/STAFF/MANAGER roles. WAL, foreign keys and five-second provider timeout are configured. EF reports no pending model changes.

Synthetic patient/staff/manager accounts are seeded only when both the environment is Demo and Demo:Enabled is true. The seeder is not registered outside that condition. Existing accounts, password hashes and roles are preserved on restart. Account/role writes use transactions; they are not a booking-concurrency proof. The initial development database was recreated after correcting test-host isolation, before live verification; final tests use independent files and never touch that database.

Authentication uses Identity password hashing, five-failure/five-minute lockout, security-stamp validation on each authenticated request, Secure/HttpOnly/Lax authentication cookies, secure antiforgery cookies, protected POST login/logout, local return URL checks and generic Turkish credential errors. Disabling an account changes its security stamp and rejects subsequent cookie use. Unsafe MVC actions have automatic antiforgery validation. No public signup/password recovery is exposed.

Razor pages provide Turkish homepage/login, role-specific navigation/workspaces, access-denied and generic-error views. Local Bootstrap 5.3.8 CSS/license, application CSS and favicon are included. Future appointment/configuration capabilities are labeled unavailable and have no working action links.

## Commands and final results

| Executed command | Result |
| --- | --- |
| `dotnet restore HospitalAppointments.sln` | All seven projects restored; lock files generated. |
| `dotnet restore HospitalAppointments.sln --locked-mode` | All seven projects restored against locked dependency graphs. |
| `dotnet build HospitalAppointments.sln --no-restore` | Build succeeded; **0 warnings, 0 errors**. |
| `dotnet ef migrations add InitialIdentity --project src/HospitalAppointments.Infrastructure --startup-project src/HospitalAppointments.Web --output-dir Migrations` | Initial migration/designer/snapshot generated. |
| `dotnet ef database update --project src/HospitalAppointments.Infrastructure --startup-project src/HospitalAppointments.Web` | Initial migration applied. |
| `dotnet ef migrations has-pending-model-changes --no-build --project src/HospitalAppointments.Infrastructure --startup-project src/HospitalAppointments.Web` | No changes since the last migration. |
| `dotnet test HospitalAppointments.sln --no-restore --logger "trx;LogFilePrefix=stage1-verified" --results-directory TestResults` | **57 passed, 0 failed, 0 skipped**. |

| Test project | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| HospitalAppointments.Domain.Tests | 8 | 0 | 0 |
| HospitalAppointments.Application.Tests | 13 | 0 | 0 |
| HospitalAppointments.IntegrationTests | 36 | 0 | 0 |
| **Total** | **57** | **0** | **0** |

Final ignored TRX reports: `TestResults/stage1-verified_net10.0_20261026005546.trx`, `...005547.trx` and `...005553.trx`. The accompanying command output is `TestResults/stage1-verified-test.log`. These are actual C# results and do not mean the historical 118 Java cases were all implemented. The parity matrix retains all 118 reference rows and identifies 31 foundation rows with current evidence; multiple reference rows may share a C# theory.

Earlier test runs exposed a fixture-constructor issue and late application of test-host configuration. These were fixed: the fixture has one public constructor, test settings are applied as early host settings, and a regression assertion checks the actual SQLite connection DataSource equals the intended isolated path. Test cancellation-token warnings were fixed rather than suppressed. The final run above is green.

## Coverage exercised

- Domain identity/name/role invariants, immutable role composition and framework independence.
- Application input bounds, trimming, actor-derived identity and direct service role denials.
- Actual migrated file-backed SQLite schema, foreign keys/WAL, DI resolution and absence of appointment entities.
- Real Identity forms/cookies for all three roles, own-role access and other-role denial, local redirects, missing/invalid credentials and no password redisplay.
- Missing antiforgery token rejection, cross-browser token rejection, POST-only logout and cleared authentication after logout.
- Hash verification, weak-password/oversized-user rejection, normalized username uniqueness at the manager and database levels, multi-role accounts and denial of unassigned accounts.
- Lockout after five failures, rejection during lockout and successful login after expiry; disabled-user cookie invalidation and login denial.
- Seeder idempotency; two separate host lifetimes sharing one file preserve IDs, changed password hash and migration history.
- Demo registration/data/banner absent in fresh Production/Development databases even if Demo:Enabled is true, and in Demo when the setting is false.
- Local assets, Turkish markup/keyboard landmark, unavailable booking/configuration routes, inaccessible database URL and unknown/H2-console routes.

No appointment/availability/concurrency test is claimed. Bootstrap rendering checks and HTML assertions are targeted, not an exhaustive browser/mobile/accessibility certification.

## Running Kestrel smoke checks

Started the real app in Demo on `https://localhost:7143` and `http://localhost:5143`. Fresh startup applied the migration and seeded three synthetic accounts. Actual HTTP requests submitted the rendered antiforgery tokens, used real cookies, loaded each role's page (**200**), tried another role (**403** after the access-denied redirect), and logged out; subsequent protected access redirected to login for patient, staff and manager.

GETs returned **200** for homepage, Bootstrap CSS (**232111 bytes**), application CSS (**4033 bytes**) and favicon (**185 bytes**). Homepage markup is Turkish and explicitly identifies the foundation stage. The background launcher PID is recorded in ignored `TestResults/stage1-app.pid`; stdout/stderr are in `stage1-app.log` and `stage1-app-error.log`.

The localhost development certificate exists but is **not trusted** on this machine. Live HTTP smoke requests used PowerShell's SkipCertificateCheck solely for these known localhost requests. No browser security interstitial was bypassed and no trust setting was changed. Before using the app in a browser, run `dotnet dev-certs https --trust` yourself and accept the trust prompt. Secure cookies intentionally require HTTPS.

See [README](../README.md) for exact launch/account/manual-test instructions. Outside Demo, a previously populated database is not scrubbed; use a separate file to test an empty non-demo environment.

## Boundaries and remaining work

Booking, approval, staff hierarchy, facilities, offerings, schedules, object/facility grants and their SQLite race proof remain future authorized stages. Only role-level/account checks exist now because no appointment/facility objects exist. Identity bootstrap runs local migrations at startup; this is a classroom deployment choice, not production migration orchestration. Data Protection uses the platform's standard local key storage.

The original Java project was not modified or run. The original assignment hash remains the planning audit's value. Git retains the one original Stage 0 commit `48d924fb90b8a5d549c46bec1491c1b9b301cc7e`; Stage 1 changes are local/uncommitted, with no push. Both stages occurred on 26 October 2026 and are one actual calendar development day. Stage 1 ends here; Stage 2 and any new commit/push require explicit approval.
