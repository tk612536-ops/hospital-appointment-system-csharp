using HospitalAppointments.Domain.Patients;
using HospitalAppointments.Domain.Scheduling;
using HospitalAppointments.Domain.Staff;

namespace HospitalAppointments.Domain.Appointments;

public enum AppointmentStatus { Pending, Approved, Rejected }

public sealed class Appointment
{
    public long Id { get; private set; }
    public long PatientId { get; private set; }
    public Patient Patient { get; private set; } = null!;
    public long OfferingId { get; private set; }
    public StaffOffering Offering { get; private set; } = null!;
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }
    public int BookedDurationMinutes { get; private set; }
    public decimal BookedFee { get; private set; }
    public string BookedCurrency { get; private set; } = "";
    public string BookedServiceName { get; private set; } = "";
    public string BookedStaffName { get; private set; } = "";
    public AppointmentStatus Status { get; private set; } = AppointmentStatus.Pending;
    public DateTime CreatedAt { get; private set; }
    public long? DecidedByAccountId { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public long Version { get; private set; } = 1;
    public TimeInterval Interval => new(Start, End);
    private Appointment() { }
    public Appointment(Patient patient, StaffOffering offering, TimeInterval interval, DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(patient);
        ArgumentNullException.ThrowIfNull(offering);
        if (interval.End != interval.Start.AddMinutes(offering.Service.DurationMinutes)) throw new ArgumentException("Interval must use server duration.");
        DomainRules.Utc(createdAt);
        Patient = patient;
        Offering = offering;
        Start = interval.Start;
        End = interval.End;
        BookedDurationMinutes = offering.Service.DurationMinutes;
        BookedFee = offering.Service.Fee;
        BookedCurrency = offering.Service.Currency;
        BookedServiceName = offering.Service.Name;
        BookedStaffName = offering.Staff.Name;
        CreatedAt = createdAt;
    }
    public void Approve(long actorId, DateTime time) => Decide(AppointmentStatus.Approved, actorId, time);
    public void Reject(long actorId, DateTime time) => Decide(AppointmentStatus.Rejected, actorId, time);
    private void Decide(AppointmentStatus result, long actorId, DateTime time)
    {
        if (Status != AppointmentStatus.Pending) throw new InvalidOperationException("Appointment has already been decided.");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(actorId);
        DomainRules.Utc(time);
        Status = result;
        DecidedByAccountId = actorId;
        DecidedAt = time;
        Version++;
    }
}
