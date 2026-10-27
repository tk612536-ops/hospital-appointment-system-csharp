namespace HospitalAppointments.Domain.Facilities;

public sealed class OpeningWindow
{
    public long Id { get; private set; }
    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = null!;
    public DayOfWeek Weekday { get; private set; }
    public TimeOnly OpensAt { get; private set; }
    public TimeOnly ClosesAt { get; private set; }
    private OpeningWindow() { }
    public OpeningWindow(Branch branch, DayOfWeek weekday, TimeOnly opens, TimeOnly closes)
    {
        ArgumentNullException.ThrowIfNull(branch);
        if (!Enum.IsDefined(weekday) || opens >= closes) throw new ArgumentException("Use a positive same-day opening window.");
        Branch = branch;
        Weekday = weekday;
        OpensAt = opens;
        ClosesAt = closes;
    }
}
