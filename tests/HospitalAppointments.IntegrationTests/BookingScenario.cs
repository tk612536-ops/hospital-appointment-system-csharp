using System.Security.Claims;
using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Domain.Staff;
using HospitalAppointments.Infrastructure.Identity;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalAppointments.IntegrationTests;

internal sealed class FixedClinicTime : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2030, 1, 7, 6, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
}

internal sealed class BookingScenario : IDisposable
{
    public ClinicFactory Factory { get; } = new("Demo", true, seedBookings: true, clock: new FixedClinicTime());
    public DateOnly Date { get; } = new(2030, 1, 9);
    public long North { get; private set; }
    public long South { get; private set; }
    public long Lab { get; private set; }
    public long Clinic { get; private set; }
    public long Version { get; private set; }
    public static CancellationToken Token => TestContext.Current.CancellationToken;

    public static async Task<BookingScenario> CreateAsync()
    {
        var scenario = new BookingScenario();
        await using var scope = scenario.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var offerings = await db.Set<StaffOffering>().Include(o => o.Service).ThenInclude(s => s.Department).ThenInclude(d => d.Branch).ToListAsync(Token);
        scenario.North = offerings.Single(o => o.Service.DurationMinutes == 30 && o.Service.Department.Branch.Name == "Kuzey").Id;
        scenario.South = offerings.Single(o => o.Service.Department.Branch.Name == "Güney").Id;
        scenario.Lab = offerings.Single(o => o.Service.Department.Branch.Name == "Kuzey" && o.Service.DurationMinutes == 20).Id;
        scenario.Clinic = offerings.Single(o => o.Service.Department.Branch.Name == "Merkez").Id;
        scenario.Version = offerings.Single(o => o.Id == scenario.North).Service.Version;
        await db.Set<Appointment>().ExecuteDeleteAsync(Token);
        return scenario;
    }

    public async Task<T> AsAsync<T>(string login, Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var user = await services.GetRequiredService<UserManager<ApplicationUser>>().FindByNameAsync(login)
            ?? throw new InvalidOperationException("Synthetic actor missing.");
        var accessor = services.GetRequiredService<IHttpContextAccessor>();
        var previous = accessor.HttpContext;
        accessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())], "test-actor")) };
        try { return await action(services); }
        finally { accessor.HttpContext = previous; }
    }

    public Task<long> BookAsync(string patient, long offering, int hour, int minute = 0) => AsAsync(patient,
        s => s.GetRequiredService<AppointmentService>().BookAsync(new(offering, Date, new(hour, minute), Version), Token));
    public Task<bool> DecideAsync(string actor, long id, AppointmentStatus status) => AsAsync(actor, async s =>
    { await s.GetRequiredService<AppointmentService>().DecideAsync(id, status, Token); return true; });
    public Task<AppointmentView> DetailAsync(string actor, long id) => AsAsync(actor,
        s => s.GetRequiredService<AppointmentService>().DetailAsync(id, Token));
    public void Dispose() => Factory.Dispose();
}
