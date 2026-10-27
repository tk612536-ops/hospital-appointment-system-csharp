using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Domain.Patients;
using HospitalAppointments.Domain.Scheduling;
using HospitalAppointments.Domain.Staff;

namespace HospitalAppointments.Application.Services;

public sealed class AvailabilityService(IScheduleRepository schedules, IAppointmentRepository appointments, ClinicTime time)
{
    public async Task<DailySchedule> LoadAsync(StaffOffering offering, Patient patient, DateOnly date, CancellationToken token)
    {
        if (date == DateOnly.MaxValue) throw new BookingException("Bu tarih desteklenen takvim aralığının dışında. Başka bir tarih seçin.");
        var dayStart = time.ToUtc(date, TimeOnly.MinValue);
        var dayEnd = time.ToUtc(date.AddDays(1), TimeOnly.MinValue);
        var opening = await schedules.OpeningAsync(offering.Branch.Id, date.DayOfWeek, token);
        var available = await schedules.AvailabilityAsync(offering.StaffId, offering.Branch.Id, dayStart, dayEnd, token);
        var reserved = await appointments.ConflictsAsync(offering.StaffId, patient.Id, dayStart, dayEnd, token);
        return new DailySchedule(offering.IsBookable, offering.Service.DurationMinutes, date,
            opening.Select(w => time.Interval(date, w.OpensAt, w.ClosesAt)).ToArray(), available, reserved, time);
    }

    public sealed class DailySchedule(bool bookable, int duration, DateOnly date,
        IReadOnlyList<TimeInterval> opening, IReadOnlyList<TimeInterval> availability,
        IReadOnlyList<TimeInterval> reserved, ClinicTime time)
    {
        public TimeInterval Require(TimeOnly start)
        {
            if (!bookable) throw new BookingException("Bu hizmet veya çalışan artık randevuya açık değil.");
            if (start.Minute % 15 != 0 || start.Ticks % TimeSpan.TicksPerMinute != 0)
                throw new BookingException("Listelenen 15 dakikalık başlangıç saatlerinden birini seçin.");
            var utc = time.ToUtc(date, start);
            if (utc <= time.UtcNow) throw new BookingException("Gelecekte bir randevu saati seçin.");
            var interval = new TimeInterval(utc, utc.AddMinutes(duration));
            if (DateOnly.FromDateTime(time.Local(interval.End)) != date || !interval.IsCoveredBy(opening))
                throw new BookingException("Hizmetin tamamı şube çalışma saatlerine sığmalıdır.");
            if (!interval.IsCoveredBy(availability))
                throw new BookingException("Hizmetin tamamı çalışanın uygunluk aralığına sığmıyor veya bir ara ile çakışıyor.");
            if (reserved.Any(interval.Overlaps)) throw new BookingException("Çalışanın veya hastanın bu saatlerle çakışan bir randevusu var.");
            return interval;
        }

        public IReadOnlyList<TimeOnly> Starts()
        {
            var starts = new List<TimeOnly>();
            for (var minute = 0; minute < 1440; minute += 15)
            {
                var start = TimeOnly.MinValue.AddMinutes(minute);
                try { Require(start); starts.Add(start); }
                catch (BookingException) { }
            }
            return starts;
        }
    }
}
