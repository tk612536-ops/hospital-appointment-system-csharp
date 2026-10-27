using System.Net;
using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Infrastructure.Identity;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalAppointments.IntegrationTests;

public sealed class ManagementHttpTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    internal static async Task<HttpResponseMessage> PostAsync(HttpClient browser, string page, string action, Dictionary<string, string> fields)
    {
        fields["__RequestVerificationToken"] = await BrowserForms.TokenAsync(browser, page);
        return await browser.PostAsync(action, new FormUrlEncodedContent(fields), Token);
    }
    private static void Redirected(HttpResponseMessage response) => Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

    [Theory]
    [InlineData("")]
    [InlineData("/services")]
    [InlineData("/staff")]
    [InlineData("/schedule")]
    public async Task ManagerPagesRenderLocalTurkishFormsAndRejectOtherRolesOrFacilities(string suffix)
    {
        using var s = await ManagementScenario.CreateAsync(); var url = "/manager/facilities/" + s.Facility + suffix;
        using var anonymous = s.Booking.Factory.Browser(); Redirected(await anonymous.GetAsync(url, Token));
        foreach (var actor in new[] { "patient", "staff", "clinicmanager" })
        {
            using var denied = s.Booking.Factory.Browser(); Redirected(await BrowserForms.LoginAsync(denied, actor));
            var response = await denied.GetAsync(url, Token); Redirected(response); Assert.Contains("access-denied", response.Headers.Location!.ToString());
            var deniedPage = await denied.GetAsync(response.Headers.Location, Token); Assert.Equal(HttpStatusCode.Forbidden, deniedPage.StatusCode);
        }
        using var manager = s.Booking.Factory.Browser(); Redirected(await BrowserForms.LoginAsync(manager, "manager"));
        var allowed = await manager.GetAsync(url, Token); Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        var html = WebUtility.HtmlDecode(await allowed.Content.ReadAsStringAsync(Token));
        Assert.Contains("lang=\"tr\"", html); Assert.Contains("Configured hospital", html); Assert.Contains("__RequestVerificationToken", html);
        Assert.Contains("/vendor/bootstrap/bootstrap.min.css", html); Assert.DoesNotContain("SyntheticDoctor10!", html);
        Assert.DoesNotContain("https://cdn", html);
    }

    [Fact]
    public async Task EveryManagerMutationRequiresAntiforgeryAndWrongRolePostsNeverReachServices()
    {
        using var s = await ManagementScenario.CreateAsync(); var root = "/manager/facilities/" + s.Facility;
        string[] actions = ["/manager/facilities", root, root + "/branches", root + "/branches/" + s.Branch,
            root + "/departments", root + "/departments/" + s.Department, root + "/services", root + "/services/" + s.Service,
            root + "/services/" + s.Service + "/active", root + "/staff/specialties", root + "/staff/doctors", root + "/staff/healthcare",
            root + "/staff/doctors/" + s.Doctor, root + "/staff/healthcare/" + s.Doctor,
            root + "/staff/offerings", root + "/staff/offerings/" + s.Offering + "/active", root + "/schedule/hours",
            root + "/schedule/availability", root + "/schedule/availability/" + s.Window, root + "/schedule/availability/" + s.Window + "/remove"];
        using var manager = s.Booking.Factory.Browser(); await BrowserForms.LoginAsync(manager, "manager");
        foreach (var action in actions) Assert.Equal(HttpStatusCode.BadRequest, (await manager.PostAsync(action, new FormUrlEncodedContent([]), Token)).StatusCode);
        foreach (var role in new[] { "patient", "staff" })
        {
            using var browser = s.Booking.Factory.Browser(); await BrowserForms.LoginAsync(browser, role);
            var csrf = await BrowserForms.TokenAsync(browser, "/workspace");
            foreach (var action in actions)
            {
                var response = await browser.PostAsync(action, new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = csrf }), Token);
                Redirected(response); Assert.Contains("access-denied", response.Headers.Location!.ToString());
            }
        }
        Assert.True(Assert.Single((await s.PageAsync()).Services).Active);
    }

    [Theory]
    [InlineData("0", "10,00")]
    [InlineData("30", "0,001")]
    [InlineData("30", "-1")]
    [InlineData("not-a-number", "10,00")]
    [InlineData("30", "")]
    [InlineData("30", "12.50")]
    [InlineData("30", "1.234,50")]
    public async Task ManagerHttpValidationRejectsInvalidTermsWithoutChangingStoredValues(string duration, string fee)
    {
        using var s = await ManagementScenario.CreateAsync(); using var manager = s.Booking.Factory.Browser(); await BrowserForms.LoginAsync(manager, "manager");
        var root = "/manager/facilities/" + s.Facility + "/services";
        var response = await PostAsync(manager, root, root + "/" + s.Service, new() { ["Name"] = "Invalid", ["Duration"] = duration, ["Fee"] = fee });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(77.50m, Assert.Single((await s.PageAsync()).Services).Fee);
    }

    [Fact]
    public async Task ForgedEditDepartmentCannotRelocateServiceAndManagerTextIsRazorEncoded()
    {
        using var s = await ManagementScenario.CreateAsync(); var second = await s.SecondBranchAsync();
        var otherDepartment = (await s.PageAsync()).Departments.Single(d => d.BranchId == second.Branch).Id;
        using var manager = s.Booking.Factory.Browser(); await BrowserForms.LoginAsync(manager, "manager");
        var root = "/manager/facilities/" + s.Facility + "/services";
        Redirected(await PostAsync(manager, root, root + "/" + s.Service, new() { ["Name"] = "<script>alert('demo')</script>", ["Duration"] = "20", ["Fee"] = "12,25", ["DepartmentId"] = otherDepartment.ToString() }));
        var page = await s.PageAsync(); Assert.Equal(s.Department, page.Services.Single(x => x.Id == s.Service).DepartmentId);
        var html = await manager.GetStringAsync(root, Token); Assert.DoesNotContain("<script>alert", html); Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public async Task StructureStaffAndActivationEditFormsSaveRealChanges()
    {
        using var s = await ManagementScenario.CreateAsync(); using var manager = s.Booking.Factory.Browser(); await BrowserForms.LoginAsync(manager, "manager");
        var root = "/manager/facilities/" + s.Facility;
        Redirected(await PostAsync(manager, root, root, new() { ["Name"] = "Updated hospital", ["Type"] = "Hospital" }));
        Redirected(await PostAsync(manager, root, root + "/branches", new() { ["Name"] = "Added branch" }));
        var branch = (await s.PageAsync()).Branches.Single(b => b.Name == "Added branch").Id;
        Redirected(await PostAsync(manager, root, root + "/branches/" + branch, new() { ["Name"] = "Renamed branch" }));
        Redirected(await PostAsync(manager, root, root + "/departments/" + s.Department, new() { ["Name"] = "Updated department" }));
        Redirected(await PostAsync(manager, root + "/staff", root + "/staff/healthcare", new() { ["Name"] = "New worker", ["JobTitle"] = "Nurse" }));
        var employee = (await s.PageAsync()).Staff.Single(w => !w.IsDoctor).Id;
        Redirected(await PostAsync(manager, root + "/staff", root + "/staff/healthcare/" + employee, new() { ["Name"] = "Updated worker", ["JobTitle"] = "Physiotherapist", ["Active"] = "true" }));
        Redirected(await PostAsync(manager, root + "/staff", root + "/staff/doctors/" + s.Doctor, new() { ["Name"] = "Updated doctor", ["SpecialtyId"] = s.Specialty.ToString(), ["Active"] = "true" }));
        foreach (var active in new[] { "false", "true" })
        {
            Redirected(await PostAsync(manager, root + "/services", root + "/services/" + s.Service + "/active", new() { ["active"] = active }));
            Redirected(await PostAsync(manager, root + "/staff", root + "/staff/offerings/" + s.Offering + "/active", new() { ["active"] = active }));
        }
        Redirected(await PostAsync(manager, root + "/schedule", root + "/schedule/availability/" + s.Window, new() { ["Date"] = "2030-01-09", ["Start"] = "09:30", ["End"] = "11:30" }));
        Assert.Equal(new TimeOnly(9, 30), Assert.Single((await s.PageAsync()).Availability).Starts);
        Redirected(await PostAsync(manager, root + "/schedule", root + "/schedule/availability/" + s.Window + "/remove", new()));
        var page = await s.PageAsync(); Assert.Empty(page.Availability); Assert.Equal("Updated hospital", page.Facility.Name);
        Assert.Contains(page.Branches, b => b.Name == "Renamed branch"); Assert.Equal("Updated department", page.Departments.Single().Name);
        Assert.Equal("Physiotherapist", page.Staff.Single(w => w.Id == employee).Profession); Assert.True(page.Offerings.Single().Active);
    }

    [Fact]
    public async Task ConfigurationBusyResponseLeavesNoPartialStaffAccountAndFreshRetrySucceeds()
    {
        using var s = await ManagementScenario.CreateAsync(); using var manager = s.Booking.Factory.Browser(); await BrowserForms.LoginAsync(manager, "manager");
        var root = "/manager/facilities/" + s.Facility + "/staff";
        var fields = new Dictionary<string, string> { ["Name"] = "Busy demo", ["JobTitle"] = "Nurse", ["Username"] = "busy-demo", ["Password"] = ManagementScenario.Password,
            ["__RequestVerificationToken"] = await BrowserForms.TokenAsync(manager, root) };
        await using (var holder = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = s.Booking.Factory.DatabasePath, ForeignKeys = true }.ToString()))
        {
            await holder.OpenAsync(Token); await using var held = holder.BeginTransaction(deferred: false);
            var response = await manager.PostAsync(root + "/healthcare", new FormUrlEncodedContent(fields), Token);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.Equal(TimeSpan.FromSeconds(1), response.Headers.RetryAfter!.Delta);
            Assert.Contains("Sistem şu anda meşgul", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(Token)));
            await s.Run(async p => Assert.Null(await p.GetRequiredService<UserManager<ApplicationUser>>().FindByNameAsync("busy-demo")));
            Assert.Single((await s.PageAsync()).Staff);
        }
        Redirected(await manager.PostAsync(root + "/healthcare", new FormUrlEncodedContent(fields), Token));
        Assert.Equal(2, (await s.PageAsync()).Staff.Count);
    }

    [Fact]
    public async Task ForeignBranchPostCannotBeUsedToAddADepartmentUnderAnOwnedUrl()
    {
        using var s = await ManagementScenario.CreateAsync(); using var manager = s.Booking.Factory.Browser(); await BrowserForms.LoginAsync(manager, "manager");
        var root = "/manager/facilities/" + s.Facility;
        var foreign = await s.Run(p => p.GetRequiredService<ManagementQueryService>().FacilitiesAsync(Token));
        var foreignPage = await s.Run(p => p.GetRequiredService<ManagementQueryService>().PageAsync(foreign.First(f => f.Id != s.Facility).Id, Token));
        var response = await PostAsync(manager, root, root + "/departments", new() { ["Name"] = "Forged", ["BranchId"] = foreignPage.Branches.First().Id.ToString() });
        Redirected(response); Assert.Contains("access-denied", response.Headers.Location!.ToString());
        Assert.Single((await s.PageAsync()).Departments);
    }

    [Fact]
    public async Task RealManagerFormsLeadToNewDoctorApprovalAndPersistAcrossRestart()
    {
        string databasePath; long facilityId; long appointmentId; long availabilityId;
        using (var s = await BookingScenario.CreateAsync())
        {
            databasePath = s.Factory.DatabasePath;
            using var manager = s.Factory.Browser(); Redirected(await BrowserForms.LoginAsync(manager, "manager"));
            var created = await PostAsync(manager, "/manager/facilities", "/manager/facilities", new() { ["Name"] = "Form demo clinic", ["Type"] = "Clinic", ["FirstBranchName"] = "Form main" });
            Redirected(created); facilityId = long.Parse(created.Headers.Location!.ToString().Split('/').Last());
            var root = "/manager/facilities/" + facilityId;
            Task<ManagementPage> Page() => s.AsAsync("manager", p => p.GetRequiredService<ManagementQueryService>().PageAsync(facilityId, Token));
            var branch = (await Page()).Branches.Single().Id;
            Redirected(await PostAsync(manager, root, root + "/departments", new() { ["Name"] = "Form care", ["BranchId"] = branch.ToString() }));
            var department = (await Page()).Departments.Single().Id;
            Redirected(await PostAsync(manager, root + "/services", root + "/services", new() { ["Name"] = "Form examination", ["DepartmentId"] = department.ToString(), ["Duration"] = "30", ["Fee"] = "77,50" }));
            Redirected(await PostAsync(manager, root + "/staff", root + "/staff/specialties", new() { ["Name"] = "Form specialty" }));
            var specialty = (await Page()).Specialties.Single(x => x.Label == "Form specialty").Id;
            Redirected(await PostAsync(manager, root + "/staff", root + "/staff/doctors", new() { ["Name"] = "Dr. Form Demo", ["SpecialtyId"] = specialty.ToString(), ["Username"] = "form-doctor", ["Password"] = ManagementScenario.Password,
                ["Role"] = "MANAGER", ["AccountId"] = "1" })); // ignored forged elevation/attachment
            var page = await Page(); var doctor = page.Staff.Single().Id; var service = page.Services.Single().Id;
            Redirected(await PostAsync(manager, root + "/staff", root + "/staff/offerings", new() { ["StaffId"] = doctor.ToString(), ["ServiceId"] = service.ToString() }));
            Redirected(await PostAsync(manager, root + "/schedule", root + "/schedule/hours", new() { ["BranchId"] = branch.ToString(), ["Day"] = "Wednesday", ["Periods[0].Opens"] = "09:00", ["Periods[0].Closes"] = "12:00", ["Periods[1].Opens"] = "13:00", ["Periods[1].Closes"] = "17:00" }));
            Redirected(await PostAsync(manager, root + "/schedule", root + "/schedule/availability", new() { ["BranchId"] = branch.ToString(), ["StaffId"] = doctor.ToString(), ["Date"] = "2030-01-09", ["Start"] = "09:00", ["End"] = "12:00" }));
            page = await Page(); availabilityId = page.Availability.Single().Id; var offering = page.Offerings.Single().Id;
            var version = await s.AsAsync("manager", async p => (await p.GetRequiredService<IBookingCatalogRepository>().FindAsync(offering, Token))!.Service.Version);
            using var patient = s.Factory.Browser(); Redirected(await BrowserForms.LoginAsync(patient, "patient"));
            var booked = await PostAsync(patient, "/patient/book?offeringId=" + offering + "&date=2030-01-09", "/patient/book", new() { ["OfferingId"] = offering.ToString(), ["Date"] = "2030-01-09", ["StartTime"] = "10:00", ["CatalogVersion"] = version.ToString() });
            Redirected(booked); appointmentId = long.Parse(booked.Headers.Location!.ToString().Split('/').Last());
            using var doctorBrowser = s.Factory.Browser(); Redirected(await BrowserForms.LoginAsync(doctorBrowser, "form-doctor", ManagementScenario.Password));
            var details = "/appointments/" + appointmentId;
            Redirected(await PostAsync(doctorBrowser, details, details + "/decision", new() { ["decision"] = "Approved" }));
            Assert.Contains("Onaylandı", WebUtility.HtmlDecode(await patient.GetStringAsync(details, Token)));
            var unsafeEdit = await PostAsync(manager, root + "/schedule", root + "/schedule/availability/" + availabilityId, new() { ["Date"] = "2030-01-09", ["Start"] = "09:00", ["End"] = "10:15" });
            Assert.Equal(HttpStatusCode.Conflict, unsafeEdit.StatusCode);
            await s.AsAsync("manager", async p =>
            {
                var users = p.GetRequiredService<UserManager<ApplicationUser>>(); var account = (await users.FindByNameAsync("form-doctor"))!;
                Assert.Equal(["STAFF"], await users.GetRolesAsync(account)); return true;
            });
        }
        using var restarted = new ClinicFactory("Demo", true, databasePath, seedBookings: true, clock: new FixedClinicTime());
        using var verification = restarted.Services.CreateScope(); var db = verification.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var saved = await db.Set<Appointment>().SingleAsync(a => a.Id == appointmentId, Token);
        Assert.Equal(AppointmentStatus.Approved, saved.Status); Assert.Equal(77.50m, saved.BookedFee); Assert.Equal(30, saved.BookedDurationMinutes);
        var savedWindow = await db.Set<HospitalAppointments.Domain.Staff.AvailabilityWindow>().SingleAsync(w => w.Id == availabilityId, Token);
        Assert.Equal(9, savedWindow.End.Hour); // UTC noon local, unsafe change never committed
        using var returningPatient = restarted.Browser(); Redirected(await BrowserForms.LoginAsync(returningPatient, "patient"));
        Assert.Contains("Onaylandı", WebUtility.HtmlDecode(await returningPatient.GetStringAsync("/appointments/" + appointmentId, Token)));
        using var returningManager = restarted.Browser(); Redirected(await BrowserForms.LoginAsync(returningManager, "manager"));
        Assert.Contains("Form demo clinic", await returningManager.GetStringAsync("/manager/facilities/" + facilityId, Token));
        using var returningDoctor = restarted.Browser(); Redirected(await BrowserForms.LoginAsync(returningDoctor, "form-doctor", ManagementScenario.Password));
    }
}
