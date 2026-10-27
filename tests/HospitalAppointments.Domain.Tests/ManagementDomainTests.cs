using HospitalAppointments.Domain.Catalog;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Scheduling;
using HospitalAppointments.Domain.Staff;

namespace HospitalAppointments.Domain.Tests;

public sealed class ManagementDomainTests
{
    [Theory]
    [InlineData("", 30, "12.50")]
    [InlineData("Revised", 0, "12.50")]
    [InlineData("Revised", 30, "12.501")]
    [InlineData("Revised", 30, "-1")]
    public void ServiceEditValidatesAllFieldsBeforeChangingAnything(string name, int duration, string price)
    {
        var department = new Department(new Branch(new Facility("Demo", FacilityType.Clinic), "Main"), "Care");
        var service = new HealthcareService(department, "Original", 45, 600.25m); var version = service.Version;
        Assert.ThrowsAny<ArgumentException>(() => service.UpdateDetails(name, duration, decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture)));
        Assert.Equal("Original", service.Name); Assert.Equal(45, service.DurationMinutes); Assert.Equal(600.25m, service.Fee); Assert.Equal(version, service.Version);
        service.UpdateDetails("Revised", 20, 77.50m);
        Assert.Equal(version + 1, service.Version); Assert.Same(department, service.Department); Assert.Equal(77.50m, service.Fee);
    }

    [Fact]
    public void InvalidNamedEditsLeaveNamesAndOwnershipUnchanged()
    {
        var facility = new Facility("Demo", FacilityType.Clinic); var branch = new Branch(facility, "Main"); var department = new Department(branch, "Care");
        var specialty = new Specialty("General care"); var doctor = new Doctor(facility, null, "Doctor", specialty);
        var worker = new HealthcareStaff(facility, null, "Worker", "Nurse");
        Assert.ThrowsAny<ArgumentException>(() => branch.Rename(" ")); Assert.Equal("Main", branch.Name); Assert.Same(facility, branch.Facility);
        Assert.ThrowsAny<ArgumentException>(() => department.Rename(" ")); Assert.Equal("Care", department.Name); Assert.Same(branch, department.Branch);
        Assert.ThrowsAny<ArgumentException>(() => doctor.Rename(" ")); Assert.Equal("Doctor", doctor.Name);
        Assert.Throws<ArgumentNullException>(() => doctor.AssignSpecialty(null!)); Assert.Same(specialty, doctor.Specialty);
        Assert.ThrowsAny<ArgumentException>(() => worker.ChangeJobTitle(" ")); Assert.Equal("Nurse", worker.ProfessionalDescription); Assert.Same(facility, worker.Facility);
    }

    [Fact]
    public void ReschedulingPreservesTheEmployeeAndBranchAndRejectsNullAtomically()
    {
        var facility = new Facility("Demo", FacilityType.Hospital); var branch = new Branch(facility, "Main");
        var worker = new HealthcareStaff(facility, null, "Demo", "Technician");
        var start = new DateTime(2030, 1, 9, 6, 0, 0, DateTimeKind.Utc);
        var window = new AvailabilityWindow(worker, branch, new(start, start.AddHours(2)));
        Assert.Throws<ArgumentNullException>(() => window.Reschedule(null!)); Assert.Equal(start, window.Start);
        window.Reschedule(new TimeInterval(start.AddDays(1), start.AddDays(1).AddHours(1)));
        Assert.Same(worker, window.Staff); Assert.Same(branch, window.Branch); Assert.Equal(start.AddDays(1), window.Start);
    }
}
