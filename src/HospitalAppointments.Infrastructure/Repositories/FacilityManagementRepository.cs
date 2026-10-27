using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointments.Infrastructure.Repositories;

public sealed class FacilityManagementRepository(ApplicationDbContext db) : IFacilityManagementRepository
{
    public async Task<IReadOnlyList<Facility>> ManagedAsync(long accountId, CancellationToken token) =>
        await db.Set<Facility>().AsNoTracking().Where(f => db.Set<ManagerFacilityGrant>().Any(g => g.AccountId == accountId && g.FacilityId == f.Id)).OrderBy(f => f.Name).ToListAsync(token);
    public Task<Facility?> FacilityAsync(long id, CancellationToken token) => db.Set<Facility>().SingleOrDefaultAsync(f => f.Id == id, token);
    public Task<Branch?> BranchAsync(long id, CancellationToken token) => db.Set<Branch>().Include(b => b.Facility).SingleOrDefaultAsync(b => b.Id == id, token);
    public Task<Department?> DepartmentAsync(long id, CancellationToken token) => db.Set<Department>().Include(d => d.Branch).ThenInclude(b => b.Facility).SingleOrDefaultAsync(d => d.Id == id, token);
    public void AddFacility(Facility facility, Branch firstBranch, ManagerFacilityGrant grant) => db.AddRange(facility, firstBranch, grant);
    public void AddBranch(Branch branch) => db.Add(branch);
    public void AddDepartment(Department department) => db.Add(department);
}
