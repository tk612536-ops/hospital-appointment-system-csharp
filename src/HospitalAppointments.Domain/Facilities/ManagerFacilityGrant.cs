namespace HospitalAppointments.Domain.Facilities;

public sealed class ManagerFacilityGrant
{
    public long AccountId { get; private set; }
    public long FacilityId { get; private set; }
    public Facility Facility { get; private set; } = null!;
    private ManagerFacilityGrant() { }
    // Caller authorization belongs to the application service, not this association.
    public ManagerFacilityGrant(long accountId, Facility facility)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(accountId);
        AccountId = accountId;
        Facility = facility ?? throw new ArgumentNullException(nameof(facility));
    }
}
