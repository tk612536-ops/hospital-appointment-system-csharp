using System.Collections.Concurrent;
using System.Diagnostics;
using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Application.Transactions;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Domain.Staff;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalAppointments.IntegrationTests;

public sealed class BookingConcurrencyTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static async Task<bool[]> CompeteAsync(BookingScenario scenario, string firstActor, Func<IServiceProvider, Task> first,
        string secondActor, Func<IServiceProvider, Task> second)
    {
        using var start = new Barrier(2);
        var connections = new ConcurrentBag<System.Data.Common.DbConnection>();
        Task<bool> Launch(string actor, Func<IServiceProvider, Task> action) => Task.Run(() => scenario.AsAsync(actor, async services =>
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            connections.Add(db.Database.GetDbConnection());
            if (!start.SignalAndWait(TimeSpan.FromSeconds(10), Token)) throw new TimeoutException("Race start timed out.");
            try { await action(services); return true; }
            catch (BookingException) { return false; }
        }), Token);
        var results = await Task.WhenAll(Launch(firstActor, first), Launch(secondActor, second)).WaitAsync(TimeSpan.FromSeconds(20), Token);
        Assert.Equal(2, connections.Count); Assert.NotSame(connections.First(), connections.Last());
        Assert.All(connections, connection => Assert.Equal(scenario.Factory.DatabasePath, connection.DataSource));
        return results;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConflictingIndependentBookingsHaveExactlyOneWinner(bool samePatientDifferentStaff)
    {
        using var s = await BookingScenario.CreateAsync();
        var outcomes = await CompeteAsync(s, "patient", async p =>
            await p.GetRequiredService<AppointmentService>().BookAsync(new(s.North, s.Date, new(10, 0), s.Version), Token),
            samePatientDifferentStaff ? "patient" : "patient2", async p =>
            await p.GetRequiredService<AppointmentService>().BookAsync(new(samePatientDifferentStaff ? s.Lab : s.North, s.Date, new(10, 15), s.Version), Token));
        Assert.Single(outcomes, success => success);
        using var scope = s.Factory.Services.CreateScope();
        Assert.Single(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<Appointment>().ToListAsync(Token));
    }

    [Fact]
    public async Task SameStaffAcrossBranchesIsOneGlobalResource()
    {
        using var s = await BookingScenario.CreateAsync();
        using (var scope = s.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var south = (await scope.ServiceProvider.GetRequiredService<IBookingCatalogRepository>().FindAsync(s.South, Token))!;
            // Defensive imported inconsistency; normal future configuration must reject overlapping branch shifts.
            db.Add(new AvailabilityWindow(south.Staff, south.Branch, scope.ServiceProvider.GetRequiredService<ClinicTime>().Interval(s.Date, new(9, 0), new(11, 0))));
            await db.SaveChangesAsync(Token);
        }
        var results = await CompeteAsync(s, "patient", async p => await p.GetRequiredService<AppointmentService>().BookAsync(new(s.North, s.Date, new(10, 0), s.Version), Token),
            "patient2", async p => await p.GetRequiredService<AppointmentService>().BookAsync(new(s.South, s.Date, new(10, 15), s.Version), Token));
        Assert.Single(results, success => success);
        using var verification = s.Factory.Services.CreateScope();
        Assert.Single(await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<Appointment>().ToListAsync(Token));
    }

    [Fact]
    public async Task ConcurrentAdjacentBookingsBothCommit()
    {
        using var s = await BookingScenario.CreateAsync();
        var results = await CompeteAsync(s, "patient", async p => await p.GetRequiredService<AppointmentService>().BookAsync(new(s.North, s.Date, new(10, 0), s.Version), Token),
            "patient2", async p => await p.GetRequiredService<AppointmentService>().BookAsync(new(s.North, s.Date, new(10, 30), s.Version), Token));
        Assert.All(results, Assert.True);
        using var scope = s.Factory.Services.CreateScope();
        var appointments = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<Appointment>().OrderBy(a => a.Start).ToListAsync(Token);
        Assert.Equal(2, appointments.Count); Assert.Equal(appointments[0].End, appointments[1].Start);
    }

    [Fact]
    public async Task CompetingApprovalAndRejectionTransitionExactlyOnce()
    {
        using var s = await BookingScenario.CreateAsync(); var id = await s.BookAsync("patient", s.North, 10);
        var results = await CompeteAsync(s, "staff", p => p.GetRequiredService<AppointmentService>().DecideAsync(id, AppointmentStatus.Approved, Token),
            "manager", p => p.GetRequiredService<AppointmentService>().DecideAsync(id, AppointmentStatus.Rejected, Token));
        Assert.Single(results, success => success);
        var result = await s.DetailAsync("patient", id);
        Assert.NotEqual(AppointmentStatus.Pending, result.Status); Assert.NotNull(result.DecidedAt); Assert.NotNull(result.DecidedByAccountId);
    }

    [Fact]
    public async Task ImmediateScopeReservesWriterBeforeAnyDomainReadAndEnlistsEf()
    {
        using var s = await BookingScenario.CreateAsync();
        using var scope = s.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var writer = await scope.ServiceProvider.GetRequiredService<IWriteTransaction>().BeginAsync(Token);
        Assert.IsType<SqliteTransaction>(db.Database.CurrentTransaction!.GetDbTransaction());
        Assert.Same(db.Database.GetDbConnection(), db.Database.CurrentTransaction!.GetDbTransaction().Connection);
        await using var competing = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = s.Factory.DatabasePath, DefaultTimeout = 1, ForeignKeys = true }.ToString());
        await competing.OpenAsync(Token);
        var error = Assert.Throws<SqliteException>(() => competing.BeginTransaction(deferred: false));
        Assert.Equal(5, error.SqliteErrorCode);
    }

    [Fact]
    public async Task UncommittedSavedReservationRollsBackAndFreshBookingSucceeds()
    {
        using var s = await BookingScenario.CreateAsync();
        using (var scope = s.Factory.Services.CreateScope())
        {
            var p = scope.ServiceProvider;
            await using var writer = await p.GetRequiredService<IWriteTransaction>().BeginAsync(Token);
            var account = await p.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Infrastructure.Identity.ApplicationUser>>().FindByNameAsync("patient");
            var patient = (await p.GetRequiredService<IBookingAccessRepository>().PatientAsync(account!.Id, Token))!;
            var o = (await p.GetRequiredService<IBookingCatalogRepository>().FindAsync(s.North, Token))!;
            var interval = p.GetRequiredService<ClinicTime>().Interval(s.Date, new(10, 0), new(10, 30));
            p.GetRequiredService<IAppointmentRepository>().Add(new Appointment(patient, o, interval, p.GetRequiredService<ClinicTime>().UtcNow));
            await writer.SaveAsync(Token);
            // Dispose without commit, simulating a failure after INSERT.
        }
        using (var scope = s.Factory.Services.CreateScope())
            Assert.Empty(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<Appointment>().ToListAsync(Token));
        Assert.True(await s.BookAsync("patient", s.North, 10) > 0);
    }

    [Fact]
    public async Task BusyTimeoutIsBoundedLeavesNoReservationAndAllowsFreshRetry()
    {
        using var s = await BookingScenario.CreateAsync();
        await using var holder = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = s.Factory.DatabasePath, ForeignKeys = true }.ToString());
        await holder.OpenAsync(Token);
        var held = holder.BeginTransaction(deferred: false);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await Assert.ThrowsAsync<DatabaseBusyException>(() => s.AsAsync("patient", async p =>
            {
                ((SqliteConnection)p.GetRequiredService<ApplicationDbContext>().Database.GetDbConnection()).DefaultTimeout = 1;
                return await p.GetRequiredService<AppointmentService>().BookAsync(new(s.North, s.Date, new(10, 0), s.Version), Token);
            }));
            Assert.InRange(stopwatch.Elapsed, TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(8));
        }
        finally { await held.DisposeAsync(); }
        using (var scope = s.Factory.Services.CreateScope())
            Assert.Empty(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<Appointment>().ToListAsync(Token));
        Assert.True(await s.BookAsync("patient", s.North, 10) > 0);
    }

    [Fact]
    public async Task AlreadyTrackedCatalogIsRefreshedAfterWriterAcquisition()
    {
        using var s = await BookingScenario.CreateAsync();
        await s.AsAsync("patient", async p =>
        {
            var previous = await p.GetRequiredService<IBookingCatalogRepository>().FindAsync(s.North, Token);
            using (var other = s.Factory.Services.CreateScope())
            {
                await using var writer = await other.ServiceProvider.GetRequiredService<IWriteTransaction>().BeginAsync(Token);
                var fresh = (await other.ServiceProvider.GetRequiredService<IBookingCatalogRepository>().FindAsync(s.North, Token))!;
                fresh.Service.ReviseTerms(45, 999m); await writer.SaveAsync(Token); await writer.CommitAsync(Token);
            }
            Assert.Equal(s.Version, previous!.Service.Version);
            await Assert.ThrowsAsync<BookingException>(() => p.GetRequiredService<AppointmentService>().BookAsync(new(s.North, s.Date, new(10, 0), s.Version), Token));
            return true;
        });
    }
}
