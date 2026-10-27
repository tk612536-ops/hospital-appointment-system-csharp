using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Domain.Catalog;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Patients;
using HospitalAppointments.Domain.Staff;
using HospitalAppointments.Infrastructure.Identity;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointments.Infrastructure.Demo;

public sealed class DemoBookingSeeder(ApplicationDbContext database, UserManager<ApplicationUser> users, ClinicTime time)
{
    public async Task SeedAsync()
    {
        if (await database.Set<DemoSeedMarker>().AnyAsync(m => m.Key == "booking-v1")) return;
        var patientAccount = (await users.FindByNameAsync("patient"))!;
        var doctorAccount = (await users.FindByNameAsync("staff"))!;
        var manager = (await users.FindByNameAsync("manager"))!;
        var patient2 = await ExtraAccountAsync("patient2", "Demo İkinci Hasta", RoleNames.Patient);
        var labAccount = await ExtraAccountAsync("lab", "Demo Laboratuvar Personeli", RoleNames.Staff);
        var clinicStaff = await ExtraAccountAsync("clinicstaff", "Demo Klinik Personeli", RoleNames.Staff);
        var clinicManager = await ExtraAccountAsync("clinicmanager", "Demo Klinik Yöneticisi", RoleNames.Manager);
        var hospital = new Facility("Demo Bakım Hastanesi", FacilityType.Hospital);
        var clinic = new Facility("Demo Klinik", FacilityType.Clinic);
        var north = new Branch(hospital, "Kuzey"); var south = new Branch(hospital, "Güney"); var central = new Branch(clinic, "Merkez");
        var medicine = new Department(north, "Genel Tıp"); var southMedicine = new Department(south, "Genel Tıp");
        var laboratory = new Department(north, "Laboratuvar"); var clinicLab = new Department(central, "Laboratuvar");
        var doctor = new Doctor(hospital, doctorAccount.Id, "Dr. Deniz (Demo)", new Specialty("Genel Tıp"));
        var lab = new HealthcareStaff(hospital, labAccount.Id, "Ece (Demo)", "Laboratuvar Teknikeri");
        var worker = new HealthcareStaff(clinic, clinicStaff.Id, "Can (Demo)", "Laboratuvar Teknikeri");
        var shortExam = new StaffOffering(doctor, new HealthcareService(medicine, "Genel muayene", 30, 600m));
        var longExam = new StaffOffering(doctor, new HealthcareService(medicine, "Uzun muayene", 45, 900m));
        var southExam = new StaffOffering(doctor, new HealthcareService(southMedicine, "Genel muayene", 30, 650m));
        var labOffering = new StaffOffering(lab, new HealthcareService(laboratory, "Laboratuvar işlemi", 20, 250.25m));
        var clinicOffering = new StaffOffering(worker, new HealthcareService(clinicLab, "Laboratuvar işlemi", 20, 250m));
        var firstPatient = new Patient(patientAccount.Id, patientAccount.DisplayName);
        var secondPatient = new Patient(patient2.Id, patient2.DisplayName);
        database.AddRange(shortExam, longExam, southExam, labOffering, clinicOffering, firstPatient, secondPatient,
            new ManagerFacilityGrant(manager.Id, hospital), new ManagerFacilityGrant(clinicManager.Id, clinic));
        foreach (var branch in new[] { north, south, central })
            foreach (var weekday in Enum.GetValues<DayOfWeek>())
                database.AddRange(new OpeningWindow(branch, weekday, new TimeOnly(9, 0), new TimeOnly(12, 0)),
                    new OpeningWindow(branch, weekday, new TimeOnly(13, 0), new TimeOnly(17, 0)));
        var firstDate = time.Today.AddDays(1);
        for (var day = 0; day < 14; day++)
        {
            var date = firstDate.AddDays(day);
            database.AddRange(new AvailabilityWindow(doctor, north, time.Interval(date, new(9, 0), new(11, 0))),
                new AvailabilityWindow(doctor, south, time.Interval(date, new(13, 0), new(15, 0))),
                new AvailabilityWindow(lab, north, time.Interval(date, new(9, 0), new(12, 0))),
                new AvailabilityWindow(lab, north, time.Interval(date, new(13, 0), new(17, 0))),
                new AvailabilityWindow(worker, central, time.Interval(date, new(9, 0), new(12, 0))));
        }
        database.Add(new Appointment(secondPatient, longExam, time.Interval(firstDate, new(9, 0), new(9, 45)), time.UtcNow));
        database.Add(new DemoSeedMarker("booking-v1"));
        await database.SaveChangesAsync();
    }
    private async Task<ApplicationUser> ExtraAccountAsync(string username, string name, string role)
    {
        var existing = await users.FindByNameAsync(username);
        if (existing is not null) return existing;
        var user = new ApplicationUser(username, name);
        DemoAccountSeeder.RequireSuccess(await users.CreateAsync(user, DemoAccountSeeder.Password));
        DemoAccountSeeder.RequireSuccess(await users.AddToRoleAsync(user, role));
        return user;
    }
}
