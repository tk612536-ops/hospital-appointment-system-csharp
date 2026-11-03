using System.Net;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Application.Transactions;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Staff;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalAppointments.IntegrationTests;

public sealed class GrantRevocationTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(AppointmentStatus.Pending)]
    [InlineData(AppointmentStatus.Approved)]
    public async Task RevokedFacilityGrantDeniesAppointmentAccessWithoutChangingRoleOrAudit(AppointmentStatus status)
    {
        using var s = await BookingScenario.CreateAsync();
        var id = await s.BookAsync("patient", s.North, 10);
        if (status == AppointmentStatus.Approved) await s.DecideAsync("staff", id, status);
        var original = await SavedAsync();
        var details = "/appointments/" + id;
        using var manager = s.Factory.Browser();
        Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.LoginAsync(manager, "manager")).StatusCode);
        var oldToken = await BrowserForms.TokenAsync(manager, details);
        Assert.Equal(id, Assert.Single(await s.AsAsync("manager",
            p => p.GetRequiredService<AppointmentService>().ListAsync(false, Token))).Id);

        // Administrative fixture change only: there is no public grant-administration endpoint.
        using (var scope = s.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await using var transaction = await scope.ServiceProvider.GetRequiredService<IWriteTransaction>().BeginAsync(Token);
            var accountId = await db.Users.Where(u => u.UserName == "manager").Select(u => u.Id).SingleAsync(Token);
            var facilityId = await db.Set<StaffOffering>().Where(o => o.Id == s.North).Select(o => o.Service.Department.Branch.FacilityId).SingleAsync(Token);
            db.Remove(await db.Set<ManagerFacilityGrant>().SingleAsync(g => g.AccountId == accountId && g.FacilityId == facilityId, Token));
            await transaction.SaveAsync(Token);
            await transaction.CommitAsync(Token);
        }

        // Same browser/cookie, no new login: the role still works, but no longer grants this object.
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync("/manager", Token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync("/work/appointments", Token)).StatusCode);
        Assert.Empty(await s.AsAsync("manager", p => p.GetRequiredService<AppointmentService>().ListAsync(false, Token)));
        await ForbiddenAsync(await manager.GetAsync(details, Token));
        Assert.Equivalent(original, await SavedAsync(), strict: true);
        var denied = await manager.PostAsync(details + "/decision", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["decision"] = "Rejected", ["__RequestVerificationToken"] = oldToken
        }), Token);
        await ForbiddenAsync(denied);
        Assert.Equivalent(original, await SavedAsync(), strict: true);

        await Assert.ThrowsAsync<AccountAccessDeniedException>(() => s.DetailAsync("manager", id));
        await Assert.ThrowsAsync<AccountAccessDeniedException>(() => s.DecideAsync("manager", id, AppointmentStatus.Rejected));
        Assert.Equivalent(original, await SavedAsync(), strict: true);
        Assert.Equal(status, (await s.DetailAsync("patient", id)).Status);
        Assert.Equal(status, (await s.DetailAsync("staff", id)).Status);

        async Task ForbiddenAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/account/access-denied", response.Headers.Location!.ToString());
            Assert.Equal(HttpStatusCode.Forbidden, (await manager.GetAsync(response.Headers.Location, Token)).StatusCode);
        }
        async Task<Appointment> SavedAsync()
        {
            using var scope = s.Factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<Appointment>()
                .AsNoTracking().SingleAsync(Token);
        }
    }
}
