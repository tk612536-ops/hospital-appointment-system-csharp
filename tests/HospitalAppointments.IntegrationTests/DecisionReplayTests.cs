using System.Net;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalAppointments.IntegrationTests;

public sealed class DecisionReplayTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(AppointmentStatus.Approved)]
    [InlineData(AppointmentStatus.Rejected)]
    public async Task StaleDecisionFormCannotRewriteCommittedStateOrAudit(AppointmentStatus decision)
    {
        using var s = await BookingScenario.CreateAsync();
        var id = await s.BookAsync("patient", s.North, 10);
        var details = "/appointments/" + id;
        using var staff = s.Factory.Browser();
        Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.LoginAsync(staff, "staff")).StatusCode);
        Assert.Contains("name=\"decision\"", await staff.GetStringAsync(details, Token));
        var staleToken = await BrowserForms.TokenAsync(staff, details);

        using var manager = s.Factory.Browser();
        Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.LoginAsync(manager, "manager")).StatusCode);
        var committed = await ManagementHttpTests.PostAsync(manager, details, details + "/decision",
            new() { ["decision"] = decision.ToString() });
        Assert.Equal(HttpStatusCode.Redirect, committed.StatusCode);
        var original = await SavedAsync();
        var clock = (FixedClinicTime)s.Factory.Services.GetRequiredService<TimeProvider>();
        Assert.Equal(decision, original.Status);
        Assert.Equal(2, original.Version);
        Assert.Equal(clock.GetUtcNow().UtcDateTime, original.DecidedAt);
        using (var scope = s.Factory.Services.CreateScope())
            Assert.Equal(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users
                .Where(u => u.UserName == "manager").Select(u => u.Id).SingleAsync(Token), original.DecidedByAccountId);

        var opposite = decision == AppointmentStatus.Approved ? AppointmentStatus.Rejected : AppointmentStatus.Approved;
        foreach (var replay in new[] { decision, opposite })
        {
            // Different actor and instant expose accidental audit rewrites, even for the same decision.
            clock.Now = clock.Now.AddMinutes(5);
            var response = await staff.PostAsync(details + "/decision", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["decision"] = replay.ToString(), ["__RequestVerificationToken"] = staleToken
            }), Token);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Contains("daha önce karar verilmiş", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(Token)));
            Assert.Equivalent(original, await SavedAsync(), strict: true);
        }
        Assert.DoesNotContain("name=\"decision\"", await staff.GetStringAsync(details, Token));
        Assert.Equal(decision, (await s.DetailAsync("patient", id)).Status);

        async Task<Appointment> SavedAsync()
        {
            using var scope = s.Factory.Services.CreateScope();
            // A fresh database query also proves the failed requests inserted no extra appointment.
            return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<Appointment>()
                .AsNoTracking().SingleAsync(Token);
        }
    }
}
