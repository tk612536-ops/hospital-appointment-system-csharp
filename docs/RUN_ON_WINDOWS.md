# Run and deliver on another Windows computer

These instructions describe the tested C#/.NET 10 classroom application. All accounts/names are synthetic. Use a writable local folder such as `C:\ClinicDemo`; do not run the SQLite file from a network share, protected Program Files folder, or the Java reference directory.

## Prerequisites

- Windows x64 is the environment exercised here. The verified SDK is **10.0.403**; it includes ASP.NET Core/.NET **10.0.14**. The source global.json permits later patches of the same SDK feature band. Install that SDK for the simplest source/build/HTTPS setup. Git is needed only for cloning/history, not for running a source ZIP or publish folder.
- For the framework-dependent Release folder alone, a compatible **ASP.NET Core Runtime 10** is required; the base .NET Runtime or Desktop Runtime alone is insufficient. The SDK includes it. The package does not bundle the runtime. Native SQLite dependencies are included in the publish folder; copy the whole folder, not only the Web DLL.
- Source restore/tool restore initially needs NuGet access. Once prepared, the running application uses local Bootstrap/CSS/fonts and no CDN. Visual Studio is optional; PowerShell and the dotnet CLI are sufficient.
- Local demo HTTPS needs a development certificate on the **target computer/current user**. With the SDK installed, run yourself and approve its Windows trust prompt:

```powershell
dotnet dev-certs https --trust
```

Then use **https://localhost:7143**. Do not export another person's private certificate, copy secrets into the package, bypass browser warnings or turn off Secure cookies. Each target computer establishes its own local certificate. If a browser was already open, restart it after trusting the certificate if needed.

Microsoft references: [framework-dependent publishing](https://learn.microsoft.com/en-us/dotnet/core/deploying/), [development certificate command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-dev-certs). They explain runtime/certificate behavior; this project's own executed evidence is in STAGE4_VERIFICATION.md.

## Option A — source checkout or source ZIP

Use the final `HospitalAppointments-submission-source.zip` or clone the C# repository's [`main` branch](https://github.com/tk612536-ops/hospital-appointment-system-csharp/tree/main). A private repository requires access for the reviewer. The source ZIP can be extracted and built without a GitHub login.

Extract the source archive or clone the repository into a new local folder. Open PowerShell **in the folder containing HospitalAppointments.sln**, then run:

```powershell
dotnet --version
dotnet tool restore
dotnet restore HospitalAppointments.sln --locked-mode
dotnet build HospitalAppointments.sln --no-restore
dotnet test HospitalAppointments.sln --no-restore
dotnet run --no-launch-profile --project src/HospitalAppointments.Web -- --environment Demo
```

Startup applies the included migrations, creates required Identity roles, and seeds fictional users/catalog/shifts only in explicitly enabled Demo mode. No separate SQLite installation or database server is needed. The default source database is `src/HospitalAppointments.Web/data/clinic.db`. The directory is created automatically and must be writable.

Open [https://localhost:7143](https://localhost:7143). HTTP 5143 redirects to HTTPS; authentication and antiforgery require the HTTPS URL. Stop with Ctrl+C before rebuilding, moving, backing up or resetting the application. A running host can lock DLLs on Windows.

## Option B — framework-dependent Release folder

From the extracted source root, use the SDK to restore and publish the Release folder:

```powershell
dotnet restore HospitalAppointments.sln --locked-mode
dotnet publish src/HospitalAppointments.Web -c Release --no-restore --self-contained false -o artifacts/release
```

Copy the complete published folder when transferring it to a target Windows computer. In PowerShell, change into that folder and start the app:

```powershell
dotnet .\HospitalAppointments.Web.dll --environment Demo
```

The folder is the content root. Its default database is **release/data/clinic.db**. A source-build database is not automatically shared. For an explicit database location, use an absolute path in a writable local directory:

```powershell
dotnet .\HospitalAppointments.Web.dll --environment Demo --Database:Path C:\ClinicDemo\data\clinic.db
```

When launching from a different working directory (for example, a shortcut or another PowerShell folder), also specify the extracted Release folder as the content root:

```powershell
dotnet 'C:\ClinicDemo\release\HospitalAppointments.Web.dll' --contentRoot 'C:\ClinicDemo\release' --environment Demo --Database:Path data/clinic.db
```

Replace the example folder with your actual extraction path. The content root supplies appsettings and local static assets; a relative database path resolves under that root, so this example uses **C:\ClinicDemo\release\data\clinic.db** regardless of the launcher folder. Keep the same content root/database on restart. This launch mode is exercised by `PublishedInstallationTests`; it does not require copying a source database.

Use the same path on restart. A freshly published folder gets its own SQLite database on first start. Keep that database and any local HTTPS certificate out of transfers. Install and trust the certificate on the target as above. The default settings target a localhost classroom demonstration.

## Synthetic credentials

All these demonstration accounts use **KlinikDemo10!**:

| Username | Role / purpose |
| --- | --- |
| patient | Patient and own appointment history. |
| patient2 | Second patient for ownership/conflict demonstrations. |
| staff | Doctor assigned to the synthetic hospital. |
| lab | Hospital healthcare/laboratory worker. |
| clinicstaff | Worker at the separate single-branch clinic. |
| manager | Manager granted the hospital; can create new owned institutions. |
| clinicmanager | Manager granted only the separate clinic. |

Five wrong passwords lock the account for five minutes. Newly registered staff use the credentials the manager entered and receive STAFF only. Optional credentials can be omitted; a profile without a login remains bookable through manager-defined offerings/availability, with a granted manager able to decide its appointments.

Seed registration requires both environment `Demo` and `Demo:Enabled=true`; booking fixtures also require `Demo:SeedBookings=true`. Outside that environment, schema/roles are initialized but demo seeders are absent. Selecting Production does **not** remove existing demo accounts from an old database. To examine a clean non-demo host, select a different file explicitly. Public signup, password recovery and initial non-demo manager provisioning are outside the UI scope.

## First rehearsal

Follow [the README walkthrough](../README.md) or [the Turkish presentation sequence](PRESENTATION_GUIDE_TR.md). Managers create institution → first branch → department → service → specialty → doctor/login → offering → weekly opening hours → dated availability. Patients book; assigned staff approve/reject; patients see the result. Schedule changes that would invalidate future/ongoing reserved intervals are rejected.

Initial fixtures start tomorrow and span fourteen dates. Restarts **never** advance or duplicate saved appointments/availability. If the demo ages, add fresh future availability through management, with appropriate weekly hours and active branch offering. Existing future shifts can prevent reducing hours; adjust safe unreserved shifts first. Dates/times display in Europe/Istanbul. Native date/time controls may follow the browser/OS format; descriptive dates and money use Turkish formatting.

For the unchanged seeded hospital, sign in as `manager`, open its schedule page, and add availability for **Dr. Deniz (Demo)** at **Kuzey**, on a chosen future date from **09:00 to 11:00**. The seeded weekly hours already cover that interval on every weekday. As `patient`, select the same date and the doctor's **Genel muayene** offering; **10:30–11:00** is a valid 30-minute interval if it is unreserved. Sign in as `staff` to approve it. This extends the demo without resetting its accounts or historical appointments. If configuration or passwords have been changed, use the saved current configuration/credentials.

## Back up or deliberately reset a demo

First stop **every** host using the selected database. A reset destroys that selected synthetic history; it is never automatic. Prefer keeping a stopped-folder backup before changes. Do not copy a database while another process is writing it. Do not delete the Java application's files.

From the source solution root, this explicit non-recursive example resets only the default C# demo database and its sidecars:

```powershell
$demoDatabase = Join-Path $PWD 'src/HospitalAppointments.Web/data/clinic.db'
foreach ($demoFile in @($demoDatabase, "$demoDatabase-wal", "$demoDatabase-shm")) {
    if (Test-Path -LiteralPath $demoFile) { Remove-Item -LiteralPath $demoFile }
}
```

For a publish folder, change the database variable to that folder's actual `data/clinic.db`; for `--Database:Path`, use that explicitly selected path instead. Check it before running. Do not recursively remove source folders or use guessed paths. Restart Demo to apply schema and seed fresh future fixtures. Existing seed marker/name edits do not trigger reset.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| SDK not found / wrong version | Install SDK 10.0.403 or an allowed feature-band patch; restart the terminal; check dotnet --version and global.json. |
| HTTPS certificate warning | Run the trust command yourself for the current user; use localhost, restart the browser if necessary. No warning bypass is needed. |
| Port already used | Stop the earlier app process. Alternatively choose a free HTTPS URL with `--urls https://localhost:7243` and browse that port. |
| Build reports locked DLL | Stop the app before rebuilding/publishing. Do not stop unrelated processes or reset source. |
| No valid start times | Check the future date, full service duration, branch hours/lunch gap, active offering/staff/service, matching availability and patient/employee reservations. Updating a selection is explicit. |
| Missing assets in published app | Copy the complete publish folder and start from it. Verify wwwroot/vendor/bootstrap/bootstrap.min.css and wwwroot/css/site.css exist. |
| SQLite busy / 503 | Another writer may hold the file; wait for the command to finish and retry as a fresh request. No partial success is claimed. Keep the file local. |
| Login fails | Use the correct demo database/environment, avoid lockout, and remember startup never replaces stored passwords. |
| Empty Production database | Expected: no public signup/initial manager provisioning screen. Use explicit Demo for class rehearsal, not real patient data. |

## Limitations

Single timezone/currency, same-day periods and 15-minute starts are deliberate. No overnight/holiday calendar, travel/room scheduling, payments, medical records, public signup/recovery/grant editor or automatic Pending expiry/cancellation/rescheduling is included. Safe concurrent configuration edits use the last valid state; unsaved manager form input is not automatically restored. Historical service/staff names and terms are snapshots; institution/branch/professional labels are current links.

## Course delivery checklist

1. **Use `artifacts/HospitalAppointments-submission-source.zip`** from the project folder if SABİS accepts a source ZIP. The teacher's assignment requires a SABİS upload but specifies no archive format or filename. Follow any additional course instructions about the required file type and naming. The old Stage 4 archives are historical snapshots.
2. Extract the ZIP and open the folder containing `HospitalAppointments.sln`. It contains the four C# projects, three test projects, seven dependency lock files, the EF tool manifest and migration sources, local Bootstrap assets/license and project documentation. The synthetic demo login constants are included so a fresh install can seed the classroom accounts.
3. Share the [C# repository](https://github.com/tk612536-ops/hospital-appointment-system-csharp) and ensure the professor can access it. The assignment requires at least ten commits on different days. Check the actual `main` history with `git log --format="%h %cI %s"`.
4. Upload before **15 November 2026 at 23:59 Türkiye time** and verify the SABİS receipt. This deadline and the **17 November 2026** presentation start are the schedule supplied by the owner. Check the separately assigned date/time and attend. The assignment requires both upload and attendance. Confirm the selection record and individual/maximum-two-person group with the course.

The source ZIP can be used to build a Release folder locally. See [the isolated published-installation evidence](DAY6_VERIFICATION.md) and [Turkish rehearsal guide](PRESENTATION_GUIDE_TR.md) for the tested run modes. The Windows verification used isolated folders on the development computer.
