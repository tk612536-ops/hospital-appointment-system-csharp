using HospitalAppointments.Domain.Facilities;

namespace HospitalAppointments.Domain.Staff;

public sealed class Doctor : StaffMember
{
    public long SpecialtyId { get; private set; }
    public Specialty Specialty { get; private set; } = null!;
    public override string ProfessionalDescription => Specialty.Name;
    private Doctor() { }
    public Doctor(Facility facility, long? accountId, string name, Specialty specialty) : base(facility, accountId, name)
        => AssignSpecialty(specialty);
    public void AssignSpecialty(Specialty specialty) => Specialty = specialty ?? throw new ArgumentNullException(nameof(specialty));
}
