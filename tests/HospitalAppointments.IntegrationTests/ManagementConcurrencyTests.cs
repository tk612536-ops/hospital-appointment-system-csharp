using System.Collections.Concurrent;
using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Staff;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HospitalAppointments.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace HospitalAppointments.IntegrationTests;

public sealed class ManagementConcurrencyTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<bool[]> CompeteAsync(ManagementScenario scenario, string actor1, Func<IServiceProvider, Task> first,
        string actor2, Func<IServiceProvider, Task> second)
    {
        using var start = new Barrier(2); var connections = new ConcurrentBag<System.Data.Common.DbConnection>();
        Task<bool> Launch(string actor, Func<IServiceProvider, Task> command) => Task.Run(() => scenario.Run(async p =>
        {
            connections.Add(p.GetRequiredService<ApplicationDbContext>().Database.GetDbConnection());
            if (!start.SignalAndWait(TimeSpan.FromSeconds(10), Token)) throw new TimeoutException("Configuration race start timed out.");
            try { await command(p); return true; }
            catch (BookingException) { return false; }
            catch (ConfigurationException) { return false; }
        }, actor), Token);
        var result = await Task.WhenAll(Launch(actor1, first), Launch(actor2, second)).WaitAsync(TimeSpan.FromSeconds(20), Token);
        Assert.Equal(2, connections.Count); Assert.NotSame(connections.First(), connections.Last());
        Assert.All(connections, connection => Assert.Equal(scenario.Booking.Factory.DatabasePath, connection.DataSource));
        await VerifyPersistedInvariantsAsync(scenario); return result;
    }

    private static async Task VerifyPersistedInvariantsAsync(ManagementScenario s)
    {
        using var scope = s.Booking.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var time = scope.ServiceProvider.GetRequiredService<ClinicTime>();
        var shifts = await db.Set<AvailabilityWindow>().AsNoTracking().Where(w => w.Staff.FacilityId == s.Facility).ToListAsync(Token);
        var hours = await db.Set<OpeningWindow>().AsNoTracking().Where(w => w.Branch.FacilityId == s.Facility).ToListAsync(Token);
        foreach (var shift in shifts)
        {
            var date = DateOnly.FromDateTime(time.Local(shift.Start));
            Assert.True(shift.Interval.IsCoveredBy(hours.Where(w => w.BranchId == shift.BranchId && w.Weekday == date.DayOfWeek).Select(w => time.Interval(date, w.OpensAt, w.ClosesAt))));
            Assert.DoesNotContain(shifts, other => other.Id != shift.Id && other.StaffId == shift.StaffId && other.Interval.Overlaps(shift.Interval));
        }
        var bookings = await db.Set<Appointment>().AsNoTracking().Include(a => a.Offering).ThenInclude(o => o.Service).ThenInclude(x => x.Department).ThenInclude(d => d.Branch)
            .Where(a => a.Offering.Staff.FacilityId == s.Facility && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved)).ToListAsync(Token);
        foreach (var booking in bookings)
        {
            var date = DateOnly.FromDateTime(time.Local(booking.Start));
            Assert.True(booking.Interval.IsCoveredBy(shifts.Where(w => w.StaffId == booking.Offering.StaffId && w.BranchId == booking.Offering.Branch.Id).Select(w => w.Interval)));
            Assert.True(booking.Interval.IsCoveredBy(hours.Where(w => w.BranchId == booking.Offering.Branch.Id && w.Weekday == date.DayOfWeek).Select(w => time.Interval(date, w.OpensAt, w.ClosesAt))));
            Assert.Equal(TimeSpan.FromMinutes(booking.BookedDurationMinutes), booking.End - booking.Start);
            Assert.DoesNotContain(bookings, other => other.Id != booking.Id && (other.Offering.StaffId == booking.Offering.StaffId || other.PatientId == booking.PatientId) && other.Interval.Overlaps(booking.Interval));
        }
    }

    [Fact]
    public async Task BookingVersusAvailabilityShrinkHasExactlyOneSuccessfulOutcome()
    {
        using var s = await ManagementScenario.CreateAsync();
        var result = await CompeteAsync(s, "patient", async p => await p.GetRequiredService<AppointmentService>().BookAsync(new(s.Offering, s.Date, new(10, 30), s.Version), Token),
            "manager", p => p.GetRequiredService<ScheduleManagementService>().UpdateAvailabilityAsync(s.Facility, s.Window, s.Date, new(9, 0), new(10, 0), Token));
        Assert.Single(result, success => success);
        using var verification = s.Booking.Factory.Services.CreateScope();
        Assert.Equal(result[0] ? 1 : 0, await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<Appointment>().CountAsync(Token));
        Assert.Equal(result[1] ? new TimeOnly(10, 0) : new(12, 0), Assert.Single((await s.PageAsync()).Availability).Ends);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BookingVersusOpeningHoursPreservesBothShiftsAndReservations(bool unsafeChange)
    {
        using var s = await ManagementScenario.CreateAsync();
        var result = await CompeteAsync(s, "patient", async p => await p.GetRequiredService<AppointmentService>().BookAsync(new(s.Offering, s.Date, new(10, 30), s.Version), Token),
            "manager", p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(s.Facility, s.Branch, s.Date.DayOfWeek, [new(new(9, 0), new(unsafeChange ? 10 : 12, 0))], Token));
        Assert.True(result[0]); Assert.Equal(!unsafeChange, result[1]); // existing shift also prevents the unsafe hours edit before a booking exists
    }

    [Fact]
    public async Task BookingVersusServiceDeactivationKeepsAnyEarlierReservation()
    {
        using var s = await ManagementScenario.CreateAsync();
        var result = await CompeteAsync(s, "patient", async p => await p.GetRequiredService<AppointmentService>().BookAsync(new(s.Offering, s.Date, new(10, 0), s.Version), Token),
            "manager", p => p.GetRequiredService<CatalogManagementService>().ChangeActiveAsync(s.Facility, s.Service, false, Token));
        Assert.True(result[1]); Assert.False(Assert.Single((await s.PageAsync()).Services).Active);
        using var verification = s.Booking.Factory.Services.CreateScope();
        var saved = await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<Appointment>().ToListAsync(Token);
        Assert.Equal(result[0] ? 1 : 0, saved.Count);
        if (result[0]) { Assert.Equal(77.50m, saved[0].BookedFee); Assert.Equal(AppointmentStatus.Pending, saved[0].Status); }
        await Assert.ThrowsAsync<BookingException>(() => s.BookAsync(11));
    }

    [Fact]
    public async Task CatalogEditVersusBookingNeverProducesMixedOrStaleTerms()
    {
        using var s = await ManagementScenario.CreateAsync();
        var result = await CompeteAsync(s, "patient", async p => await p.GetRequiredService<AppointmentService>().BookAsync(new(s.Offering, s.Date, new(10, 0), s.Version), Token),
            "manager", p => p.GetRequiredService<CatalogManagementService>().UpdateAsync(s.Facility, s.Service, "Revised", 45, 200.25m, Token));
        Assert.True(result[1]);
        using var verification = s.Booking.Factory.Services.CreateScope();
        var saved = await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<Appointment>().ToListAsync(Token);
        Assert.Equal(result[0] ? 1 : 0, saved.Count);
        if (result[0]) { Assert.Equal(30, saved[0].BookedDurationMinutes); Assert.Equal(77.50m, saved[0].BookedFee); Assert.Equal("Configured examination", saved[0].BookedServiceName); }
        else await Assert.ThrowsAsync<BookingException>(() => s.BookAsync(11));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompetingOverlappingShiftCreationHasExactlyOneWinner(bool acrossBranches)
    {
        using var s = await ManagementScenario.CreateAsync(shift: false);
        var otherBranch = acrossBranches ? (await s.SecondBranchAsync()).Branch : s.Branch;
        var result = await CompeteAsync(s, "manager", async p => await p.GetRequiredService<ScheduleManagementService>().AddAvailabilityAsync(s.Facility, s.Branch, s.Doctor, s.Date, new(9, 0), new(11, 0), Token),
            "manager", async p => await p.GetRequiredService<ScheduleManagementService>().AddAvailabilityAsync(s.Facility, otherBranch, s.Doctor, s.Date, new(10, 0), new(12, 0), Token));
        Assert.Single(result, success => success); Assert.Single((await s.PageAsync()).Availability);
    }

    [Fact]
    public async Task HoursChangeVersusNewAvailabilityCannotLeaveAnInvalidShift()
    {
        using var s = await ManagementScenario.CreateAsync(shift: false);
        var result = await CompeteAsync(s, "manager", async p => await p.GetRequiredService<ScheduleManagementService>().AddAvailabilityAsync(s.Facility, s.Branch, s.Doctor, s.Date, new(9, 0), new(11, 0), Token),
            "manager", p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(s.Facility, s.Branch, s.Date.DayOfWeek, [new(new(10, 0), new(12, 0))], Token));
        Assert.Single(result, success => success);
        Assert.Equal(result[0] ? 1 : 0, (await s.PageAsync()).Availability.Count);
    }

    [Fact]
    public async Task CompetingStaffRegistrationsCannotDuplicateOrStealALogin()
    {
        using var s = await ManagementScenario.CreateAsync();
        var result = await CompeteAsync(s, "manager", async p => await p.GetRequiredService<StaffManagementService>().RegisterHealthcareStaffAsync(s.Facility, "First", "Nurse", "racing-user", ManagementScenario.Password, Token),
            "manager", async p => await p.GetRequiredService<StaffManagementService>().RegisterHealthcareStaffAsync(s.Facility, "Second", "Technician", "racing-user", ManagementScenario.Password, Token));
        Assert.Single(result, success => success);
        await s.Run(async p =>
        {
            var users = p.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByNameAsync("racing-user"))!;
            Assert.Equal(["STAFF"], await users.GetRolesAsync(user));
            Assert.Equal(1, await p.GetRequiredService<ApplicationDbContext>().Set<StaffMember>().CountAsync(w => w.AccountId == user.Id, Token));
        });
        Assert.Equal(2, (await s.PageAsync()).Staff.Count);
    }

    [Fact]
    public async Task CompetingEditsOfTheSameShiftSerializeAndPreserveExistingBooking()
    {
        using var s = await ManagementScenario.CreateAsync(); await s.BookAsync();
        var result = await CompeteAsync(s, "manager", p => p.GetRequiredService<ScheduleManagementService>().UpdateAvailabilityAsync(s.Facility, s.Window, s.Date, new(10, 0), new(11, 0), Token),
            "manager", p => p.GetRequiredService<ScheduleManagementService>().UpdateAvailabilityAsync(s.Facility, s.Window, s.Date, new(9, 30), new(10, 30), Token));
        Assert.All(result, Assert.True); Assert.Single((await s.PageAsync()).Availability);
    }
}
