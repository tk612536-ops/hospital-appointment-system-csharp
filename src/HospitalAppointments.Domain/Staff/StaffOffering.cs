using HospitalAppointments.Domain.Catalog;
using HospitalAppointments.Domain.Facilities;

namespace HospitalAppointments.Domain.Staff;

public sealed class StaffOffering
{
    public long Id { get; private set; }
    public long StaffId { get; private set; }
    public StaffMember Staff { get; private set; } = null!;
    public long ServiceId { get; private set; }
    public HealthcareService Service { get; private set; } = null!;
    public bool IsActive { get; private set; } = true;
    public Branch Branch => Service.Department.Branch;
    public bool IsBookable => IsActive && Staff.IsActive && Service.IsActive;
    private StaffOffering() { }
    public StaffOffering(StaffMember staff, HealthcareService service)
    {
        ArgumentNullException.ThrowIfNull(staff);
        ArgumentNullException.ThrowIfNull(service);
        if (!staff.Facility.SameAs(service.Department.Branch.Facility)) throw new ArgumentException("Offering must belong to the employing facility.");
        Staff = staff;
        Service = service;
    }
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
