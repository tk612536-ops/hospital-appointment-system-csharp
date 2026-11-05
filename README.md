# Klinik Randevu — C# appointments and facility management

University Object-Oriented Programming project using C#/.NET 10, ASP.NET Core MVC, EF Core/SQLite, ASP.NET Core Identity, Razor Views and local Bootstrap. UI text is Turkish; code and technical documentation are English.

**Stages 1–4 are implemented and reviewed.** Patients book eligible employee/service intervals and follow their approval status. Assigned staff and scoped managers approve/reject. Managers configure hospitals/clinics, branches, departments, services, both staff types, specialties, offerings, hours and dated availability. Configuration changes preserve reservations/history. The abstract StaffMember hierarchy and layered architecture remain intact. The professor's C# instruction governs this rebuild; the Java reference is untouched.

The Stage 1–3 checkpoint is [abebdb2](https://github.com/tk612536-ops/hospital-appointment-system-csharp/commit/abebdb2d0142ea883af83aa1475b385874d22e79); the independently reviewed Stage 4 checkpoint is [22f326c](https://github.com/tk612536-ops/hospital-appointment-system-csharp/commit/22f326ca741d2c964774511cb806a072b3416b1e), published on 28 October 2026. **Day 9 verification on 4 November has 207 passing tests (31 domain, 26 application, 150 integration), retaining every Day 8 case.** A complete isolated Kestrel/HTTPS form rehearsal verified manager-created configuration, a new doctor's approval, patient confirmation, unsafe-edit rejection and restart persistence. The [Turkish presentation guide](docs/PRESENTATION_GUIDE_TR.md) now provides an 8–10 minute route, exact buttons/results, expired-calendar fallback and troubleshooting. This was HTTP verification; no visual/browser test was performed today. See [Day 9 evidence](docs/DAY9_VERIFICATION.md), [Day 8 authorization evidence](docs/DAY8_VERIFICATION.md), [Day 6 published-installation evidence](docs/DAY6_VERIFICATION.md), [historical Stage 4 verification](docs/STAGE4_VERIFICATION.md), [teacher audit](docs/REQUIREMENTS_AUDIT.md), [architecture review](docs/ARCHITECTURE_REVIEW.md), and [another-Windows-computer setup/delivery](docs/RUN_ON_WINDOWS.md).

## Run on Windows

From the extracted or cloned folder that contains `HospitalAppointments.sln`, with .NET SDK 10.0.403 or a compatible patch installed:

```powershell
dotnet tool restore
dotnet restore HospitalAppointments.sln --locked-mode
dotnet build HospitalAppointments.sln --no-restore
dotnet test HospitalAppointments.sln --no-build --no-restore
```

The local development HTTPS certificate must be trusted by your browser. If needed, run this yourself and accept the certificate-trust prompt:

```powershell
dotnet dev-certs https --trust
```

Start the explicit synthetic demo:

```powershell
dotnet run --no-launch-profile --project src/HospitalAppointments.Web -- --environment Demo
```

Open [https://localhost:7143](https://localhost:7143). HTTP port 5143 redirects to HTTPS. HTTPS is required because authentication and antiforgery cookies are Secure. Do not disable browser certificate warnings; trust the development certificate instead. Initial dependencies need network access; runtime CSS and fonts do not require a CDN. Stop the foreground application with Ctrl+C.

Stop a running application before rebuilding on Windows, to avoid locked DLLs. The Stage 4 live rehearsal uses a separate published folder/database; see the verification report rather than assuming its observed appointment IDs belong to the source default database.

## Synthetic demo accounts

These credentials are intentionally public and work only when demo identities exist in the selected database:

| Username | Workspace | Demo password |
| --- | --- | --- |
| patient | Hasta | KlinikDemo10! |
| staff | Personel | KlinikDemo10! |
| manager | Yönetici | KlinikDemo10! |
| patient2 | Second patient / conflict demonstration | KlinikDemo10! |
| lab | Hospital laboratory employee | KlinikDemo10! |
| clinicstaff | Single-branch clinic employee | KlinikDemo10! |
| clinicmanager | Manager granted only the separate clinic | KlinikDemo10! |

The public demo password is a seed constant. Identity stores password hashes in SQLite. Five incorrect password attempts lock an account for five minutes. Login trims surrounding whitespace and accepts case-insensitive ASCII usernames. The demonstration banner appears only when both the environment is `Demo` and `Demo:Enabled` is true.

Seeding is idempotent: existing accounts, hashes, passwords, enabled states and roles are not reset. Stage 2 fixtures use a stable `booking-v1` database marker independent of editable facility names. They create a two-branch Demo Bakım Hastanesi, one-branch Demo Klinik, doctor/laboratory offerings, manager grants, two patients and fourteen initial future dates beginning tomorrow. Restarts never move or duplicate appointments or shifts. `Demo:SeedBookings=false` enables foundation-only demonstration accounts without booking fixtures (used by the preserved Stage 1 tests).

Outside the explicitly enabled Demo environment, demo seeders are not registered and no demo users are created. Required role names are initialized in all environments. Old saved fixture dates do not advance automatically; managers can add suitable future availability or deliberately reset an unwanted synthetic database. Existing future shifts may require opening hours on the relevant weekday before new availability is added.

Switching environment does not erase demo users already stored in a database. To verify an empty non-demo host, use a separate local file:

```powershell
dotnet run --no-launch-profile --project src/HospitalAppointments.Web -- --environment Production --Database:Path data/non-demo.db --urls https://localhost:7143
```

It creates schema and roles, but no users. Public registration/account administration is intentionally absent. This local classroom configuration is not an internet deployment recipe.

## Manual booking and approval walkthrough

1. Sign in as `patient` and choose **Randevu al**. Select Demo Bakım Hastanesi → Kuzey → Genel muayene → Dr. Deniz, and an initially seeded future date.
2. A 30-minute appointment at **10:30** ends exactly at **11:00** and is valid. **10:45 is omitted** because it would end after the doctor's shift. Choose another free date/time if the interval was already used in a rehearsal.
3. Select a displayed start. Details show **Onay Bekliyor**, server-derived end/duration/fee and the saved names. Pending already reserves employee and patient time.
4. Log out, sign in as `staff`, open **Randevu değerlendirme**, inspect that appointment and choose **Onayla**.
5. Log back in as `patient`; **Randevularım** and details now show **Onaylandı** and the original booked terms.
6. For rejection, create another Pending appointment and use **Reddet** as its assigned staff or granted manager. The interval becomes bookable again.
7. Book the 20-minute laboratory procedure with Ece to demonstrate the non-doctor subtype. Its fee is an invented **250.25 TRY**, stored as exactly **25025** minor units.
8. `manager` sees hospital appointments; `clinicmanager` sees only the separate clinic. `lab` cannot decide Dr. Deniz's appointments. `patient2` cannot open the first patient's details. Hidden controls are supplemented by service authorization.
9. Restart with the same database: bookings, decisions, IDs and future shifts remain. Management edits preserve booked service/staff names, duration, fee and currency. Facility/branch names and professional descriptions remain current linked labels.

All times are Europe/Istanbul. The start grid is 15 minutes, durations need not be multiples of it, and intervals are half-open. Full continuous coverage is required in both opening hours and matching branch availability. Employee overlaps span branches; patient overlaps span the application. Rejected appointments release time; there is no cancellation/rescheduling/expiry workflow.

Booking selection is keyboard-friendly and explicit: after changing a parent institution/branch/service, press **Uygun saatleri göster** to refresh dependent choices and starts. Changing an option does not automatically navigate. The skip link targets main content; forms have labels, statuses retain text alongside color, and wide tables scroll within focusable regions on narrow screens. Native date/time controls can follow the browser's own format.

## Manual manager-to-patient walkthrough

1. Sign in as `manager`, select **Kurum yönetimi**, and create a hospital or clinic with its first branch. The creator receives management access atomically. Add another branch if needed, then create a department in the intended branch.
2. Open **Hizmetler ve ücretler**. Create a 30-minute service at `775,50` TRY. Turkish fee fields use a comma and no thousands separators; dot/grouped/scientific input and fractions below one kuruş are rejected rather than silently rounded or regrouped.
3. Open **Çalışanlar ve atamalar**. Add a specialty, register a synthetic doctor, and optionally specify a new username and a strong password (8–128 characters, upper/lowercase, digit, symbol). Both credential fields may instead remain empty. A new account receives STAFF only; an existing account cannot be attached/taken over. Register healthcare workers with a job title through the neighboring form.
4. Assign the doctor to the service. That offering also assigns the branch; repeat with other services/branches as appropriate. Specialty alone does not authorize a service.
5. Open **Saatler ve uygunluk**. Select the branch and weekday of a future date, then press **Saatleri göster** to load that day. Set `09:00–12:00` and `13:00–17:00`, leaving the other pairs blank. Save. Blank pairs are ignored; all pairs blank closes that weekday. Then add that doctor's availability on the chosen future date, `09:00–12:00`.
6. Sign in as `patient`, select the new facility/service/doctor/date and reserve a free 30-minute start. Sign in with the newly created doctor credentials and approve. Return as the patient and verify **Onaylandı**.
7. As manager, try shortening the covering shift so it ends halfway through that appointment, or removing it. The localized error returns HTTP 409 and the persisted interval stays unchanged. Compatible edits work. Opening-hour reductions also preserve future unbooked shifts.
8. Edit the service duration/price, or deactivate the service, employee or offering. Existing booked terms/history and authorized decisions remain. Deactivation blocks new bookings. Reactivation requires the relevant staff/service to be active. Resource links cannot be moved to another department/branch/facility.
9. Restart the app with the same database. New configuration, Identity hashes/roles and the Approved reservation remain; seeding does not overwrite them.

These pages use native forms/details and local Bootstrap; no CDN or JavaScript framework is needed. Credential values are never re-rendered on validation errors. Errors provide a link back to management; unsaved management form entries are not restored automatically.

## Projects and responsibilities

| Project | Implemented responsibility |
| --- | --- |
| HospitalAppointments.Domain | Encapsulated facilities/catalog/staff/patient/appointment model, immutable intervals and credential-free account snapshots. |
| HospitalAppointments.Application | Booking/approval/account services; ManagementScope, Facility/Catalog/Staff/ScheduleManagementService, ManagementQueryService; repository/security/transaction ports. |
| HospitalAppointments.Infrastructure | EF mappings/migrations, scoped booking/management repositories, unchanged immediate-writer protocol, Identity adapters/new STAFF provisioning and explicit synthetic seeders. |
| HospitalAppointments.Web | Thin account/workspace/booking/appointment and four management controllers, separate inputs/DTOs, strict Turkish fee parsing and Turkish Razor Views. |
| HospitalAppointments.Domain.Tests | Account/interval/state/fee/ownership/snapshot/polymorphism invariants. |
| HospitalAppointments.Application.Tests | Identity boundaries, authorization and deterministic scheduling policies. |
| HospitalAppointments.IntegrationTests | Real SQLite persistence, HTTP ownership/antiforgery, separate-connection races, separate-process Kestrel race, busy rollback and restart. |

Controllers adapt HTTP to application services. Booking/decision/management services authorize and coordinate domain behavior through repository and transaction ports; EF queries stay in Infrastructure. Identity handles credentials through authentication/provisioning ports. New staff user/STAFF role/profile writes share one transaction. Doctor and HealthcareStaff override ProfessionalDescription; choices, mixed staff management lists and appointment details use it through StaffMember. The editor distinguishes real subtype fields; common booking has no staff-type switch. Roles are composed onto accounts.

Booking acquires SQLite's immediate writer position **before** reading current catalog/schedule/conflict data, then inserts and commits using the same scoped context. Decisions and management mutations use the same protocol and reload current state, roles and facility grants. Conflicting writers wait then recheck; exhausted contention returns 503 without partial data. No process-local lock or unique-start constraint is used. [Concurrency details and executed evidence](docs/CONCURRENCY.md).

## Database and migrations

The default file is `src/HospitalAppointments.Web/data/clinic.db`, resolved from Web content root, outside wwwroot and ignored by Git. Three migrations cover Identity, the booking model and explicit integer-storage checks for money. Staff use TPH; relationships into history use restrictive deletion. Fees are decimal in C# and integer minor units in SQLite; instants are UTC DateTime mapped to integer ticks. WAL and foreign keys are enabled; provider timeout is five seconds.

The local app migrates before accepting requests, then initializes roles and enabled demo fixtures. SQLite check-constraint changes require EF table rebuilds; the ExactMinorUnits migration emitted a non-transactional-PRAGMA warning and completed successfully. Migration atomicity is separate from the tested booking transaction guarantee. Use a backup before future changes to a database you need to retain. Tests apply the same migrations on isolated files under ignored TestResults/integration, never EF InMemory or the Java database.

Startup applies the migrations automatically. To compare the EF model to its migration snapshot, run from the solution root:

```powershell
dotnet ef migrations has-pending-model-changes --project src/HospitalAppointments.Infrastructure --startup-project src/HospitalAppointments.Web
```

The model check compares code to the snapshot without migrating a database. The design-time factory never seeds users. For a fresh demo, stop the app first and deliberately remove only its synthetic `clinic.db`, `clinic.db-wal` and `clinic.db-shm` files, then restart. This destroys that demo database; it is not an automatic startup/reset operation. Do not touch the Java reference.

## Submission and verification

Use **HospitalAppointments-submission-source.zip** for SABİS if the course accepts a source ZIP. The [Windows delivery checklist](docs/RUN_ON_WINDOWS.md#course-delivery-checklist) identifies the package, setup steps and course obligations. The deadline supplied by the owner is **15 November 2026 at 23:59 Türkiye time**. Presentations begin **17 November 2026** with individually assigned times. Attendance is mandatory.

The [teacher audit](docs/REQUIREMENTS_AUDIT.md) maps all 13 application requirements to code and tests. The [Stage 4 report](docs/STAGE4_VERIFICATION.md) records the historical browser/keyboard checks and published workflow. [Test parity](docs/TEST_PARITY.md) maps the 118 historical Java execution rows to C# cases or provider-specific replacements. See [requirements](REQUIREMENTS.md), [architecture](docs/ARCHITECTURE_REVIEW.md), [plan](docs/PLAN.md) and [SQLite concurrency](docs/CONCURRENCY.md) for the design.
