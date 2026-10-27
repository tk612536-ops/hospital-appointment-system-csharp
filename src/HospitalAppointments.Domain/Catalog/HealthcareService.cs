using HospitalAppointments.Domain.Facilities;

namespace HospitalAppointments.Domain.Catalog;

public sealed class HealthcareService
{
    public long Id { get; private set; }
    public long DepartmentId { get; private set; }
    public Department Department { get; private set; } = null!;
    public string Name { get; private set; } = "";
    public int DurationMinutes { get; private set; }
    public decimal Fee { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public bool IsActive { get; private set; } = true;
    public long Version { get; private set; } = 1;
    private HealthcareService() { }
    public HealthcareService(Department department, string name, int duration, decimal fee)
    {
        Department = department ?? throw new ArgumentNullException(nameof(department));
        Name = DomainRules.Text(name);
        ReviseTerms(duration, fee);
    }
    public void ReviseTerms(int duration, decimal fee)
    {
        if (duration is < 1 or > 1439) throw new ArgumentOutOfRangeException(nameof(duration));
        var validFee = DomainRules.Fee(fee);
        DurationMinutes = duration;
        Fee = validFee;
        Version++;
    }
    public void Rename(string name) { Name = DomainRules.Text(name); Version++; }
    public void UpdateDetails(string name, int duration, decimal fee)
    {
        var validName = DomainRules.Text(name);
        if (duration is < 1 or > 1439) throw new ArgumentOutOfRangeException(nameof(duration));
        var validFee = DomainRules.Fee(fee);
        Name = validName; DurationMinutes = duration; Fee = validFee; Version++;
    }
    public void Deactivate() { IsActive = false; Version++; }
    public void Activate() { IsActive = true; Version++; }
}
