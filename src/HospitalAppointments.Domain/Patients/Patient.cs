namespace HospitalAppointments.Domain.Patients;

public sealed class Patient
{
    public long Id { get; private set; }
    public long AccountId { get; private set; }
    public string Name { get; private set; } = "";
    private Patient() { }
    public Patient(long accountId, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(accountId);
        AccountId = accountId;
        Name = DomainRules.Text(name, 120);
    }
}
