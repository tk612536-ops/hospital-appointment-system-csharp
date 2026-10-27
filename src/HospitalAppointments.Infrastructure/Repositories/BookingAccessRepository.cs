using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Patients;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointments.Infrastructure.Repositories;

public sealed class BookingAccessRepository(ApplicationDbContext database) : IBookingAccessRepository
{
    public Task<Patient?> PatientAsync(long accountId, CancellationToken token) =>
        database.Set<Patient>().AsNoTracking().SingleOrDefaultAsync(p => p.AccountId == accountId, token);
    public Task<bool> ManagesAsync(long accountId, long facilityId, CancellationToken token) =>
        database.Set<ManagerFacilityGrant>().AnyAsync(g => g.AccountId == accountId && g.FacilityId == facilityId, token);
}
