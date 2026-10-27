using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Domain.Appointments;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalAppointments.IntegrationTests;

public sealed class ManagementScheduleTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(AppointmentStatus.Pending)]
    [InlineData(AppointmentStatus.Approved)]
    public async Task ShrinkingRemovingOrMovingAvailabilityCannotInvalidateReservations(AppointmentStatus status)
    {
        using var s = await ManagementScenario.CreateAsync(); var id = await s.BookAsync();
        if (status == AppointmentStatus.Approved) await s.Booking.DecideAsync(ManagementScenario.DoctorLogin, id, status);
        await Assert.ThrowsAsync<ConfigurationException>(() => s.UpdateWindowAsync(new(9, 0), new(10, 15)));
        await Assert.ThrowsAsync<ConfigurationException>(() => s.UpdateWindowAsync(new(10, 15), new(12, 0)));
        await s.Run(p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(s.Facility, s.Branch, s.Date.AddDays(7).DayOfWeek, [new(new(9, 0), new(17, 0))], Token));
        await Assert.ThrowsAsync<ConfigurationException>(() => s.Run(p => p.GetRequiredService<ScheduleManagementService>().UpdateAvailabilityAsync(s.Facility, s.Window, s.Date.AddDays(7), new(9, 0), new(12, 0), Token)));
        await Assert.ThrowsAsync<ConfigurationException>(() => s.Run(p => p.GetRequiredService<ScheduleManagementService>().RemoveAvailabilityAsync(s.Facility, s.Window, Token)));
        var window = Assert.Single((await s.PageAsync()).Availability);
        Assert.Equal(s.Date, window.Date); Assert.Equal(new TimeOnly(9, 0), window.Starts); Assert.Equal(new TimeOnly(12, 0), window.Ends);
        await s.UpdateWindowAsync(new(9, 30), new(11, 30)); // still covers every minute
        Assert.Equal(status, (await s.Booking.DetailAsync("patient", id)).Status);
    }

    [Fact]
    public async Task RejectedReservationsReleaseAvailabilityAndHistoricalRowsRemain()
    {
        using var s = await ManagementScenario.CreateAsync(); var id = await s.BookAsync();
        await s.Booking.DecideAsync(ManagementScenario.DoctorLogin, id, AppointmentStatus.Rejected);
        await s.Run(p => p.GetRequiredService<ScheduleManagementService>().RemoveAvailabilityAsync(s.Facility, s.Window, Token));
        await s.Run(p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(s.Facility, s.Branch, s.Date.DayOfWeek, [], Token));
        Assert.Empty((await s.PageAsync()).Availability); Assert.Equal(AppointmentStatus.Rejected, (await s.Booking.DetailAsync("patient", id)).Status);
    }

    [Fact]
    public async Task ScheduleProtectionUsesBookedDurationRatherThanCurrentCatalogDuration()
    {
        using var s = await ManagementScenario.CreateAsync(); var id = await s.BookAsync();
        await s.Run(p => p.GetRequiredService<CatalogManagementService>().UpdateAsync(s.Facility, s.Service, "Longer", 45, 123.45m, Token));
        await s.UpdateWindowAsync(new(10, 0), new(10, 30));
        await s.Run(p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(s.Facility, s.Branch, s.Date.DayOfWeek, [new(new(10, 0), new(10, 30))], Token));
        Assert.Equal(30, (await s.Booking.DetailAsync("patient", id)).DurationMinutes);
        await Assert.ThrowsAsync<ConfigurationException>(() => s.UpdateWindowAsync(new(10, 0), new(10, 15)));
    }

    [Fact]
    public async Task OngoingReservationsStayProtectedAndCompletedAppointmentsDoNotFreezeSchedules()
    {
        using var s = await ManagementScenario.CreateAsync(); var id = await s.BookAsync();
        await s.Booking.DecideAsync(ManagementScenario.DoctorLogin, id, AppointmentStatus.Approved);
        var clock = (FixedClinicTime)s.Booking.Factory.Services.GetRequiredService<TimeProvider>();
        clock.Now = new(2030, 1, 9, 7, 15, 0, TimeSpan.Zero); // 10:15 local, halfway through the booking
        await Assert.ThrowsAsync<ConfigurationException>(() => s.Run(p => p.GetRequiredService<ScheduleManagementService>().RemoveAvailabilityAsync(s.Facility, s.Window, Token)));
        await Assert.ThrowsAsync<ConfigurationException>(() => s.Run(p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(s.Facility, s.Branch, s.Date.DayOfWeek, [], Token)));
        clock.Now = new(2030, 1, 9, 10, 0, 0, TimeSpan.Zero); // after the complete shift
        await s.Run(p => p.GetRequiredService<ScheduleManagementService>().RemoveAvailabilityAsync(s.Facility, s.Window, Token));
        await s.Run(p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(s.Facility, s.Branch, s.Date.DayOfWeek, [], Token));
        Assert.Equal(AppointmentStatus.Approved, (await s.Booking.DetailAsync("patient", id)).Status);
    }

    [Fact]
    public async Task ShiftsCannotOverlapAcrossBranchesOrDuplicateButMayBeAdjacent()
    {
        using var s = await ManagementScenario.CreateAsync(); var second = await s.SecondBranchAsync();
        await Assert.ThrowsAsync<ConfigurationException>(() => s.Run(p => p.GetRequiredService<ScheduleManagementService>().AddAvailabilityAsync(s.Facility, s.Branch, s.Doctor, s.Date, new(10, 0), new(11, 0), Token)));
        await Assert.ThrowsAsync<ConfigurationException>(() => s.Run(p => p.GetRequiredService<ScheduleManagementService>().AddAvailabilityAsync(s.Facility, second.Branch, s.Doctor, s.Date, new(11, 0), new(13, 0), Token)));
        await s.Run(p => p.GetRequiredService<ScheduleManagementService>().AddAvailabilityAsync(s.Facility, second.Branch, s.Doctor, s.Date, new(12, 0), new(13, 0), Token));
        Assert.Equal(2, (await s.PageAsync()).Availability.Count);
    }

    [Theory]
    [InlineData(8, 30, 10, 0)]
    [InlineData(11, 30, 13, 30)]
    [InlineData(16, 30, 17, 30)]
    public async Task AvailabilityMustFitTheEntireOpeningUnionIncludingLunchGap(int sh, int sm, int eh, int em)
    {
        using var s = await ManagementScenario.CreateAsync(shift: false);
        await s.Run(p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(s.Facility, s.Branch, s.Date.DayOfWeek,
            [new(new(9, 0), new(12, 0)), new(new(13, 0), new(17, 0))], Token));
        await Assert.ThrowsAsync<ConfigurationException>(() => s.Run(p => p.GetRequiredService<ScheduleManagementService>().AddAvailabilityAsync(s.Facility, s.Branch, s.Doctor, s.Date, new(sh, sm), new(eh, em), Token)));
        Assert.Empty((await s.PageAsync()).Availability);
        await s.Run(p => p.GetRequiredService<ScheduleManagementService>().AddAvailabilityAsync(s.Facility, s.Branch, s.Doctor, s.Date, new(9, 0), new(12, 0), Token));
    }

    [Theory]
    [InlineData(9, 0, 9, 0)]
    [InlineData(11, 0, 10, 0)]
    [InlineData(23, 0, 1, 0)]
    public async Task NonPositiveOrOvernightIntervalsAreRejected(int sh, int sm, int eh, int em)
    {
        using var s = await ManagementScenario.CreateAsync(shift: false);
        await Assert.ThrowsAsync<ConfigurationException>(() => s.Run(p => p.GetRequiredService<ScheduleManagementService>().AddAvailabilityAsync(s.Facility, s.Branch, s.Doctor, s.Date, new(sh, sm), new(eh, em), Token)));
    }

    [Fact]
    public async Task PastAvailabilityAndMissingOrInactiveAssignmentsAreRejected()
    {
        using var s = await ManagementScenario.CreateAsync(shift: false);
        await Assert.ThrowsAsync<ConfigurationException>(() => s.Run(p => p.GetRequiredService<ScheduleManagementService>().AddAvailabilityAsync(s.Facility, s.Branch, s.Doctor, s.Date.AddDays(-3), new(9, 0), new(10, 0), Token)));
        var branch = await s.Run(p => p.GetRequiredService<FacilityManagementService>().AddBranchAsync(s.Facility, "No assignment", Token));
        await Assert.ThrowsAsync<ConfigurationException>(() => s.Run(p => p.GetRequiredService<ScheduleManagementService>().AddAvailabilityAsync(s.Facility, branch, s.Doctor, s.Date, new(9, 0), new(10, 0), Token)));
        await s.Run(p => p.GetRequiredService<StaffManagementService>().ChangeOfferingActiveAsync(s.Facility, s.Offering, false, Token));
        await Assert.ThrowsAsync<ConfigurationException>(() => s.Run(p => p.GetRequiredService<ScheduleManagementService>().AddAvailabilityAsync(s.Facility, s.Branch, s.Doctor, s.Date, new(9, 0), new(10, 0), Token)));
    }

    [Fact]
    public async Task HoursProtectFutureUnbookedShiftsAndValidatePeriodsBeforeReplacement()
    {
        using var s = await ManagementScenario.CreateAsync();
        await Assert.ThrowsAsync<ConfigurationException>(() => s.Run(p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(s.Facility, s.Branch, s.Date.DayOfWeek, [new(new(10, 0), new(17, 0))], Token)));
        await Assert.ThrowsAsync<ConfigurationException>(() => s.Run(p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(s.Facility, s.Branch, s.Date.DayOfWeek, [new(new(9, 0), null)], Token)));
        await Assert.ThrowsAsync<ConfigurationException>(() => s.Run(p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(s.Facility, s.Branch, s.Date.DayOfWeek, [new(new(9, 0), new(12, 0)), new(new(11, 0), new(17, 0))], Token)));
        var original = Assert.Single((await s.PageAsync()).Opening); Assert.Equal(new TimeOnly(9, 0), original.Opens); Assert.Equal(new TimeOnly(17, 0), original.Closes);
        await s.Run(p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(s.Facility, s.Branch, s.Date.DayOfWeek, [new(new(9, 0), new(10, 0)), new(new(10, 0), new(17, 0))], Token));
        Assert.Equal(2, (await s.PageAsync()).Opening.Count);
    }

    [Fact]
    public async Task ClosingUnreservedDayPreservesOtherWeekdays()
    {
        using var s = await ManagementScenario.CreateAsync();
        await s.Run(p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(s.Facility, s.Branch, DayOfWeek.Sunday, [new(new(9, 0), new(12, 0))], Token));
        await s.Run(p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(s.Facility, s.Branch, DayOfWeek.Sunday, [new(null, null)], Token));
        Assert.Equal(s.Date.DayOfWeek, Assert.Single((await s.PageAsync()).Opening).Day);
    }
}
