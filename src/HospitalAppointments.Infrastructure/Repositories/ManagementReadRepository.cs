using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Domain.Catalog;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Staff;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointments.Infrastructure.Repositories;

public sealed class ManagementReadRepository(ApplicationDbContext db) : IManagementReadRepository
{
    public async Task<ManagementData?> LoadAsync(long facilityId, CancellationToken token)
    {
        var facility = await db.Set<Facility>().AsNoTracking().SingleOrDefaultAsync(f => f.Id == facilityId, token);
        if (facility is null) return null;
        var branches = await db.Set<Branch>().AsNoTracking().Where(b => b.FacilityId == facilityId).OrderBy(b => b.Id).ToListAsync(token);
        var departments = await db.Set<Department>().AsNoTracking().Include(d => d.Branch).Where(d => d.Branch.FacilityId == facilityId).OrderBy(d => d.Id).ToListAsync(token);
        var services = await db.Set<HealthcareService>().AsNoTracking().Include(s => s.Department).ThenInclude(d => d.Branch)
            .Where(s => s.Department.Branch.FacilityId == facilityId).OrderBy(s => s.Id).ToListAsync(token);
        var staff = await db.Set<StaffMember>().AsNoTracking().Include(s => ((Doctor)s).Specialty)
            .Where(s => s.FacilityId == facilityId).OrderBy(s => s.Id).ToListAsync(token);
        var specialties = await db.Set<Specialty>().AsNoTracking().OrderBy(s => s.Name).ToListAsync(token);
        var offerings = await db.Set<StaffOffering>().AsNoTracking().Include(o => o.Staff)
            .Include(o => o.Service).ThenInclude(s => s.Department).ThenInclude(d => d.Branch)
            .Where(o => o.Staff.FacilityId == facilityId).OrderBy(o => o.Id).ToListAsync(token);
        var opening = await db.Set<OpeningWindow>().AsNoTracking().Where(w => w.Branch.FacilityId == facilityId).OrderBy(w => w.BranchId).ThenBy(w => w.Weekday).ThenBy(w => w.OpensAt).ToListAsync(token);
        var availability = await db.Set<AvailabilityWindow>().AsNoTracking().Include(w => w.Branch).Include(w => w.Staff)
            .Where(w => w.Branch.FacilityId == facilityId).OrderBy(w => w.Start).ToListAsync(token);
        return new(facility, branches, departments, services, staff, specialties, offerings, opening, availability);
    }
}
