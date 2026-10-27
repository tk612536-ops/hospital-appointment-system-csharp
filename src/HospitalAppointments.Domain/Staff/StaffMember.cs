using HospitalAppointments.Domain.Facilities;

namespace HospitalAppointments.Domain.Staff;

public abstract class StaffMember
{
    public long Id { get; private set; }
    public long FacilityId { get; private set; }
    public Facility Facility { get; private set; } = null!;
    public long? AccountId { get; private set; }
    public string Name { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    public abstract string ProfessionalDescription { get; }
    protected StaffMember() { }
    protected StaffMember(Facility facility, long? accountId, string name)
    {
        if (accountId is <= 0) throw new ArgumentOutOfRangeException(nameof(accountId));
        Facility = facility ?? throw new ArgumentNullException(nameof(facility));
        AccountId = accountId;
        Rename(name);
    }
    public void Rename(string name) => Name = DomainRules.Text(name);
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
