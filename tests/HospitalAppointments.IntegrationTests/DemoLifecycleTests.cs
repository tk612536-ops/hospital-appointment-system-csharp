using System.Net;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Staff;
using HospitalAppointments.Infrastructure.Demo;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalAppointments.IntegrationTests;

public sealed class DemoLifecycleTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task FreshDemoSeedsTomorrowInIstanbulWhenUtcDateIsStillYesterday()
    {
        // 7 January UTC is already 8 January in Istanbul. First fixture date must be 9 January.
        var clock = new FixedClinicTime { Now = new(2030, 1, 7, 22, 30, 0, TimeSpan.Zero) };
        using var factory = new ClinicFactory("Demo", true, seedBookings: true, clock: clock);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var time = scope.ServiceProvider.GetRequiredService<ClinicTime>();
        var firstDate = new DateOnly(2030, 1, 9);
        var windows = await db.Set<AvailabilityWindow>().AsNoTracking().ToArrayAsync(Token);
        var dates = windows.Select(w => DateOnly.FromDateTime(time.Local(w.Start))).Distinct().Order().ToArray();
        Assert.Equal(Enumerable.Range(0, 14).Select(firstDate.AddDays), dates);
        Assert.All(windows, w => Assert.True(w.Start > clock.GetUtcNow().UtcDateTime));
        var sample = await db.Set<Appointment>().AsNoTracking().SingleAsync(Token);
        Assert.Equal(firstDate, DateOnly.FromDateTime(time.Local(sample.Start)));

        var offering = await NorthAsync(db);
        using var patient = factory.Browser();
        Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.LoginAsync(patient, "patient")).StatusCode);
        var page = await patient.GetStringAsync($"/patient/book?offeringId={offering.Id}&date=2030-01-09", Token);
        Assert.Contains("name=\"StartTime\" value=\"10:30\"", page);
    }

    [Fact]
    public async Task AgedDemoRestartPreservesHistoryAndManagerCanExtendWithoutReset()
    {
        string databasePath;
        Appointment originalAppointment;
        AvailabilityWindow[] originalWindows;
        long offeringId, version, facilityId, branchId, staffId;
        using (var s = await BookingScenario.CreateAsync())
        {
            databasePath = s.Factory.DatabasePath;
            var id = await s.BookAsync("patient", s.North, 10);
            await s.DecideAsync("staff", id, AppointmentStatus.Approved);
            using var scope = s.Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            originalAppointment = await db.Set<Appointment>().AsNoTracking().SingleAsync(Token);
            originalWindows = await db.Set<AvailabilityWindow>().AsNoTracking().OrderBy(w => w.Id).ToArrayAsync(Token);
            var offering = await NorthAsync(db);
            offeringId = offering.Id; version = offering.Service.Version;
            facilityId = offering.Branch.FacilityId; branchId = offering.Branch.Id; staffId = offering.StaffId;
        }

        // Every original fixture is now past. Restart must not move rows or silently seed a new horizon.
        var clock = new FixedClinicTime { Now = new(2030, 2, 1, 6, 0, 0, TimeSpan.Zero) };
        using var restarted = new ClinicFactory("Demo", true, databasePath, seedBookings: true, clock: clock);
        using (var verification = restarted.Services.CreateScope())
        {
            var db = verification.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Equivalent(originalAppointment, await db.Set<Appointment>().AsNoTracking().SingleAsync(Token), strict: true);
            var windows = await db.Set<AvailabilityWindow>().AsNoTracking().OrderBy(w => w.Id).ToArrayAsync(Token);
            Assert.Equivalent(originalWindows, windows, strict: true);
            Assert.All(windows, w => Assert.True(w.End < clock.GetUtcNow().UtcDateTime));
            Assert.Equal(7, await db.Users.CountAsync(Token));
            Assert.Equal(2, await db.Set<Facility>().CountAsync(Token));
            Assert.Equal("booking-v1", (await db.Set<DemoSeedMarker>().SingleAsync(Token)).Key);
        }

        using var patient = restarted.Browser();
        Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.LoginAsync(patient, "patient")).StatusCode);
        var selection = $"/patient/book?offeringId={offeringId}&date=2030-02-02";
        var empty = await patient.GetStringAsync(selection, Token);
        Assert.Contains("Bu seçim için uygun saat bulunmuyor", WebUtility.HtmlDecode(empty));
        Assert.DoesNotContain("name=\"StartTime\"", empty);
        var fields = new Dictionary<string, string>
        {
            ["OfferingId"] = offeringId.ToString(), ["Date"] = "2030-02-02",
            ["StartTime"] = "10:30", ["CatalogVersion"] = version.ToString()
        };
        // A forged submission cannot book an unconfigured date even though the catalog still exists.
        var unavailable = await ManagementHttpTests.PostAsync(patient, "/patient", "/patient/book", fields);
        Assert.Equal(HttpStatusCode.Conflict, unavailable.StatusCode);
        Assert.Contains("uygunluk aralığına", WebUtility.HtmlDecode(await unavailable.Content.ReadAsStringAsync(Token)));
        Assert.Contains("Onaylandı", WebUtility.HtmlDecode(await patient.GetStringAsync("/appointments/" + originalAppointment.Id, Token)));

        using var manager = restarted.Browser();
        Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.LoginAsync(manager, "manager")).StatusCode);
        var schedule = $"/manager/facilities/{facilityId}/schedule";
        var extended = await ManagementHttpTests.PostAsync(manager, schedule, schedule + "/availability", new()
        {
            ["BranchId"] = branchId.ToString(), ["StaffId"] = staffId.ToString(),
            ["Date"] = "2030-02-02", ["Start"] = "09:00", ["End"] = "11:00"
        });
        Assert.Equal(HttpStatusCode.Redirect, extended.StatusCode);
        Assert.Contains("name=\"StartTime\" value=\"10:30\"", await patient.GetStringAsync(selection, Token));
        var booked = await ManagementHttpTests.PostAsync(patient, selection, "/patient/book", fields);
        Assert.Equal(HttpStatusCode.Redirect, booked.StatusCode);
        var details = booked.Headers.Location!.ToString();
        using var staff = restarted.Browser();
        Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.LoginAsync(staff, "staff")).StatusCode);
        var approved = await ManagementHttpTests.PostAsync(staff, details, details + "/decision", new() { ["decision"] = "Approved" });
        Assert.Equal(HttpStatusCode.Redirect, approved.StatusCode);
        Assert.Contains("Onaylandı", WebUtility.HtmlDecode(await patient.GetStringAsync(details, Token)));

        using var finalScope = restarted.Services.CreateScope();
        var saved = finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equivalent(originalAppointment, await saved.Set<Appointment>().AsNoTracking()
            .SingleAsync(a => a.Id == originalAppointment.Id, Token), strict: true);
        var retainedWindows = await saved.Set<AvailabilityWindow>().AsNoTracking()
            .Where(w => w.Start < clock.GetUtcNow().UtcDateTime).OrderBy(w => w.Id).ToArrayAsync(Token);
        Assert.Equivalent(originalWindows, retainedWindows, strict: true);
        Assert.Equal(originalWindows.Length + 1, await saved.Set<AvailabilityWindow>().CountAsync(Token));
        var newAppointment = await saved.Set<Appointment>().AsNoTracking().SingleAsync(a => a.Id != originalAppointment.Id, Token);
        Assert.Equal(AppointmentStatus.Approved, newAppointment.Status);
        Assert.Equal(new DateTime(2030, 2, 2, 7, 30, 0, DateTimeKind.Utc), newAppointment.Start);
        Assert.Equal(new DateTime(2030, 2, 2, 8, 0, 0, DateTimeKind.Utc), newAppointment.End);
    }

    private static Task<StaffOffering> NorthAsync(ApplicationDbContext db) => db.Set<StaffOffering>()
        .Include(o => o.Service).ThenInclude(s => s.Department).ThenInclude(d => d.Branch)
        .SingleAsync(o => o.Service.DurationMinutes == 30 && o.Service.Department.Branch.Name == "Kuzey", Token);
}
