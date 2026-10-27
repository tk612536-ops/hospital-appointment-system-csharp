using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Domain.Staff;

namespace HospitalAppointments.Application.Services;

public sealed class ManagementQueryService(ManagementScope scope, IFacilityManagementRepository facilities, IManagementReadRepository reads, ClinicTime time)
{
    public async Task<IReadOnlyList<FacilityRow>> FacilitiesAsync(CancellationToken token = default)
    {
        var actor = await scope.RequireManagerAsync(token);
        return (await facilities.ManagedAsync(actor.Id, token)).Select(f => new FacilityRow(f.Id, f.Name, f.Type)).ToArray();
    }

    public async Task<ManagementPage> PageAsync(long facilityId, CancellationToken token = default)
    {
        await scope.RequireFacilityAsync(facilityId, token);
        var data = await reads.LoadAsync(facilityId, token) ?? throw new BookingNotFoundException();
        return new(new(data.Facility.Id, data.Facility.Name, data.Facility.Type),
            data.Branches.Select(b => new BranchRow(b.Id, b.Name)).ToArray(),
            data.Departments.Select(d => new DepartmentRow(d.Id, d.BranchId, d.Branch.Name, d.Name)).ToArray(),
            data.Services.Select(s => new ServiceRow(s.Id, s.DepartmentId, s.Department.Name, s.Department.Branch.Name, s.Name, s.DurationMinutes, s.Fee, s.IsActive)).ToArray(),
            data.Staff.Select(s => new StaffRow(s.Id, s.Name, s.ProfessionalDescription, s is Doctor,
                (s as Doctor)?.SpecialtyId, (s as HealthcareStaff)?.JobTitle, s.IsActive, s.AccountId is not null,
                string.Join(", ", data.Offerings.Where(o => o.StaffId == s.Id && o.IsActive).Select(o => o.Branch.Name).Distinct()))).ToArray(),
            data.Specialties.Select(s => new Choice(s.Id, s.Name)).ToArray(),
            data.Offerings.Select(o => new OfferingRow(o.Id, o.Staff.Name, o.Service.Name, o.Branch.Name, o.IsActive)).ToArray(),
            data.Opening.Select(w => new OpeningRow(w.BranchId, w.Weekday, w.OpensAt, w.ClosesAt)).ToArray(),
            data.Availability.Select(w => new AvailabilityRow(w.Id, w.StaffId, w.BranchId, w.Staff.Name, w.Branch.Name,
                DateOnly.FromDateTime(time.Local(w.Start)), TimeOnly.FromDateTime(time.Local(w.Start)), TimeOnly.FromDateTime(time.Local(w.End)))).ToArray(), time.Today.AddDays(1));
    }
}
