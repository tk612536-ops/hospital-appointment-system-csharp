using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Patients;
using HospitalAppointments.Domain.Scheduling;
using HospitalAppointments.Domain.Staff;

namespace HospitalAppointments.Application.Repositories;

public interface IBookingCatalogRepository
{
    Task<IReadOnlyList<StaffOffering>> ListAsync(CancellationToken token);
    Task<StaffOffering?> FindAsync(long id, CancellationToken token);
}

public interface IScheduleRepository
{
    Task<IReadOnlyList<OpeningWindow>> OpeningAsync(long branchId, DayOfWeek weekday, CancellationToken token);
    Task<IReadOnlyList<TimeInterval>> AvailabilityAsync(long staffId, long branchId, DateTime start, DateTime end, CancellationToken token);
}

public interface IBookingAccessRepository
{
    Task<Patient?> PatientAsync(long accountId, CancellationToken token);
    Task<bool> ManagesAsync(long accountId, long facilityId, CancellationToken token);
}

public interface IAppointmentRepository
{
    Task<IReadOnlyList<TimeInterval>> ConflictsAsync(long staffId, long patientId, DateTime start, DateTime end, CancellationToken token);
    Task<Appointment?> FindAsync(long id, bool tracking, CancellationToken token);
    Task<IReadOnlyList<Appointment>> ForAccountAsync(long accountId, bool patient, bool staff, bool manager, CancellationToken token);
    void Add(Appointment appointment);
}
