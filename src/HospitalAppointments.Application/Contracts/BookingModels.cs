using HospitalAppointments.Domain.Appointments;

namespace HospitalAppointments.Application.Contracts;

public sealed record BookAppointmentCommand(long OfferingId, DateOnly Date, TimeOnly StartTime, long CatalogVersion);
public sealed record Choice(long Id, string Label);
public sealed record BookingPage(IReadOnlyList<Choice> Facilities, IReadOnlyList<Choice> Branches,
    IReadOnlyList<Choice> Services, IReadOnlyList<Choice> Staff, long? FacilityId, long? BranchId,
    long? ServiceId, long? OfferingId, DateOnly Date, int DurationMinutes, decimal Fee,
    string Currency, long CatalogVersion, IReadOnlyList<TimeOnly> Starts);
public sealed record AppointmentView(long Id, string PatientName, string ServiceName, string StaffName,
    string ProfessionalDescription, string Facility, string Branch, DateTime Start, DateTime End,
    int DurationMinutes, decimal Fee, string Currency, AppointmentStatus Status, long? DecidedByAccountId,
    DateTime? DecidedAt, bool CanDecide);

public sealed class BookingException(string message) : Exception(message);
public sealed class BookingNotFoundException() : Exception("Record not found.");
