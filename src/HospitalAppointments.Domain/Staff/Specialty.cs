namespace HospitalAppointments.Domain.Staff;

public sealed class Specialty
{
    public long Id { get; private set; }
    public string Name { get; private set; } = "";
    private Specialty() { }
    public Specialty(string name) => Name = DomainRules.Text(name);
}
