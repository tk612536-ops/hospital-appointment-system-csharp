using HospitalAppointments.Domain.Scheduling;

namespace HospitalAppointments.Application.Services;

public sealed class ClinicTime(TimeProvider clock, TimeZoneInfo zone)
{
    public DateTime UtcNow => clock.GetUtcNow().UtcDateTime;
    public DateTime Local(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
    public DateOnly Today => DateOnly.FromDateTime(Local(UtcNow));
    public DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(local) || zone.IsAmbiguousTime(local)) throw new ArgumentException("Use an unambiguous clinic time.");
        return TimeZoneInfo.ConvertTimeToUtc(local, zone);
    }
    public TimeInterval Interval(DateOnly date, TimeOnly start, TimeOnly end) => new(ToUtc(date, start), ToUtc(date, end));
}
