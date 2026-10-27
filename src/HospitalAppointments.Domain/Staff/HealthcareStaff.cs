using HospitalAppointments.Domain.Facilities;

namespace HospitalAppointments.Domain.Staff;

public sealed class HealthcareStaff : StaffMember
{
    public string JobTitle { get; private set; } = "";
    public override string ProfessionalDescription => JobTitle;
    private HealthcareStaff() { }
    public HealthcareStaff(Facility facility, long? accountId, string name, string title) : base(facility, accountId, name)
        => ChangeJobTitle(title);
    public void ChangeJobTitle(string title) => JobTitle = DomainRules.Text(title);
}
