using System.Net;
using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Application.Transactions;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Domain.Catalog;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Staff;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalAppointments.IntegrationTests;

public sealed class BookingIntegrationTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task EntireDurationAtBoundaryBooksWithExactTermsAndUtcPersistence()
    {
        using var s = await BookingScenario.CreateAsync();
        var id = await s.BookAsync("patient", s.North, 10, 30);
        var view = await s.DetailAsync("patient", id);
        Assert.Equal(11, view.End.Hour); Assert.Equal(30, view.DurationMinutes); Assert.Equal(600m, view.Fee);
        Assert.Equal(AppointmentStatus.Pending, view.Status);
        using var scope = s.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var row = await db.Set<Appointment>().SingleAsync(Token);
        Assert.Equal(DateTimeKind.Utc, row.Start.Kind); Assert.Equal(7, row.Start.Hour);
        await db.Database.OpenConnectionAsync(Token);
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT typeof(Start) || ':' || typeof(BookedFeeMinor) || ':' || BookedFeeMinor FROM Appointments";
        Assert.Equal("integer:integer:60000", await command.ExecuteScalarAsync(Token));
    }

    [Theory]
    [InlineData(10, 45)]
    [InlineData(13, 0)]
    [InlineData(8, 45)]
    [InlineData(10, 1)]
    public async Task PartialDurationWrongBranchHoursAndOffGridAreRejected(int hour, int minute)
    {
        using var s = await BookingScenario.CreateAsync();
        await Assert.ThrowsAsync<BookingException>(() => s.BookAsync("patient", s.North, hour, minute));
        using var scope = s.Factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<Appointment>().ToListAsync(Token));
    }

    [Theory]
    [InlineData(11, 45)]
    [InlineData(12, 0)]
    [InlineData(16, 45)]
    public async Task BranchLunchAndClosingGapsRejectTheFullInterval(int hour, int minute)
    {
        using var s = await BookingScenario.CreateAsync();
        await Assert.ThrowsAsync<BookingException>(() => s.BookAsync("patient", s.Lab, hour, minute));
    }

    [Fact]
    public async Task AdjacentWindowsMergeAndPositiveAvailabilityGapsDoNot()
    {
        using var s = await BookingScenario.CreateAsync();
        await using var scope = s.Factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var o = (await scope.ServiceProvider.GetRequiredService<IBookingCatalogRepository>().FindAsync(s.North, Token))!;
        var time = scope.ServiceProvider.GetRequiredService<ClinicTime>();
        await db.Set<AvailabilityWindow>().Where(w => w.StaffId == o.StaffId && w.BranchId == o.Branch.Id).ExecuteDeleteAsync(Token);
        db.AddRange(new AvailabilityWindow(o.Staff, o.Branch, time.Interval(s.Date, new(9, 0), new(10, 0))),
            new AvailabilityWindow(o.Staff, o.Branch, time.Interval(s.Date, new(10, 0), new(11, 0))));
        await db.SaveChangesAsync(Token);
        Assert.True(await s.BookAsync("patient", s.North, 9, 45) > 0);
        await db.Set<Appointment>().ExecuteDeleteAsync(Token);
        await db.Set<AvailabilityWindow>().Where(w => w.StaffId == o.StaffId && w.BranchId == o.Branch.Id).ExecuteDeleteAsync(Token);
        db.ChangeTracker.Clear(); db.Attach(o.Staff); db.Attach(o.Branch);
        db.AddRange(new AvailabilityWindow(o.Staff, o.Branch, time.Interval(s.Date, new(9, 0), new(10, 0))),
            new AvailabilityWindow(o.Staff, o.Branch, time.Interval(s.Date, new(10, 15), new(11, 0))));
        await db.SaveChangesAsync(Token);
        await Assert.ThrowsAsync<BookingException>(() => s.BookAsync("patient", s.North, 9, 45));
    }

    [Fact]
    public async Task PendingApprovedReserveAndRejectedReleases()
    {
        using var s = await BookingScenario.CreateAsync(); var id = await s.BookAsync("patient", s.North, 10);
        await Assert.ThrowsAsync<BookingException>(() => s.BookAsync("patient2", s.North, 10, 15));
        await s.DecideAsync("staff", id, AppointmentStatus.Approved);
        await Assert.ThrowsAsync<BookingException>(() => s.BookAsync("patient2", s.North, 10));
        var rejected = await s.BookAsync("patient", s.Lab, 13);
        await s.DecideAsync("lab", rejected, AppointmentStatus.Rejected);
        Assert.True(await s.BookAsync("patient2", s.Lab, 13) > 0);
    }

    [Fact]
    public async Task PatientConflictsAcrossEmployeesAndBranchesAndAdjacencyWorks()
    {
        using var s = await BookingScenario.CreateAsync();
        await s.BookAsync("patient", s.South, 13);
        await Assert.ThrowsAsync<BookingException>(() => s.BookAsync("patient", s.Lab, 13, 15));
        await s.BookAsync("patient", s.North, 10);
        Assert.True(await s.BookAsync("patient2", s.North, 10, 30) > 0);
    }

    [Fact]
    public async Task CandidatesReuseFullDurationAndPatientConflictRules()
    {
        using var s = await BookingScenario.CreateAsync();
        var page = await s.AsAsync("patient", p => p.GetRequiredService<BookingPageService>().PageAsync(null, null, null, s.North, s.Date, Token));
        Assert.Contains(new TimeOnly(10, 30), page.Starts); Assert.DoesNotContain(new TimeOnly(10, 45), page.Starts);
        await s.BookAsync("patient", s.South, 13);
        var lab = await s.AsAsync("patient", p => p.GetRequiredService<BookingPageService>().PageAsync(null, null, null, s.Lab, s.Date, Token));
        Assert.DoesNotContain(new TimeOnly(13, 0), lab.Starts);
    }

    [Fact]
    public async Task SnapshotTermsAndNamesSurviveEditsAndStaleFormIsRejected()
    {
        using var s = await BookingScenario.CreateAsync(); var id = await s.BookAsync("patient", s.North, 10);
        using (var scope = s.Factory.Services.CreateScope())
        {
            await using var tx = await scope.ServiceProvider.GetRequiredService<IWriteTransaction>().BeginAsync(Token);
            var o = (await scope.ServiceProvider.GetRequiredService<IBookingCatalogRepository>().FindAsync(s.North, Token))!;
            o.Service.ReviseTerms(45, 999.99m); o.Service.Rename("Yeni koşullar"); o.Staff.Rename("Yeni ad");
            await tx.SaveAsync(Token); await tx.CommitAsync(Token);
        }
        await Assert.ThrowsAsync<BookingException>(() => s.BookAsync("patient2", s.North, 9));
        var view = await s.DetailAsync("patient", id);
        Assert.Equal(30, view.DurationMinutes); Assert.Equal(600m, view.Fee); Assert.Equal("Genel muayene", view.ServiceName); Assert.Equal("Dr. Deniz (Demo)", view.StaffName);
    }

    [Fact]
    public async Task HealthcareStaffAndTphMappingUseTheSameBookingOperation()
    {
        using var s = await BookingScenario.CreateAsync(); var id = await s.BookAsync("patient", s.Lab, 10);
        var view = await s.DetailAsync("patient", id);
        Assert.Equal(20, view.DurationMinutes); Assert.Equal(250.25m, view.Fee); Assert.Equal(20, view.End.Minute);
        Assert.Equal("Laboratuvar Teknikeri", view.ProfessionalDescription);
        using var scope = s.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Single(await db.Set<StaffMember>().OfType<Doctor>().ToListAsync(Token));
        Assert.Equal(2, await db.Set<StaffMember>().OfType<HealthcareStaff>().CountAsync(Token));
    }

    [Fact]
    public async Task OwnershipFacilityGrantsAndDecisionTransitionsAreEnforcedInServices()
    {
        using var s = await BookingScenario.CreateAsync(); var id = await s.BookAsync("patient", s.North, 10);
        await Assert.ThrowsAsync<AccountAccessDeniedException>(() => s.DetailAsync("patient2", id));
        foreach (var actor in new[] { "patient", "lab", "clinicmanager" })
            await Assert.ThrowsAsync<AccountAccessDeniedException>(() => s.DecideAsync(actor, id, AppointmentStatus.Approved));
        await Assert.ThrowsAsync<AccountAccessDeniedException>(() => s.BookAsync("staff", s.North, 9));
        await s.DecideAsync("manager", id, AppointmentStatus.Approved);
        await Assert.ThrowsAsync<BookingException>(() => s.DecideAsync("staff", id, AppointmentStatus.Rejected));
        await Assert.ThrowsAsync<BookingException>(() => s.DecideAsync("manager", id, AppointmentStatus.Pending));
        var unrelated = await s.AsAsync("clinicmanager", p => p.GetRequiredService<AppointmentService>().ListAsync(false, Token)); Assert.Empty(unrelated);
        var clinicId = await s.BookAsync("patient2", s.Clinic, 9);
        await s.DecideAsync("clinicmanager", clinicId, AppointmentStatus.Approved);
        var hospitalList = await s.AsAsync("manager", p => p.GetRequiredService<AppointmentService>().ListAsync(false, Token));
        var clinicList = await s.AsAsync("clinicmanager", p => p.GetRequiredService<AppointmentService>().ListAsync(false, Token));
        var unassignedList = await s.AsAsync("lab", p => p.GetRequiredService<AppointmentService>().ListAsync(false, Token));
        var ownHistory = await s.AsAsync("patient", p => p.GetRequiredService<AppointmentService>().ListAsync(true, Token));
        Assert.Equal(id, Assert.Single(hospitalList).Id); Assert.Equal(clinicId, Assert.Single(clinicList).Id);
        Assert.Empty(unassignedList); Assert.Equal(id, Assert.Single(ownHistory).Id);
    }

    [Fact]
    public async Task DeactivationPreventsNewBookingsButPreservesDecisionAndHistory()
    {
        using var s = await BookingScenario.CreateAsync(); var id = await s.BookAsync("patient", s.North, 10);
        using var scope = s.Factory.Services.CreateScope(); await using var tx = await scope.ServiceProvider.GetRequiredService<IWriteTransaction>().BeginAsync(Token);
        var o = (await scope.ServiceProvider.GetRequiredService<IBookingCatalogRepository>().FindAsync(s.North, Token))!;
        o.Deactivate(); await tx.SaveAsync(Token); await tx.CommitAsync(Token);
        await Assert.ThrowsAsync<BookingException>(() => s.BookAsync("patient2", s.North, 9));
        await s.DecideAsync("staff", id, AppointmentStatus.Approved);
        Assert.Equal(AppointmentStatus.Approved, (await s.DetailAsync("patient", id)).Status);
    }

    [Fact]
    public async Task EmptyCatalogAndMalformedPostsRemainUsableAndSafe()
    {
        using var s = await BookingScenario.CreateAsync();
        using var browser = s.Factory.Browser(); await BrowserForms.LoginAsync(browser, "patient");
        var token = await BrowserForms.TokenAsync(browser, "/patient/book");
        var response = await browser.PostAsync("/patient/book", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["StartTime"] = "invalid" }), Token);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = s.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Set<HealthcareService>().ExecuteUpdateAsync(update => update.SetProperty(x => x.IsActive, false), Token);
        var empty = await browser.GetStringAsync("/patient/book", Token);
        Assert.Contains("Şu anda randevuya açık hizmet bulunmuyor", WebUtility.HtmlDecode(empty));
        Assert.DoesNotContain("name=\"StartTime\"", empty);
    }

    [Fact]
    public async Task RealPatientBookingStaffApprovalAndPatientConfirmationIgnoreForgedTerms()
    {
        using var s = await BookingScenario.CreateAsync();
        using var patient = s.Factory.Browser(); await BrowserForms.LoginAsync(patient, "patient");
        var token = await BrowserForms.TokenAsync(patient, "/patient/book?offeringId=" + s.North + "&date=2030-01-09");
        var saved = await patient.PostAsync("/patient/book", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["OfferingId"] = s.North.ToString(), ["Date"] = "2030-01-09", ["StartTime"] = "10:30", ["CatalogVersion"] = s.Version.ToString(),
            ["PatientId"] = "999", ["Fee"] = "0", ["DurationMinutes"] = "1", ["Status"] = "Approved", ["__RequestVerificationToken"] = token
        }), Token);
        Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode); var location = saved.Headers.Location!.OriginalString;
        Assert.Contains("Onay Bekliyor", WebUtility.HtmlDecode(await patient.GetStringAsync(location, Token)));
        using var otherPatient = s.Factory.Browser(); await BrowserForms.LoginAsync(otherPatient, "patient2");
        Assert.Equal(HttpStatusCode.Redirect, (await otherPatient.GetAsync(location, Token)).StatusCode);
        using var staff = s.Factory.Browser(); await BrowserForms.LoginAsync(staff, "staff");
        Assert.Contains("Genel muayene", WebUtility.HtmlDecode(await staff.GetStringAsync("/work/appointments", Token)));
        Assert.Equal(HttpStatusCode.BadRequest, (await staff.PostAsync(location + "/decision", new FormUrlEncodedContent(new Dictionary<string, string> { ["decision"] = "Approved" }), Token)).StatusCode);
        var decisionToken = await BrowserForms.TokenAsync(staff, location);
        var decision = await staff.PostAsync(location + "/decision", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["decision"] = "Approved", ["__RequestVerificationToken"] = decisionToken }), Token);
        Assert.Equal(HttpStatusCode.Redirect, decision.StatusCode);
        var confirmed = WebUtility.HtmlDecode(await patient.GetStringAsync(location, Token));
        Assert.Contains("Onaylandı", confirmed); Assert.Contains("600,00 TRY", confirmed); Assert.Contains("30 dakika", confirmed);
        Assert.Contains("Onaylandı", WebUtility.HtmlDecode(await patient.GetStringAsync("/patient/appointments", Token)));
    }
}
