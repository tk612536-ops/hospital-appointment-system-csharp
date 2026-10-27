using System.Net;
using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Staff;
using HospitalAppointments.Infrastructure.Demo;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalAppointments.IntegrationTests;

public sealed class BookingPersistenceTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SqliteConstraintsAndRestrictiveHistoryDeletionActuallyApply()
    {
        using var s = await BookingScenario.CreateAsync(); var id = await s.BookAsync("patient", s.North, 10);
        using var scope = s.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync("UPDATE Appointments SET End = Start", Token));
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync("UPDATE Appointments SET Status = 'Approved'", Token));
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync("UPDATE HealthcareServices SET FeeMinor = -1", Token));
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlAsync($"DELETE FROM StaffOfferings WHERE Id = {s.North}", Token));
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync("UPDATE Appointments SET BookedFeeMinor = 60000.5", Token));
        await Assert.ThrowsAsync<SqliteException>(() => db.Database.ExecuteSqlRawAsync("UPDATE StaffMembers SET StaffType = 'Unknown'", Token));
        Assert.Equal(AppointmentStatus.Pending, (await s.DetailAsync("patient", id)).Status);
    }

    [Fact]
    public async Task FractionalFeeRoundTripsAsExactIntegerMinorUnits()
    {
        using var s = await BookingScenario.CreateAsync(); var id = await s.BookAsync("patient", s.Lab, 10);
        using var scope = s.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.OpenConnectionAsync(Token); using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "SELECT BookedFeeMinor FROM Appointments WHERE Id = @id";
        var parameter = cmd.CreateParameter(); parameter.ParameterName = "@id"; parameter.Value = id; cmd.Parameters.Add(parameter);
        Assert.Equal(25025L, await cmd.ExecuteScalarAsync(Token));
        Assert.Equal(250.25m, (await s.DetailAsync("patient", id)).Fee);
    }

    [Fact]
    public async Task RenamedSeedAndDecidedAppointmentSurviveRestartWithoutMovingDates()
    {
        string path; long id; DateTime start; int windowCount;
        using (var s = await BookingScenario.CreateAsync())
        {
            path = s.Factory.DatabasePath; id = await s.BookAsync("patient", s.North, 10, 30);
            await s.DecideAsync("staff", id, AppointmentStatus.Approved);
            start = (await s.DetailAsync("patient", id)).Start;
            using var scope = s.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var facility = await db.Set<Facility>().SingleAsync(f => f.Type == FacilityType.Hospital, Token);
            facility.UpdateDetails("Yeni demo adı", FacilityType.Hospital); await db.SaveChangesAsync(Token);
            windowCount = await db.Set<AvailabilityWindow>().CountAsync(Token);
        }
        using var restarted = new ClinicFactory("Demo", true, path, seedBookings: true, clock: new FixedClinicTime());
        using var verification = restarted.Services.CreateScope(); var database = verification.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var saved = await database.Set<Appointment>().SingleAsync(Token);
        Assert.Equal(id, saved.Id); Assert.Equal(AppointmentStatus.Approved, saved.Status);
        Assert.Equal(start.Hour - 3, saved.Start.Hour); Assert.Equal(windowCount, await database.Set<AvailabilityWindow>().CountAsync(Token));
        Assert.Equal(2, await database.Set<Facility>().CountAsync(Token));
        Assert.Equal("Yeni demo adı", (await database.Set<Facility>().SingleAsync(f => f.Type == FacilityType.Hospital, Token)).Name);
        Assert.All(await database.Set<AvailabilityWindow>().ToListAsync(Token), window => Assert.True(window.Start > new FixedClinicTime().GetUtcNow()));
    }

    [Fact]
    public async Task BusyHttpResponseIs503AndPreservesSelectionWithoutSaving()
    {
        using var s = await BookingScenario.CreateAsync(); using var browser = s.Factory.Browser();
        await BrowserForms.LoginAsync(browser, "patient");
        var token = await BrowserForms.TokenAsync(browser, "/patient/book?offeringId=" + s.North + "&date=2030-01-09");
        await using var holder = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = s.Factory.DatabasePath, ForeignKeys = true }.ToString());
        await holder.OpenAsync(Token); await using var held = holder.BeginTransaction(deferred: false);
        var response = await browser.PostAsync("/patient/book", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["OfferingId"] = s.North.ToString(), ["Date"] = "2030-01-09", ["StartTime"] = "10:30",
            ["CatalogVersion"] = s.Version.ToString(), ["__RequestVerificationToken"] = token
        }), Token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(1), response.Headers.RetryAfter!.Delta);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(Token));
        Assert.Contains("Sistem şu anda meşgul", html); Assert.Contains("2030-01-09", html);
        using var scope = s.Factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<Appointment>().ToListAsync(Token));
    }
}
