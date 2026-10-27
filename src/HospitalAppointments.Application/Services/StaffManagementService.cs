using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Domain.Staff;

namespace HospitalAppointments.Application.Services;

public sealed class StaffManagementService(ManagementScope scope, IFacilityManagementRepository facilities,
    IStaffManagementRepository staff, ICatalogManagementRepository catalog, IStaffAccountProvisioner accounts)
{
    public async Task<long> AddSpecialtyAsync(long facilityId, string name, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var proposed = new Specialty(name);
        var specialty = await staff.SpecialtyNamedAsync(proposed.Name, token);
        if (specialty is null) { specialty = proposed; staff.AddSpecialty(specialty); }
        await transaction.SaveAsync(token); await transaction.CommitAsync(token); return specialty.Id;
    }

    public async Task<long> RegisterDoctorAsync(long facilityId, string name, long specialtyId, string? username, string? password, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var facility = await facilities.FacilityAsync(facilityId, token) ?? throw new BookingNotFoundException();
        var specialty = await staff.SpecialtyAsync(specialtyId, token) ?? throw new BookingNotFoundException();
        var accountId = await OptionalAccountAsync(username, password, name, token);
        var doctor = new Doctor(facility, accountId, name, specialty); staff.AddStaff(doctor);
        await transaction.SaveAsync(token); await transaction.CommitAsync(token); return doctor.Id;
    }

    public async Task<long> RegisterHealthcareStaffAsync(long facilityId, string name, string title, string? username, string? password, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var facility = await facilities.FacilityAsync(facilityId, token) ?? throw new BookingNotFoundException();
        var accountId = await OptionalAccountAsync(username, password, name, token);
        var employee = new HealthcareStaff(facility, accountId, name, title); staff.AddStaff(employee);
        await transaction.SaveAsync(token); await transaction.CommitAsync(token); return employee.Id;
    }

    public async Task UpdateDoctorAsync(long facilityId, long staffId, string name, long specialtyId, bool active, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var employee = await staff.StaffAsync(staffId, token) ?? throw new BookingNotFoundException();
        ManagementScope.RequireOwner(facilityId, employee.FacilityId);
        if (employee is not Doctor doctor) throw new BookingNotFoundException();
        var specialty = await staff.SpecialtyAsync(specialtyId, token) ?? throw new BookingNotFoundException();
        doctor.Rename(name); doctor.AssignSpecialty(specialty);
        if (active) doctor.Activate(); else doctor.Deactivate();
        await transaction.SaveAsync(token); await transaction.CommitAsync(token);
    }

    public async Task UpdateHealthcareStaffAsync(long facilityId, long staffId, string name, string title, bool active, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var employee = await staff.StaffAsync(staffId, token) ?? throw new BookingNotFoundException();
        ManagementScope.RequireOwner(facilityId, employee.FacilityId);
        if (employee is not HealthcareStaff worker) throw new BookingNotFoundException();
        worker.Rename(name); worker.ChangeJobTitle(title);
        if (active) worker.Activate(); else worker.Deactivate();
        await transaction.SaveAsync(token); await transaction.CommitAsync(token);
    }

    public async Task<long> AssignServiceAsync(long facilityId, long staffId, long serviceId, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var employee = await staff.StaffAsync(staffId, token) ?? throw new BookingNotFoundException();
        var service = await catalog.FindAsync(serviceId, token) ?? throw new BookingNotFoundException();
        ManagementScope.RequireOwner(facilityId, employee.FacilityId);
        ManagementScope.RequireOwner(facilityId, service.Department.Branch.FacilityId);
        if (!employee.IsActive || !service.IsActive) throw new ConfigurationException("Hizmet atamadan önce çalışanı ve hizmeti etkinleştirin.");
        var offering = await staff.OfferingForAsync(staffId, serviceId, token);
        if (offering is null) { offering = new StaffOffering(employee, service); staff.AddOffering(offering); }
        else offering.Activate();
        await transaction.SaveAsync(token); await transaction.CommitAsync(token); return offering.Id;
    }

    public async Task ChangeOfferingActiveAsync(long facilityId, long offeringId, bool active, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var offering = await staff.OfferingAsync(offeringId, token) ?? throw new BookingNotFoundException();
        ManagementScope.RequireOwner(facilityId, offering.Staff.FacilityId);
        ManagementScope.RequireOwner(facilityId, offering.Branch.FacilityId);
        if (active)
        {
            if (!offering.Staff.IsActive || !offering.Service.IsActive) throw new ConfigurationException("Önce çalışanı ve hizmeti etkinleştirin.");
            offering.Activate();
        }
        else offering.Deactivate();
        await transaction.SaveAsync(token); await transaction.CommitAsync(token);
    }

    private async Task<long?> OptionalAccountAsync(string? username, string? password, string name, CancellationToken token)
    {
        var noUsername = string.IsNullOrWhiteSpace(username); var noPassword = string.IsNullOrEmpty(password);
        if (noUsername && noPassword) return null;
        if (noUsername || noPassword || username!.Length > 80 || password!.Length is < 8 or > 128)
            throw new ConfigurationException("Yeni kullanıcı adını ve 8–128 karakterlik parolayı birlikte girin.");
        return await accounts.CreateAsync(username.Trim(), name, password, token);
    }
}
