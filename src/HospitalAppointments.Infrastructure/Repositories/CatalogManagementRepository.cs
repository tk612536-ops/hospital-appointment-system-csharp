using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Domain.Catalog;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointments.Infrastructure.Repositories;

public sealed class CatalogManagementRepository(ApplicationDbContext db) : ICatalogManagementRepository
{
    public Task<HealthcareService?> FindAsync(long id, CancellationToken token) => db.Set<HealthcareService>()
        .Include(s => s.Department).ThenInclude(d => d.Branch).ThenInclude(b => b.Facility).SingleOrDefaultAsync(s => s.Id == id, token);
    public void Add(HealthcareService service) => db.Add(service);
}
