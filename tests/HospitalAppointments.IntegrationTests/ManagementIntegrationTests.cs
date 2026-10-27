using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Application.Transactions;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Staff;
using HospitalAppointments.Infrastructure.Identity;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalAppointments.IntegrationTests;

public sealed class ManagementIntegrationTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task FacilityCreationGrantsOwnershipAndSupportsSingleAndMultipleBranches()
    {
        using var s = await ManagementScenario.CreateAsync();
        await s.Run(p => p.GetRequiredService<FacilityManagementService>().UpdateAsync(s.Facility, "Renamed clinic", FacilityType.Clinic, Token));
        var other = await s.Run(p => p.GetRequiredService<FacilityManagementService>().AddBranchAsync(s.Facility, "Second", Token));
        await s.Run(p => p.GetRequiredService<FacilityManagementService>().RenameBranchAsync(s.Facility, other, "Second renamed", Token));
        await s.Run(p => p.GetRequiredService<FacilityManagementService>().RenameDepartmentAsync(s.Facility, s.Department, "Care renamed", Token));
        var page = await s.PageAsync();
        Assert.Equal(FacilityType.Clinic, page.Facility.Type); Assert.Equal("Renamed clinic", page.Facility.Name); Assert.Equal(2, page.Branches.Count);
        Assert.Contains(page.Branches, b => b.Id == other && b.Name == "Second renamed"); Assert.Equal("Care renamed", Assert.Single(page.Departments).Name);
        var owned = await s.Run(p => p.GetRequiredService<ManagementQueryService>().FacilitiesAsync(Token));
        Assert.Contains(owned, f => f.Id == s.Facility);
        var foreign = await s.Run(p => p.GetRequiredService<ManagementQueryService>().FacilitiesAsync(Token), "clinicmanager");
        Assert.DoesNotContain(foreign, f => f.Id == s.Facility);
    }

    [Fact]
    public async Task InvalidOrDuplicateFacilityCreationLeavesNoPartialBranchOrGrant()
    {
        using var s = await ManagementScenario.CreateAsync();
        await Assert.ThrowsAnyAsync<ArgumentException>(() => s.Run(p => p.GetRequiredService<FacilityManagementService>().CreateAsync("Should rollback", FacilityType.Clinic, " ", Token)));
        await Assert.ThrowsAsync<PersistenceConflictException>(() => s.Run(p => p.GetRequiredService<FacilityManagementService>().CreateAsync("Configured hospital", FacilityType.Clinic, "Duplicate branch", Token)));
        await s.Run(async p =>
        {
            var db = p.GetRequiredService<ApplicationDbContext>();
            Assert.False(await db.Set<Facility>().AnyAsync(f => f.Name == "Should rollback", Token));
            Assert.False(await db.Set<Branch>().AnyAsync(b => b.Name == "Duplicate branch", Token));
            Assert.Equal(1, await db.Set<ManagerFacilityGrant>().CountAsync(g => g.FacilityId == s.Facility, Token));
        });
    }

    [Theory]
    [InlineData("patient")]
    [InlineData("staff")]
    [InlineData("clinicstaff")]
    [InlineData("clinicmanager")]
    public async Task EveryManagementServiceEnforcesRoleAndFacilityGrantDirectly(string actor)
    {
        using var s = await ManagementScenario.CreateAsync();
        Func<IServiceProvider, Task>[] commands = [
            p => p.GetRequiredService<FacilityManagementService>().UpdateAsync(s.Facility, "Forged", FacilityType.Clinic, Token),
            p => p.GetRequiredService<FacilityManagementService>().AddBranchAsync(s.Facility, "Forged", Token),
            p => p.GetRequiredService<FacilityManagementService>().AddDepartmentAsync(s.Facility, s.Branch, "Forged", Token),
            p => p.GetRequiredService<CatalogManagementService>().CreateAsync(s.Facility, s.Department, "Forged", 30, 1m, Token),
            p => p.GetRequiredService<CatalogManagementService>().ChangeActiveAsync(s.Facility, s.Service, false, Token),
            p => p.GetRequiredService<StaffManagementService>().AddSpecialtyAsync(s.Facility, "Forged", Token),
            p => p.GetRequiredService<StaffManagementService>().RegisterDoctorAsync(s.Facility, "Forged", s.Specialty, null, null, Token),
            p => p.GetRequiredService<StaffManagementService>().RegisterHealthcareStaffAsync(s.Facility, "Forged", "Nurse", null, null, Token),
            p => p.GetRequiredService<StaffManagementService>().UpdateDoctorAsync(s.Facility, s.Doctor, "Forged", s.Specialty, false, Token),
            p => p.GetRequiredService<StaffManagementService>().AssignServiceAsync(s.Facility, s.Doctor, s.Service, Token),
            p => p.GetRequiredService<StaffManagementService>().ChangeOfferingActiveAsync(s.Facility, s.Offering, false, Token),
            p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(s.Facility, s.Branch, s.Date.DayOfWeek, [], Token),
            p => p.GetRequiredService<ScheduleManagementService>().AddAvailabilityAsync(s.Facility, s.Branch, s.Doctor, s.Date, new(14, 0), new(15, 0), Token),
            p => p.GetRequiredService<ScheduleManagementService>().UpdateAvailabilityAsync(s.Facility, s.Window, s.Date, new(9, 0), new(10, 0), Token),
            p => p.GetRequiredService<ScheduleManagementService>().RemoveAvailabilityAsync(s.Facility, s.Window, Token),
            p => p.GetRequiredService<ManagementQueryService>().PageAsync(s.Facility, Token)
        ];
        foreach (var command in commands) await Assert.ThrowsAsync<AccountAccessDeniedException>(() => s.Run(command, actor));
        if (actor != "clinicmanager") await Assert.ThrowsAsync<AccountAccessDeniedException>(() => s.Run(p => p.GetRequiredService<FacilityManagementService>().CreateAsync("Forged", FacilityType.Clinic, "Main", Token), actor));
        Assert.Equal("Configured hospital", (await s.PageAsync()).Facility.Name);
    }

    [Fact]
    public async Task ForgedChildResourcesAreRejectedEvenWhenManagerOwnsTheUrlFacility()
    {
        using var s = await ManagementScenario.CreateAsync();
        var seeded = await s.Run(p => p.GetRequiredService<ManagementQueryService>().FacilitiesAsync(Token));
        var other = await s.Run(p => p.GetRequiredService<ManagementQueryService>().PageAsync(seeded.First(f => f.Id != s.Facility).Id, Token));
        var foreignBranch = other.Branches.First().Id; var foreignStaff = other.Staff.First().Id;
        var foreignService = other.Services.First().Id; var foreignDepartment = other.Departments.First().Id;
        Func<IServiceProvider, Task>[] commands = [
            p => p.GetRequiredService<FacilityManagementService>().RenameBranchAsync(s.Facility, foreignBranch, "Forged", Token),
            p => p.GetRequiredService<FacilityManagementService>().AddDepartmentAsync(s.Facility, foreignBranch, "Forged", Token),
            p => p.GetRequiredService<FacilityManagementService>().RenameDepartmentAsync(s.Facility, foreignDepartment, "Forged", Token),
            p => p.GetRequiredService<CatalogManagementService>().CreateAsync(s.Facility, foreignDepartment, "Forged", 30, 1m, Token),
            p => p.GetRequiredService<CatalogManagementService>().UpdateAsync(s.Facility, foreignService, "Forged", 30, 1m, Token),
            p => p.GetRequiredService<CatalogManagementService>().ChangeActiveAsync(s.Facility, foreignService, false, Token),
            p => p.GetRequiredService<StaffManagementService>().UpdateDoctorAsync(s.Facility, foreignStaff, "Forged", s.Specialty, false, Token),
            p => p.GetRequiredService<StaffManagementService>().AssignServiceAsync(s.Facility, s.Doctor, foreignService, Token),
            p => p.GetRequiredService<StaffManagementService>().AssignServiceAsync(s.Facility, foreignStaff, s.Service, Token),
            p => p.GetRequiredService<StaffManagementService>().ChangeOfferingActiveAsync(s.Facility, other.Offerings.First().Id, false, Token),
            p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(s.Facility, foreignBranch, s.Date.DayOfWeek, [], Token),
            p => p.GetRequiredService<ScheduleManagementService>().AddAvailabilityAsync(s.Facility, foreignBranch, s.Doctor, s.Date, new(14, 0), new(15, 0), Token),
            p => p.GetRequiredService<ScheduleManagementService>().AddAvailabilityAsync(s.Facility, s.Branch, foreignStaff, s.Date, new(14, 0), new(15, 0), Token),
            p => p.GetRequiredService<ScheduleManagementService>().UpdateAvailabilityAsync(s.Facility, other.Availability.First().Id, s.Date, new(9, 0), new(10, 0), Token),
            p => p.GetRequiredService<ScheduleManagementService>().RemoveAvailabilityAsync(s.Facility, other.Availability.First().Id, Token)
        ];
        foreach (var command in commands) await Assert.ThrowsAsync<AccountAccessDeniedException>(() => s.Run(command));
        Assert.Equal("Main", (await s.PageAsync()).Branches.Single().Name);
    }

    [Theory]
    [InlineData(0, "10")]
    [InlineData(1440, "10")]
    [InlineData(30, "-1")]
    [InlineData(30, "0.001")]
    [InlineData(30, "10000000000")]
    public async Task InvalidServiceTermsRollbackWithoutRounding(int duration, string price)
    {
        using var s = await ManagementScenario.CreateAsync();
        await Assert.ThrowsAnyAsync<ArgumentException>(() => s.Run(p => p.GetRequiredService<CatalogManagementService>().UpdateAsync(s.Facility, s.Service, "Invalid", duration,
            decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture), Token)));
        var service = Assert.Single((await s.PageAsync()).Services);
        Assert.Equal("Configured examination", service.Name); Assert.Equal(77.50m, service.Fee); Assert.Equal(30, service.Duration);
    }

    [Fact]
    public async Task CatalogAndNameEditsKeepBookedSnapshotsAndExactMoney()
    {
        using var s = await ManagementScenario.CreateAsync(); var id = await s.BookAsync();
        await s.Run(p => p.GetRequiredService<CatalogManagementService>().UpdateAsync(s.Facility, s.Service, "New service", 45, 123.45m, Token));
        await s.Run(p => p.GetRequiredService<StaffManagementService>().UpdateDoctorAsync(s.Facility, s.Doctor, "New doctor", s.Specialty, true, Token));
        await s.Run(p => p.GetRequiredService<FacilityManagementService>().UpdateAsync(s.Facility, "New facility", FacilityType.Clinic, Token));
        await s.Run(p => p.GetRequiredService<FacilityManagementService>().RenameBranchAsync(s.Facility, s.Branch, "New branch", Token));
        var detail = await s.Booking.DetailAsync("patient", id);
        Assert.Equal("Configured examination", detail.ServiceName); Assert.Equal("Dr. Synthetic", detail.StaffName);
        // Facility/branch are current navigational labels; service/staff names and commercial terms are booked snapshots.
        Assert.Equal("New facility", detail.Facility); Assert.Equal("New branch", detail.Branch); Assert.Equal(30, detail.DurationMinutes); Assert.Equal(77.50m, detail.Fee);
        await Assert.ThrowsAsync<BookingException>(() => s.BookAsync(11)); // stale displayed catalog version
        await s.Run(async p =>
        {
            var db = p.GetRequiredService<ApplicationDbContext>(); await db.Database.OpenConnectionAsync(Token);
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT typeof(FeeMinor) || ':' || FeeMinor FROM HealthcareServices WHERE Id = $id";
            var param = command.CreateParameter(); param.ParameterName = "$id"; param.Value = s.Service; command.Parameters.Add(param);
            Assert.Equal("integer:12345", await command.ExecuteScalarAsync(Token));
        });
    }

    [Theory]
    [InlineData("service")]
    [InlineData("doctor")]
    [InlineData("offering")]
    public async Task DeactivationBlocksNewBookingsButKeepsHistoryAndAssignedApproval(string resource)
    {
        using var s = await ManagementScenario.CreateAsync(); var id = await s.BookAsync();
        await s.Run(p => resource switch
        {
            "service" => p.GetRequiredService<CatalogManagementService>().ChangeActiveAsync(s.Facility, s.Service, false, Token),
            "doctor" => p.GetRequiredService<StaffManagementService>().UpdateDoctorAsync(s.Facility, s.Doctor, "Dr. Synthetic", s.Specialty, false, Token),
            _ => p.GetRequiredService<StaffManagementService>().ChangeOfferingActiveAsync(s.Facility, s.Offering, false, Token)
        });
        await Assert.ThrowsAsync<BookingException>(() => s.BookAsync(11));
        await s.Booking.DecideAsync(ManagementScenario.DoctorLogin, id, AppointmentStatus.Approved);
        Assert.Equal(AppointmentStatus.Approved, (await s.Booking.DetailAsync("patient", id)).Status);
    }

    [Fact]
    public async Task DoctorAndHealthcareStaffRemainPolymorphicAndOfferingAssignmentIsIdempotent()
    {
        using var s = await ManagementScenario.CreateAsync();
        var nurse = await s.Run(p => p.GetRequiredService<StaffManagementService>().RegisterHealthcareStaffAsync(s.Facility, "Synthetic worker", "Nurse", null, null, Token));
        await s.Run(p => p.GetRequiredService<StaffManagementService>().UpdateHealthcareStaffAsync(s.Facility, nurse, "Renamed worker", "Physiotherapist", true, Token));
        var newSpecialty = await s.Run(p => p.GetRequiredService<StaffManagementService>().AddSpecialtyAsync(s.Facility, "Revised specialty", Token));
        await s.Run(p => p.GetRequiredService<StaffManagementService>().UpdateDoctorAsync(s.Facility, s.Doctor, "Updated doctor", newSpecialty, true, Token));
        await Assert.ThrowsAsync<BookingNotFoundException>(() => s.Run(p => p.GetRequiredService<StaffManagementService>().UpdateDoctorAsync(s.Facility, nurse, "Invalid", s.Specialty, true, Token)));
        await Assert.ThrowsAsync<BookingNotFoundException>(() => s.Run(p => p.GetRequiredService<StaffManagementService>().UpdateHealthcareStaffAsync(s.Facility, s.Doctor, "Invalid", "Nurse", true, Token)));
        var other = await s.Run(p => p.GetRequiredService<StaffManagementService>().AssignServiceAsync(s.Facility, nurse, s.Service, Token));
        await s.Run(p => p.GetRequiredService<StaffManagementService>().ChangeOfferingActiveAsync(s.Facility, other, false, Token));
        Assert.Equal(other, await s.Run(p => p.GetRequiredService<StaffManagementService>().AssignServiceAsync(s.Facility, nurse, s.Service, Token)));
        Assert.Equal(s.Specialty, await s.Run(p => p.GetRequiredService<StaffManagementService>().AddSpecialtyAsync(s.Facility, " Configured specialty ", Token)));
        var page = await s.PageAsync(); Assert.Equal(2, page.Staff.Count); Assert.Equal(2, page.Offerings.Count);
        Assert.Equal("Revised specialty", page.Staff.Single(w => w.Id == s.Doctor).Profession);
        Assert.Equal("Physiotherapist", page.Staff.Single(w => w.Id == nurse).Profession); Assert.False(page.Staff.Single(w => w.Id == nurse).HasLogin);
        await s.Run(p => p.GetRequiredService<ScheduleManagementService>().AddAvailabilityAsync(s.Facility, s.Branch, nurse, s.Date, new(9, 0), new(11, 0), Token));
        var id = await s.Run(p => p.GetRequiredService<AppointmentService>().BookAsync(new(other, s.Date, new(10, 0), s.Version), Token), "patient");
        Assert.Equal("Physiotherapist", (await s.Booking.DetailAsync("patient", id)).ProfessionalDescription);
    }

    [Fact]
    public async Task NewStaffLoginHasOnlyStaffRoleAndUsesIdentityHashing()
    {
        using var s = await ManagementScenario.CreateAsync();
        await s.Run(async p =>
        {
            var users = p.GetRequiredService<UserManager<ApplicationUser>>(); var user = (await users.FindByNameAsync(ManagementScenario.DoctorLogin))!;
            Assert.Equal([RoleNames.Staff], await users.GetRolesAsync(user));
            Assert.NotEqual(ManagementScenario.Password, user.PasswordHash); Assert.True(await users.CheckPasswordAsync(user, ManagementScenario.Password));
            Assert.False(await users.CheckPasswordAsync(user, "WrongPassword!"));
        });
    }

    [Theory]
    [InlineData("manager", "ValidPassword10!")]
    [InlineData("MANAGER", "ValidPassword10!")]
    [InlineData("new-user", "weakpass")]
    [InlineData("new-user", null)]
    [InlineData(null, "ValidPassword10!")]
    public async Task InvalidCredentialsOrExistingAccountsCannotCreateOrTakeOverAProfile(string? username, string? password)
    {
        using var s = await ManagementScenario.CreateAsync();
        await Assert.ThrowsAsync<ConfigurationException>(() => s.Run(p => p.GetRequiredService<StaffManagementService>().RegisterHealthcareStaffAsync(s.Facility, "Invalid", "Nurse", username, password, Token)));
        Assert.Single((await s.PageAsync()).Staff);
        await s.Run(async p => Assert.Null(await p.GetRequiredService<UserManager<ApplicationUser>>().FindByNameAsync("new-user")));
    }

    [Fact]
    public async Task ProfileFailureAfterIdentityWritesRollsBackAccountAndRoleTogether()
    {
        using var s = await ManagementScenario.CreateAsync();
        await Assert.ThrowsAnyAsync<ArgumentException>(() => s.Run(p => p.GetRequiredService<StaffManagementService>().RegisterHealthcareStaffAsync(s.Facility, "Synthetic", " ", "rollback-user", ManagementScenario.Password, Token)));
        await s.Run(async p => Assert.Null(await p.GetRequiredService<UserManager<ApplicationUser>>().FindByNameAsync("rollback-user")));
        Assert.Single((await s.PageAsync()).Staff);
    }
}
