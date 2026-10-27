using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Scheduling;

namespace HospitalAppointments.Domain.Staff;

public sealed class AvailabilityWindow
{
    public long Id { get; private set; }
    public long StaffId { get; private set; }
    public StaffMember Staff { get; private set; } = null!;
    public long BranchId { get; private set; }
    public Branch Branch { get; private set; } = null!;
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }
    public TimeInterval Interval => new(Start, End);
    private AvailabilityWindow() { }
    public AvailabilityWindow(StaffMember staff, Branch branch, TimeInterval interval)
    {
        ArgumentNullException.ThrowIfNull(staff);
        ArgumentNullException.ThrowIfNull(branch);
        if (!staff.Facility.SameAs(branch.Facility)) throw new ArgumentException("Availability must use an employing branch.");
        Staff = staff;
        Branch = branch;
        Start = interval.Start;
        End = interval.End;
    }
    public void Reschedule(TimeInterval interval)
    {
        ArgumentNullException.ThrowIfNull(interval);
        Start = interval.Start;
        End = interval.End;
    }
}
