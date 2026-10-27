using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Domain.Catalog;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Patients;
using HospitalAppointments.Domain.Scheduling;
using HospitalAppointments.Domain.Staff;

namespace HospitalAppointments.Domain.Tests;

public sealed class BookingDomainTests
{
    private static DateTime At(int hour, int minute = 0) => new(2030, 1, 8, hour, minute, 0, DateTimeKind.Utc);
    private static StaffOffering Offering()
    {
        var facility = new Facility("Demo", FacilityType.Hospital);
        var doctor = new Doctor(facility, 1, "Dr. Demo", new Specialty("Genel Tıp"));
        return new StaffOffering(doctor, new HealthcareService(new Department(new Branch(facility, "Kuzey"), "Tıp"), "Muayene", 30, 600.25m));
    }

    [Theory]
    [InlineData(9, 0, 10, 0, 9, 30, 10, 30, true)]
    [InlineData(9, 0, 10, 0, 10, 0, 11, 0, false)]
    [InlineData(9, 0, 11, 0, 9, 15, 9, 30, true)]
    [InlineData(10, 0, 11, 0, 9, 0, 10, 0, false)]
    public void OverlapIsSymmetricAndEndExclusive(int ah, int am, int bh, int bm, int ch, int cm, int dh, int dm, bool expected)
    {
        var a = new TimeInterval(At(ah, am), At(bh, bm)); var b = new TimeInterval(At(ch, cm), At(dh, dm));
        Assert.Equal(expected, a.Overlaps(b)); Assert.Equal(expected, b.Overlaps(a));
    }

    [Fact]
    public void CoverageMergesAdjacencyButNeverBridgesPositiveGaps()
    {
        var requested = new TimeInterval(At(9, 45), At(10, 15));
        Assert.True(requested.IsCoveredBy([new(At(10), At(11)), new(At(9), At(10))]));
        Assert.False(requested.IsCoveredBy([new(At(9), At(10)), new(At(10, 15), At(11))]));
        Assert.False(requested.IsCoveredBy([]));
        Assert.True(new TimeInterval(At(9), At(11)).Contains(new(At(10, 30), At(11))));
        Assert.False(new TimeInterval(At(9), At(11)).Contains(new(At(10, 45), At(11, 15))));
    }

    [Fact]
    public void InvalidIntervalsAndNonUtcInstantsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new TimeInterval(At(10), At(10)));
        Assert.Throws<ArgumentException>(() => new TimeInterval(At(11), At(10)));
        Assert.Throws<ArgumentException>(() => new TimeInterval(DateTime.SpecifyKind(At(9), DateTimeKind.Unspecified), At(10)));
    }

    [Fact]
    public void BothStaffTypesImplementTheRealSharedProfessionalContract()
    {
        var offering = Offering();
        StaffMember[] people = [offering.Staff, new HealthcareStaff(offering.Staff.Facility, null, "Demo Teknisyen", "Laboratuvar Teknikeri")];
        Assert.Equal(["Genel Tıp", "Laboratuvar Teknikeri"], people.Select(p => p.ProfessionalDescription));
        Assert.True(typeof(StaffMember).IsAbstract);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1440)]
    public void InvalidServiceDurationsLeaveTermsUnchanged(int duration)
    {
        var service = Offering().Service;
        Assert.Throws<ArgumentOutOfRangeException>(() => service.ReviseTerms(duration, 1m));
        Assert.Equal(30, service.DurationMinutes); Assert.Equal(600.25m, service.Fee);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1.001")]
    [InlineData("10000000000")]
    public void FeesRequireExactNonnegativeMinorUnits(string amount)
    {
        var service = Offering().Service;
        var version = service.Version;
        Assert.Throws<ArgumentOutOfRangeException>(() => service.ReviseTerms(45, decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
        Assert.Equal(30, service.DurationMinutes); Assert.Equal(version, service.Version);
    }

    [Fact]
    public void BookedSnapshotsSurviveCatalogAndEmployeeEdits()
    {
        var offering = Offering(); var appointment = new Appointment(new Patient(2, "Demo"), offering, new(At(10), At(10, 30)), At(9));
        offering.Service.ReviseTerms(45, 999m); offering.Service.Rename("Değişmiş"); offering.Staff.Rename("Yeni ad");
        Assert.Equal(600.25m, appointment.BookedFee); Assert.Equal(30, appointment.BookedDurationMinutes);
        Assert.Equal("Muayene", appointment.BookedServiceName); Assert.Equal("Dr. Demo", appointment.BookedStaffName);
        Assert.Equal(At(10, 30), appointment.End);
    }

    [Fact]
    public void DecisionsAreOneWayAndInvalidAuditInputIsAtomic()
    {
        var appointment = new Appointment(new Patient(2, "Demo"), Offering(), new(At(10), At(10, 30)), At(9));
        Assert.Throws<ArgumentOutOfRangeException>(() => appointment.Approve(0, At(9)));
        Assert.Throws<ArgumentException>(() => appointment.Approve(1, DateTime.SpecifyKind(At(9), DateTimeKind.Unspecified)));
        Assert.Equal(AppointmentStatus.Pending, appointment.Status); Assert.Null(appointment.DecidedAt); Assert.Null(appointment.DecidedByAccountId);
        appointment.Approve(1, At(9));
        Assert.Equal(1, appointment.DecidedByAccountId); Assert.Equal(At(9), appointment.DecidedAt);
        Assert.Throws<InvalidOperationException>(() => appointment.Reject(1, At(9)));
        var rejected = new Appointment(new Patient(2, "Demo"), Offering(), new(At(10), At(10, 30)), At(9));
        rejected.Reject(1, At(9));
        Assert.Throws<InvalidOperationException>(() => rejected.Approve(1, At(9)));
    }

    [Fact]
    public void OfferingAndAvailabilityCannotCrossEmployingFacilities()
    {
        var offering = Offering(); var otherBranch = new Branch(new Facility("Other", FacilityType.Clinic), "Other");
        Assert.Throws<ArgumentException>(() => new StaffOffering(offering.Staff, new HealthcareService(new Department(otherBranch, "Lab"), "Lab", 20, 1m)));
        Assert.Throws<ArgumentException>(() => new AvailabilityWindow(offering.Staff, otherBranch, new(At(9), At(10))));
    }

    [Fact]
    public void StateMethodsDoNotReassignOwnershipAndDeactivationRetainsHistory()
    {
        var offering = Offering(); var owner = offering.Branch.Facility;
        Assert.Throws<ArgumentOutOfRangeException>(() => owner.UpdateDetails("Wrong", (FacilityType)99));
        Assert.Equal("Demo", owner.Name);
        offering.Deactivate(); Assert.False(offering.IsBookable); offering.Activate();
        offering.Staff.Deactivate(); Assert.False(offering.IsBookable); offering.Staff.Activate();
        offering.Service.Deactivate(); Assert.False(offering.IsBookable);
        Assert.Same(owner, offering.Staff.Facility);
        Assert.Throws<ArgumentException>(() => new Appointment(new Patient(2, "Demo"), offering, new(At(10), At(10, 15)), At(9)));
    }
}
