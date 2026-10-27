namespace HospitalAppointments.Domain.Facilities;

public sealed class Branch
{
    public long Id { get; private set; }
    public long FacilityId { get; private set; }
    public Facility Facility { get; private set; } = null!;
    public string Name { get; private set; } = "";
    private Branch() { }
    public Branch(Facility facility, string name)
    {
        Facility = facility ?? throw new ArgumentNullException(nameof(facility));
        Rename(name);
    }
    public void Rename(string name) => Name = DomainRules.Text(name);
}
