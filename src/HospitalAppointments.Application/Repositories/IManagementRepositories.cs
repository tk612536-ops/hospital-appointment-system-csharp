using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Domain.Catalog;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Scheduling;
using HospitalAppointments.Domain.Staff;

namespace HospitalAppointments.Application.Repositories;

public interface IFacilityManagementRepository
{
    Task<IReadOnlyList<Facility>> ManagedAsync(long accountId, CancellationToken token);
    Task<Facility?> FacilityAsync(long id, CancellationToken token);
    Task<Branch?> BranchAsync(long id, CancellationToken token);
    Task<Department?> DepartmentAsync(long id, CancellationToken token);
    void AddFacility(Facility facility, Branch firstBranch, ManagerFacilityGrant grant);
    void AddBranch(Branch branch);
    void AddDepartment(Department department);
}

public interface ICatalogManagementRepository
{
    Task<HealthcareService?> FindAsync(long id, CancellationToken token);
    void Add(HealthcareService service);
}

public interface IStaffManagementRepository
{
    Task<StaffMember?> StaffAsync(long id, CancellationToken token);
    Task<Specialty?> SpecialtyAsync(long id, CancellationToken token);
    Task<Specialty?> SpecialtyNamedAsync(string name, CancellationToken token);
    Task<StaffOffering?> OfferingAsync(long id, CancellationToken token);
    Task<StaffOffering?> OfferingForAsync(long staffId, long serviceId, CancellationToken token);
    Task<bool> HasActiveOfferingAsync(long staffId, long branchId, CancellationToken token);
    void AddStaff(StaffMember staff);
    void AddSpecialty(Specialty specialty);
    void AddOffering(StaffOffering offering);
}

public interface IScheduleManagementRepository
{
    Task<AvailabilityWindow?> WindowAsync(long id, CancellationToken token);
    Task<IReadOnlyList<AvailabilityWindow>> FutureShiftsAsync(long branchId, DateTime now, CancellationToken token);
    Task<IReadOnlyList<TimeInterval>> StaffWindowsAsync(long staffId, long branchId, long excludedId, CancellationToken token);
    Task<bool> StaffOverlapsAsync(long staffId, long excludedId, DateTime start, DateTime end, CancellationToken token);
    Task<IReadOnlyList<Appointment>> ReservationsAsync(long branchId, DateTime now, CancellationToken token);
    Task ReplaceHoursAsync(long branchId, DayOfWeek day, IReadOnlyList<OpeningWindow> replacements, CancellationToken token);
    void AddWindow(AvailabilityWindow window);
    void RemoveWindow(AvailabilityWindow window);
}

public sealed record ManagementData(Facility Facility, IReadOnlyList<Branch> Branches, IReadOnlyList<Department> Departments,
    IReadOnlyList<HealthcareService> Services, IReadOnlyList<StaffMember> Staff, IReadOnlyList<Specialty> Specialties,
    IReadOnlyList<StaffOffering> Offerings, IReadOnlyList<OpeningWindow> Opening, IReadOnlyList<AvailabilityWindow> Availability);

public interface IManagementReadRepository
{
    Task<ManagementData?> LoadAsync(long facilityId, CancellationToken token);
}
