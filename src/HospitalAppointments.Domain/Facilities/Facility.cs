namespace HospitalAppointments.Domain.Facilities;

public enum FacilityType { Hospital, Clinic }

public sealed class Facility
{
    public long Id { get; private set; }
    public string Name { get; private set; } = "";
    public FacilityType Type { get; private set; }
    private Facility() { }
    public Facility(string name, FacilityType type) => UpdateDetails(name, type);
    public void UpdateDetails(string name, FacilityType type)
    {
        var validName = DomainRules.Text(name);
        if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type));
        Name = validName;
        Type = type;
    }
    public bool SameAs(Facility other) => ReferenceEquals(this, other) || Id > 0 && Id == other.Id;
}
