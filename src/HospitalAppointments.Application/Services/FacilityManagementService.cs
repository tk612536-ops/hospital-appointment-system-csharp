using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Domain.Facilities;

namespace HospitalAppointments.Application.Services;

public sealed class FacilityManagementService(ManagementScope scope, IFacilityManagementRepository facilities)
{
    public async Task<long> CreateAsync(string name, FacilityType type, string firstBranchName, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(null, token);
        var actor = await scope.RequireManagerAsync(token);
        var facility = new Facility(name, type);
        var branch = new Branch(facility, firstBranchName);
        facilities.AddFacility(facility, branch, new ManagerFacilityGrant(actor.Id, facility));
        await transaction.SaveAsync(token); await transaction.CommitAsync(token);
        return facility.Id;
    }

    public async Task UpdateAsync(long id, string name, FacilityType type, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(id, token);
        var facility = await facilities.FacilityAsync(id, token) ?? throw new BookingNotFoundException();
        facility.UpdateDetails(name, type);
        await transaction.SaveAsync(token); await transaction.CommitAsync(token);
    }

    public async Task<long> AddBranchAsync(long facilityId, string name, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var facility = await facilities.FacilityAsync(facilityId, token) ?? throw new BookingNotFoundException();
        var branch = new Branch(facility, name); facilities.AddBranch(branch);
        await transaction.SaveAsync(token); await transaction.CommitAsync(token); return branch.Id;
    }

    public async Task RenameBranchAsync(long facilityId, long branchId, string name, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var branch = await facilities.BranchAsync(branchId, token) ?? throw new BookingNotFoundException();
        ManagementScope.RequireOwner(facilityId, branch.FacilityId); branch.Rename(name);
        await transaction.SaveAsync(token); await transaction.CommitAsync(token);
    }

    public async Task<long> AddDepartmentAsync(long facilityId, long branchId, string name, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var branch = await facilities.BranchAsync(branchId, token) ?? throw new BookingNotFoundException();
        ManagementScope.RequireOwner(facilityId, branch.FacilityId);
        var department = new Department(branch, name); facilities.AddDepartment(department);
        await transaction.SaveAsync(token); await transaction.CommitAsync(token); return department.Id;
    }

    public async Task RenameDepartmentAsync(long facilityId, long departmentId, string name, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var department = await facilities.DepartmentAsync(departmentId, token) ?? throw new BookingNotFoundException();
        ManagementScope.RequireOwner(facilityId, department.Branch.FacilityId); department.Rename(name);
        await transaction.SaveAsync(token); await transaction.CommitAsync(token);
    }
}
