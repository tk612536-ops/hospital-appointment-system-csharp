using System.Net;
using System.Text.RegularExpressions;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Infrastructure.Identity;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalAppointments.IntegrationTests;

public sealed class FinalReviewIntegrationTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("1,000000000000000000000000000001")]
    [InlineData("0,000000000000000000000000000001")]
    [InlineData("9999999999,990000000000000000000001")]
    public async Task ExcessFractionalDigitsCannotRoundIntoAnAcceptedFee(string rawFee)
    {
        using var s = await ManagementScenario.CreateAsync(); using var manager = s.Booking.Factory.Browser();
        await BrowserForms.LoginAsync(manager, "manager"); var root = "/manager/facilities/" + s.Facility + "/services";
        var response = await ManagementHttpTests.PostAsync(manager, root, root + "/" + s.Service,
            new() { ["Name"] = "Must not save", ["Duration"] = "30", ["Fee"] = rawFee });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var saved = Assert.Single((await s.PageAsync()).Services); Assert.Equal(77.50m, saved.Fee); Assert.Equal("Configured examination", saved.Name);
    }

    [Fact]
    public async Task RejectedBookingPreservesEmployeeServiceBranchFacilityAndDateSelection()
    {
        using var s = await BookingScenario.CreateAsync(); await s.BookAsync("patient", s.South, 13);
        using var patient = s.Factory.Browser(); await BrowserForms.LoginAsync(patient, "patient2");
        var response = await ManagementHttpTests.PostAsync(patient, "/patient/book?offeringId=" + s.South + "&date=2030-01-09", "/patient/book",
            new() { ["OfferingId"] = s.South.ToString(), ["Date"] = "2030-01-09", ["StartTime"] = "13:00", ["CatalogVersion"] = s.Version.ToString() });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); var html = await response.Content.ReadAsStringAsync(Token);
        var selected = await s.AsAsync("patient2", p => p.GetRequiredService<BookingPageService>().PageAsync(null, null, null, s.South, s.Date, Token));
        foreach (var (field, value) in new[] { ("facility", selected.FacilityId), ("branch", selected.BranchId), ("service", selected.ServiceId), ("employee", selected.OfferingId) })
        {
            var select = Regex.Match(html, "<select[^>]*id=\"" + field + "\"[^>]*>(.*?)</select>", RegexOptions.Singleline);
            Assert.True(select.Success); Assert.Matches("<option[^>]*value=\"" + value + "\"[^>]*selected", select.Groups[1].Value);
        }
        Assert.Matches("id=\"date\"[^>]*value=\"2030-01-09\"", html);
        using var verification = s.Factory.Services.CreateScope();
        Assert.Single(await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<Appointment>().ToListAsync(Token));
    }

    [Fact]
    public async Task InvalidManagerFormsGiveSpecificTurkishMessagesWithoutCredentialValues()
    {
        using var s = await ManagementScenario.CreateAsync(); using var manager = s.Booking.Factory.Browser(); await BrowserForms.LoginAsync(manager, "manager");
        var root = "/manager/facilities/" + s.Facility;
        var terms = await ManagementHttpTests.PostAsync(manager, root + "/services", root + "/services/" + s.Service,
            new() { ["Name"] = "", ["Duration"] = "0", ["Fee"] = "1,000000000000000000000000000001" });
        Assert.Equal(HttpStatusCode.BadRequest, terms.StatusCode); var messages = WebUtility.HtmlDecode(await terms.Content.ReadAsStringAsync(Token));
        Assert.Contains("Hizmet adı girin", messages); Assert.Contains("Süre 1–1439", messages); Assert.Contains("iki ondalık", messages);
        var staff = await ManagementHttpTests.PostAsync(manager, root + "/staff", root + "/staff/doctors",
            new() { ["Name"] = "Synthetic", ["Username"] = "not-created", ["Password"] = "MustNeverEcho10!" });
        Assert.Equal(HttpStatusCode.BadRequest, staff.StatusCode); var feedback = WebUtility.HtmlDecode(await staff.Content.ReadAsStringAsync(Token));
        Assert.Contains("uzmanlık seçin", feedback); Assert.DoesNotContain("MustNeverEcho10!", feedback); Assert.DoesNotContain("System.", feedback);
        await s.Run(async p => Assert.Null(await p.GetRequiredService<UserManager<ApplicationUser>>().FindByNameAsync("not-created")));
    }

    [Fact]
    public async Task RoleRevocationDeniesManagementWithAnAlreadyIssuedCookieAndDirectServiceCall()
    {
        using var s = await ManagementScenario.CreateAsync(); using var manager = s.Booking.Factory.Browser(); await BrowserForms.LoginAsync(manager, "manager");
        var root = "/manager/facilities/" + s.Facility; var csrf = await BrowserForms.TokenAsync(manager, root);
        await s.Run(async p =>
        {
            var users = p.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByNameAsync("manager"))!;
            Assert.True((await users.RemoveFromRoleAsync(user, "MANAGER")).Succeeded);
        });
        var response = await manager.PostAsync(root, new FormUrlEncodedContent(new Dictionary<string, string>
            { ["Name"] = "Forged change", ["Type"] = "Clinic", ["__RequestVerificationToken"] = csrf }), Token);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Contains("access-denied", response.Headers.Location!.ToString());
        await Assert.ThrowsAsync<AccountAccessDeniedException>(() => s.Run(p => p.GetRequiredService<FacilityManagementService>().UpdateAsync(s.Facility, "Forged change", HospitalAppointments.Domain.Facilities.FacilityType.Clinic, Token)));
        using var verification = s.Booking.Factory.Services.CreateScope();
        Assert.Equal("Configured hospital", (await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<HospitalAppointments.Domain.Facilities.Facility>().SingleAsync(f => f.Id == s.Facility, Token)).Name);
    }
}
