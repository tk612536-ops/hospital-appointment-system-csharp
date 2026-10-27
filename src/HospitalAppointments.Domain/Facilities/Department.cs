namespace HospitalAppointments.Domain.Facilities;

public sealed class Department
{
    public long Id { get; private set; }
    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = null!;
    public string Name { get; private set; } = "";
    private Department() { }
    public Department(Branch branch, string name)
    {
        Branch = branch ?? throw new ArgumentNullException(nameof(branch));
        Rename(name);
    }
    public void Rename(string name) => Name = DomainRules.Text(name);
}
