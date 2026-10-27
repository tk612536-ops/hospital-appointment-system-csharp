using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Scheduling;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointments.Infrastructure.Repositories;

public sealed class AppointmentRepository(ApplicationDbContext database) : IAppointmentRepository
{
    private IQueryable<Appointment> Graph() => database.Set<Appointment>().Include(a => a.Patient)
        .Include(a => a.Offering).ThenInclude(o => o.Service).ThenInclude(s => s.Department).ThenInclude(d => d.Branch).ThenInclude(b => b.Facility)
        .Include(a => a.Offering).ThenInclude(o => o.Staff).ThenInclude(s => ((Domain.Staff.Doctor)s).Specialty);
    public async Task<IReadOnlyList<TimeInterval>> ConflictsAsync(long staffId, long patientId, DateTime start, DateTime end, CancellationToken token) =>
        await database.Set<Appointment>().AsNoTracking()
            .Where(a => (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved) && a.Start < end && start < a.End
                && (a.Offering.StaffId == staffId || a.PatientId == patientId))
            .Select(a => new TimeInterval(a.Start, a.End)).ToListAsync(token);
    public Task<Appointment?> FindAsync(long id, bool tracking, CancellationToken token) =>
        (tracking ? Graph() : Graph().AsNoTrackingWithIdentityResolution()).SingleOrDefaultAsync(a => a.Id == id, token);
    public async Task<IReadOnlyList<Appointment>> ForAccountAsync(long accountId, bool patient, bool staff, bool manager, CancellationToken token) =>
        await Graph().AsNoTrackingWithIdentityResolution().Where(a => patient && a.Patient.AccountId == accountId
            || staff && a.Offering.Staff.AccountId == accountId
            || manager && database.Set<ManagerFacilityGrant>().Any(g => g.AccountId == accountId && g.FacilityId == a.Offering.Service.Department.Branch.FacilityId))
            .OrderByDescending(a => a.Start).ToListAsync(token);
    public void Add(Appointment appointment)
    {
        database.Attach(appointment.Patient);
        database.Set<Appointment>().Add(appointment);
    }
}
