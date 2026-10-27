using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Domain.Staff;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointments.Infrastructure.Repositories;

public sealed class StaffManagementRepository(ApplicationDbContext db) : IStaffManagementRepository
{
    public Task<StaffMember?> StaffAsync(long id, CancellationToken token) => db.Set<StaffMember>().Include(s => s.Facility)
        .Include(s => ((Doctor)s).Specialty).SingleOrDefaultAsync(s => s.Id == id, token);
    public Task<Specialty?> SpecialtyAsync(long id, CancellationToken token) => db.Set<Specialty>().SingleOrDefaultAsync(s => s.Id == id, token);
    public Task<Specialty?> SpecialtyNamedAsync(string name, CancellationToken token) => db.Set<Specialty>().SingleOrDefaultAsync(s => s.Name == name, token);
    private IQueryable<StaffOffering> Offerings() => db.Set<StaffOffering>()
        .Include(o => o.Staff).ThenInclude(s => s.Facility)
        .Include(o => o.Service).ThenInclude(s => s.Department).ThenInclude(d => d.Branch).ThenInclude(b => b.Facility);
    public Task<StaffOffering?> OfferingAsync(long id, CancellationToken token) => Offerings().SingleOrDefaultAsync(o => o.Id == id, token);
    public Task<StaffOffering?> OfferingForAsync(long staffId, long serviceId, CancellationToken token) => Offerings().SingleOrDefaultAsync(o => o.StaffId == staffId && o.ServiceId == serviceId, token);
    public Task<bool> HasActiveOfferingAsync(long staffId, long branchId, CancellationToken token) =>
        db.Set<StaffOffering>().AnyAsync(o => o.StaffId == staffId && o.Service.Department.BranchId == branchId && o.IsActive && o.Service.IsActive, token);
    public void AddStaff(StaffMember staff) => db.Add(staff);
    public void AddSpecialty(Specialty specialty) => db.Add(specialty);
    public void AddOffering(StaffOffering offering) => db.Add(offering);
}
