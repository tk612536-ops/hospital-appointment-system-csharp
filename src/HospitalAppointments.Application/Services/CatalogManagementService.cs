using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Domain.Catalog;

namespace HospitalAppointments.Application.Services;

public sealed class CatalogManagementService(ManagementScope scope, IFacilityManagementRepository facilities, ICatalogManagementRepository catalog)
{
    public async Task<long> CreateAsync(long facilityId, long departmentId, string name, int duration, decimal fee, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var department = await facilities.DepartmentAsync(departmentId, token) ?? throw new BookingNotFoundException();
        ManagementScope.RequireOwner(facilityId, department.Branch.FacilityId);
        var service = new HealthcareService(department, name, duration, fee); catalog.Add(service);
        await transaction.SaveAsync(token); await transaction.CommitAsync(token); return service.Id;
    }

    public async Task UpdateAsync(long facilityId, long serviceId, string name, int duration, decimal fee, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var service = await catalog.FindAsync(serviceId, token) ?? throw new BookingNotFoundException();
        ManagementScope.RequireOwner(facilityId, service.Department.Branch.FacilityId);
        service.UpdateDetails(name, duration, fee);
        await transaction.SaveAsync(token); await transaction.CommitAsync(token);
    }

    public async Task ChangeActiveAsync(long facilityId, long serviceId, bool active, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var service = await catalog.FindAsync(serviceId, token) ?? throw new BookingNotFoundException();
        ManagementScope.RequireOwner(facilityId, service.Department.Branch.FacilityId);
        if (active) service.Activate(); else service.Deactivate();
        await transaction.SaveAsync(token); await transaction.CommitAsync(token);
    }
}
