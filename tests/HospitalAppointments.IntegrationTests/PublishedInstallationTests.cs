using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Staff;
using HospitalAppointments.Infrastructure.Demo;
using HospitalAppointments.Infrastructure.Identity;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointments.IntegrationTests;

public sealed class PublishedInstallationTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PublishedReleaseInitializesItsOwnDatabaseAndPreservesChangesAcrossProcessRestart()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "HospitalAppointments.sln"))) root = root.Parent;
        Assert.NotNull(root);
        var isolated = Path.Combine(root.FullName, "TestResults", "published", Guid.NewGuid().ToString("N"));
        var published = Path.Combine(isolated, "published app");
        var launcher = Directory.CreateDirectory(Path.Combine(isolated, "launcher")).FullName;
        await using (var publish = Child.Start(root.FullName, "publish", "src/HospitalAppointments.Web", "-c", "Release",
            "--no-restore", "--disable-build-servers", "--self-contained", "false", "-o", published))
            await publish.RequireSuccessAsync(TimeSpan.FromMinutes(3));

        var databasePath = Path.Combine(published, "data", "clinic.db");
        Assert.False(File.Exists(databasePath), "Publish must not package an existing database.");
        Assert.False(Directory.Exists(Path.Combine(launcher, "data")));
        Appointment[] originalAppointments;
        AvailabilityWindow[] originalWindows;
        int[] originalSeedCounts;
        long appointmentId;
        await using (var app = StartApp(published, launcher, out var address))
        {
            using var patient = Browser(address);
            await app.ReadyAsync(patient);
            Assert.True(File.Exists(databasePath));
            foreach (var asset in new[] { "vendor/bootstrap/bootstrap.min.css", "css/site.css" })
                Assert.Equal(await File.ReadAllBytesAsync(Path.Combine(published, "wwwroot", asset), Token),
                    await patient.GetByteArrayAsync("/" + asset, Token));

            long offeringId, version; string date;
            await using (var db = Database(databasePath))
            {
                Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync(Token));
                Assert.Empty(await db.Database.GetPendingMigrationsAsync(Token));
                originalSeedCounts = await SeedCountsAsync(db);
                Assert.Equal([7, 3, 7, 2, 3, 3, 5, 70, 42, 1], originalSeedCounts);
                Assert.Single(await db.Set<Appointment>().ToListAsync(Token));
                var offering = await db.Set<StaffOffering>().Include(o => o.Service).ThenInclude(s => s.Department)
                    .ThenInclude(d => d.Branch).SingleAsync(o => o.Service.DurationMinutes == 30 && o.Service.Department.Branch.Name == "Kuzey", Token);
                offeringId = offering.Id; version = offering.Service.Version;
                var lastStart = await db.Set<AvailabilityWindow>().Where(w => w.StaffId == offering.StaffId && w.BranchId == offering.Branch.Id)
                    .MaxAsync(w => w.Start, Token);
                // Use the last seeded date, safely future even if installation runs near local midnight.
                date = TimeZoneInfo.ConvertTimeFromUtc(lastStart, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"))
                    .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            }
            Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.LoginAsync(patient, "patient")).StatusCode);
            var booked = await ManagementHttpTests.PostAsync(patient, $"/patient/book?offeringId={offeringId}&date={date}", "/patient/book", new()
            {
                ["OfferingId"] = offeringId.ToString(), ["CatalogVersion"] = version.ToString(),
                ["Date"] = date, ["StartTime"] = "10:30"
            });
            Assert.Equal(HttpStatusCode.Redirect, booked.StatusCode);
            var details = booked.Headers.Location!.ToString(); appointmentId = long.Parse(details.Split('/').Last());
            using var staff = Browser(address);
            Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.LoginAsync(staff, "staff")).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await ManagementHttpTests.PostAsync(staff, details, details + "/decision",
                new() { ["decision"] = "Approved" })).StatusCode);
            using var manager = Browser(address);
            Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.LoginAsync(manager, "manager")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync("/manager/facilities", Token)).StatusCode);
            await using var verification = Database(databasePath);
            originalAppointments = await verification.Set<Appointment>().AsNoTracking().OrderBy(a => a.Id).ToArrayAsync(Token);
            originalWindows = await verification.Set<AvailabilityWindow>().AsNoTracking().OrderBy(w => w.Id).ToArrayAsync(Token);
            Assert.Equal(AppointmentStatus.Approved, originalAppointments.Single(a => a.Id == appointmentId).Status);
        }

        // Change only this isolated account fixture while its host is stopped; seeders must retain it.
        var changedPassword = "Synthetic1!" + Guid.NewGuid().ToString("N");
        long accountId; string changedHash;
        await using (var db = Database(databasePath))
        {
            var account = await db.Users.SingleAsync(u => u.UserName == "patient", Token);
            accountId = account.Id;
            changedHash = new PasswordHasher<ApplicationUser>().HashPassword(account, changedPassword);
            account.PasswordHash = changedHash;
            await db.SaveChangesAsync(Token);
        }

        await using (var restarted = StartApp(published, launcher, out var address))
        {
            using var patient = Browser(address);
            await restarted.ReadyAsync(patient);
            Assert.Equal(HttpStatusCode.OK, (await BrowserForms.LoginAsync(patient, "patient")).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.LoginAsync(patient, "patient", changedPassword)).StatusCode);
            Assert.Contains("Onaylandı", WebUtility.HtmlDecode(await patient.GetStringAsync("/appointments/" + appointmentId, Token)));
            await using var db = Database(databasePath);
            var account = await db.Users.SingleAsync(u => u.Id == accountId, Token);
            Assert.Equal(changedHash, account.PasswordHash);
            Assert.Equal(originalSeedCounts, await SeedCountsAsync(db));
            Assert.Equivalent(originalAppointments, await db.Set<Appointment>().AsNoTracking().OrderBy(a => a.Id).ToArrayAsync(Token), strict: true);
            Assert.Equivalent(originalWindows, await db.Set<AvailabilityWindow>().AsNoTracking().OrderBy(w => w.Id).ToArrayAsync(Token), strict: true);
            Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync(Token));
        }
        Assert.False(Directory.Exists(Path.Combine(launcher, "data")), "Database paths must resolve against the explicit content root, not the launcher directory.");
    }

    private static ApplicationDbContext Database(string path) => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(
        new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = path, ForeignKeys = true }.ToString()).Options);

    private static async Task<int[]> SeedCountsAsync(ApplicationDbContext db) =>
        [await db.Users.CountAsync(Token), await db.Roles.CountAsync(Token), await db.UserRoles.CountAsync(Token),
        await db.Set<Facility>().CountAsync(Token), await db.Set<Branch>().CountAsync(Token), await db.Set<StaffMember>().CountAsync(Token),
        await db.Set<StaffOffering>().CountAsync(Token), await db.Set<AvailabilityWindow>().CountAsync(Token),
        await db.Set<OpeningWindow>().CountAsync(Token), await db.Set<DemoSeedMarker>().CountAsync(Token)];

    private static Child StartApp(string published, string launcher, out Uri address)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        address = new Uri("https://localhost:" + port);
        return Child.Start(launcher, Path.Combine(published, "HospitalAppointments.Web.dll"), "--contentRoot", published,
            "--environment", "Demo", "--Database:Path", "data/clinic.db", "--urls", address.ToString());
    }

    private static HttpClient Browser(Uri address) => new(new HttpClientHandler
    {
        AllowAutoRedirect = false, UseCookies = true,
        // Only this test's localhost child; does not modify certificate trust or production validation.
        ServerCertificateCustomValidationCallback = (request, _, _, _) => request.RequestUri!.IsLoopback
    }) { BaseAddress = address, Timeout = TimeSpan.FromSeconds(10) };

    private sealed class Child : IAsyncDisposable
    {
        private readonly Process process;
        private readonly Task<string> stdout;
        private readonly Task<string> stderr;
        private Child(Process process)
        {
            this.process = process;
            stdout = process.StandardOutput.ReadToEndAsync(); stderr = process.StandardError.ReadToEndAsync();
        }
        public static Child Start(string workingDirectory, params string[] arguments)
        {
            var info = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = workingDirectory, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            foreach (var argument in arguments) info.ArgumentList.Add(argument);
            return new Child(Process.Start(info) ?? throw new InvalidOperationException("Could not start the test child."));
        }
        public async Task RequireSuccessAsync(TimeSpan timeout)
        {
            await Task.WhenAll(process.WaitForExitAsync(Token), stdout, stderr).WaitAsync(timeout, Token);
            Assert.True(process.ExitCode == 0, await stdout + await stderr);
        }
        public async Task ReadyAsync(HttpClient client)
        {
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed < TimeSpan.FromSeconds(45))
            {
                if (process.HasExited) throw new InvalidOperationException("Published host exited: " + await stdout + await stderr);
                try { if ((await client.GetAsync("/", Token)).IsSuccessStatusCode) return; }
                catch (HttpRequestException) { }
                await Task.Delay(100, Token); // Bounded startup readiness polling only.
            }
            throw new TimeoutException("Published host did not become ready.");
        }
        public async ValueTask DisposeAsync()
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));
            try { await Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(10)); }
            finally { process.Dispose(); }
        }
    }
}
