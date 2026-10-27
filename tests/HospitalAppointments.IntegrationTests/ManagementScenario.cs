using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Domain.Facilities;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalAppointments.IntegrationTests;

internal sealed class ManagementScenario(BookingScenario booking) : IDisposable
{
    public BookingScenario Booking { get; } = booking;
    public long Facility { get; private set; }
    public long Branch { get; private set; }
    public long Department { get; private set; }
    public long Service { get; private set; }
    public long Doctor { get; private set; }
    public long Specialty { get; private set; }
    public long Offering { get; private set; }
    public long Window { get; private set; }
    public long Version { get; private set; }
    public DateOnly Date => Booking.Date;
    public const string DoctorLogin = "configured-doctor";
    public const string Password = "SyntheticDoctor10!";
    public static CancellationToken Token => TestContext.Current.CancellationToken;

    public static async Task<ManagementScenario> CreateAsync(bool shift = true)
    {
        var s = new ManagementScenario(await BookingScenario.CreateAsync());
        s.Facility = await s.Run(p => p.GetRequiredService<FacilityManagementService>().CreateAsync("Configured hospital", FacilityType.Hospital, "Main", Token));
        s.Branch = (await s.PageAsync()).Branches.Single().Id;
        s.Department = await s.Run(p => p.GetRequiredService<FacilityManagementService>().AddDepartmentAsync(s.Facility, s.Branch, "Care", Token));
        s.Service = await s.Run(p => p.GetRequiredService<CatalogManagementService>().CreateAsync(s.Facility, s.Department, "Configured examination", 30, 77.50m, Token));
        s.Specialty = await s.Run(p => p.GetRequiredService<StaffManagementService>().AddSpecialtyAsync(s.Facility, "Configured specialty", Token));
        s.Doctor = await s.Run(p => p.GetRequiredService<StaffManagementService>().RegisterDoctorAsync(s.Facility, "Dr. Synthetic", s.Specialty, DoctorLogin, Password, Token));
        s.Offering = await s.Run(p => p.GetRequiredService<StaffManagementService>().AssignServiceAsync(s.Facility, s.Doctor, s.Service, Token));
        await s.Run(p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(s.Facility, s.Branch, s.Date.DayOfWeek, [new(new(9, 0), new(17, 0))], Token));
        if (shift) s.Window = await s.Run(p => p.GetRequiredService<ScheduleManagementService>().AddAvailabilityAsync(s.Facility, s.Branch, s.Doctor, s.Date, new(9, 0), new(12, 0), Token));
        s.Version = await s.Run(async p => (await p.GetRequiredService<HospitalAppointments.Application.Repositories.IBookingCatalogRepository>().FindAsync(s.Offering, Token))!.Service.Version);
        return s;
    }
    public Task<T> Run<T>(Func<IServiceProvider, Task<T>> action, string actor = "manager") => Booking.AsAsync(actor, action);
    public Task Run(Func<IServiceProvider, Task> action, string actor = "manager") => Booking.AsAsync(actor, async p => { await action(p); return true; });
    public Task<ManagementPage> PageAsync() => Run(p => p.GetRequiredService<ManagementQueryService>().PageAsync(Facility, Token));
    public Task<long> BookAsync(int hour = 10, int minute = 0, string actor = "patient") => Run(p => p.GetRequiredService<AppointmentService>()
        .BookAsync(new(Offering, Date, new(hour, minute), Version), Token), actor);
    public Task UpdateWindowAsync(TimeOnly start, TimeOnly end) => Run(p => p.GetRequiredService<ScheduleManagementService>().UpdateAvailabilityAsync(Facility, Window, Date, start, end, Token));
    public async Task<(long Branch, long Offering)> SecondBranchAsync()
    {
        var branch = await Run(p => p.GetRequiredService<FacilityManagementService>().AddBranchAsync(Facility, "Second", Token));
        var department = await Run(p => p.GetRequiredService<FacilityManagementService>().AddDepartmentAsync(Facility, branch, "Care", Token));
        var service = await Run(p => p.GetRequiredService<CatalogManagementService>().CreateAsync(Facility, department, "Other service", 30, 50m, Token));
        var offering = await Run(p => p.GetRequiredService<StaffManagementService>().AssignServiceAsync(Facility, Doctor, service, Token));
        await Run(p => p.GetRequiredService<ScheduleManagementService>().ReplaceOpeningHoursAsync(Facility, branch, Date.DayOfWeek, [new(new(9, 0), new(17, 0))], Token));
        return (branch, offering);
    }
    public void Dispose() => Booking.Dispose();
}
