using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Domain.Scheduling;

namespace HospitalAppointments.Application.Tests;

public sealed class AvailabilityRulesTests
{
    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2030, 1, 7, 6, 0, 0, TimeSpan.Zero);
    }
    private readonly ClinicTime time = new(new FixedClock(), TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"));
    private readonly DateOnly date = new(2030, 1, 8);

    private AvailabilityService.DailySchedule Schedule(bool active = true, IReadOnlyList<TimeInterval>? reservations = null) => new(
        active, 30, date, [time.Interval(date, new(9, 0), new(12, 0))],
        [time.Interval(date, new(9, 0), new(11, 0))], reservations ?? [], time);

    [Fact]
    public void CandidateAndSubmissionUseTheSameEntireDurationRule()
    {
        var schedule = Schedule();
        Assert.Contains(new TimeOnly(10, 30), schedule.Starts());
        Assert.DoesNotContain(new TimeOnly(10, 45), schedule.Starts());
        Assert.Equal(time.ToUtc(date, new(11, 0)), schedule.Require(new(10, 30)).End);
        Assert.Throws<BookingException>(() => schedule.Require(new(10, 45)));
    }

    [Fact]
    public void PatientOrEmployeeReservationEliminatesEveryOverlappingCandidate()
    {
        var schedule = Schedule(reservations: [time.Interval(date, new(10, 0), new(10, 45))]);
        Assert.DoesNotContain(new TimeOnly(9, 45), schedule.Starts()); Assert.DoesNotContain(new TimeOnly(10, 30), schedule.Starts());
        Assert.Contains(new TimeOnly(9, 30), schedule.Starts());
    }

    [Fact]
    public void CurrentInstantAndPastStartsAreRejected()
    {
        var today = time.Today;
        var schedule = new AvailabilityService.DailySchedule(true, 30, today,
            [time.Interval(today, new(8, 0), new(12, 0))], [time.Interval(today, new(8, 0), new(12, 0))], [], time);
        Assert.Throws<BookingException>(() => schedule.Require(new(9, 0)));
        Assert.Throws<BookingException>(() => schedule.Require(new(8, 45)));
        Assert.Equal(6, schedule.Require(new(9, 15)).Start.Hour);
    }

    [Theory]
    [InlineData(10, 1, 0)]
    [InlineData(10, 0, 1)]
    public void OffGridAndSubMinuteStartsAreRejected(int hour, int minute, int second) =>
        Assert.Throws<BookingException>(() => Schedule().Require(new(hour, minute, second)));

    [Fact]
    public void InactiveOfferingAndOvernightIntervalsAreRejected()
    {
        Assert.Empty(Schedule(false).Starts());
        var late = new AvailabilityService.DailySchedule(true, 30, date,
            [time.Interval(date, new(0, 0), new(23, 59))], [time.Interval(date, new(0, 0), new(23, 59))], [], time);
        Assert.Throws<BookingException>(() => late.Require(new(23, 45)));
    }

    [Fact]
    public void ClinicLocalDatesAndUtcConversionUseConfiguredZone()
    {
        Assert.Equal(new DateOnly(2030, 1, 7), time.Today);
        var utc = time.ToUtc(date, new(10, 30));
        Assert.Equal(DateTimeKind.Utc, utc.Kind); Assert.Equal(7, utc.Hour);
        Assert.Equal(new TimeOnly(10, 30), TimeOnly.FromDateTime(time.Local(utc)));
    }
}
