using System.Data;
using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Application.Transactions;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalAppointments.IntegrationTests;

public sealed class AppointmentRollbackTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task BookingFailureAfterInsertRollsBackAndReleasesBothPatientAndEmployee()
    {
        using var s = await BookingScenario.CreateAsync();
        await s.AsAsync("patient", async services =>
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var writes = new FailBeforeCommit(services.GetRequiredService<IWriteTransaction>(), async () =>
            {
                // Query SQLite, not the tracked entity: the INSERT really ran before the failure.
                var inserted = await db.Set<Appointment>().AsNoTracking().SingleAsync(Token);
                Assert.Equal(AppointmentStatus.Pending, inserted.Status);
                Assert.Equal(s.North, inserted.OfferingId);
            });
            var service = ActivatorUtilities.CreateInstance<AppointmentService>(services, writes);
            var failure = await Assert.ThrowsAsync<IOException>(() => service.BookAsync(
                new(s.North, s.Date, new(10, 0), s.Version), Token));
            Assert.Same(writes.Failure, failure);
            AssertClean(db);
            return true;
        });

        using (var verification = s.Factory.Services.CreateScope())
            Assert.Empty(await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .Set<Appointment>().AsNoTracking().ToListAsync(Token));

        // Different employees at the same time: neither participant retains a phantom reservation.
        var employeeRetry = await s.BookAsync("patient2", s.North, 10);
        var patientRetry = await s.BookAsync("patient", s.Lab, 10);
        using var finalScope = s.Factory.Services.CreateScope();
        var saved = await finalScope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
            .Set<Appointment>().AsNoTracking().ToListAsync(Token);
        Assert.Equal(2, saved.Count);
        Assert.Contains(saved, row => row.Id == employeeRetry && row.OfferingId == s.North);
        Assert.Contains(saved, row => row.Id == patientRetry && row.OfferingId == s.Lab);
    }

    [Theory]
    [InlineData(AppointmentStatus.Approved)]
    [InlineData(AppointmentStatus.Rejected)]
    public async Task DecisionFailureAfterUpdateRestoresPendingAuditAndReservation(AppointmentStatus decision)
    {
        using var s = await BookingScenario.CreateAsync();
        var id = await s.BookAsync("patient", s.North, 10);
        var before = await s.DetailAsync("patient", id);
        await s.AsAsync("manager", async services =>
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var writes = new FailBeforeCommit(services.GetRequiredService<IWriteTransaction>(), async () =>
            {
                var updated = await db.Set<Appointment>().AsNoTracking().SingleAsync(Token);
                Assert.Equal(decision, updated.Status);
                Assert.Equal(2, updated.Version);
                Assert.NotNull(updated.DecidedByAccountId);
                Assert.NotNull(updated.DecidedAt);
            });
            var service = ActivatorUtilities.CreateInstance<AppointmentService>(services, writes);
            var failure = await Assert.ThrowsAsync<IOException>(() => service.DecideAsync(id, decision, Token));
            Assert.Same(writes.Failure, failure);
            AssertClean(db);
            return true;
        });

        Assert.Equal(before, await s.DetailAsync("patient", id));
        using (var verification = s.Factory.Services.CreateScope())
        {
            var restored = await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .Set<Appointment>().AsNoTracking().SingleAsync(Token);
            Assert.Equal(1, restored.Version);
            Assert.Null(restored.DecidedByAccountId);
            Assert.Null(restored.DecidedAt);
        }
        await Assert.ThrowsAsync<BookingException>(
            () => s.BookAsync("patient2", s.North, 10));

        // A fresh authorized request can still decide exactly once, with its own audit actor.
        await s.DecideAsync("staff", id, decision);
        var result = await s.DetailAsync("patient", id);
        Assert.Equal(decision, result.Status);
        Assert.NotNull(result.DecidedAt);
        await s.AsAsync("staff", async services =>
        {
            var actor = await services.GetRequiredService<BookingAccessService>().ActorAsync(Token);
            Assert.Equal(actor.Id, result.DecidedByAccountId);
            var saved = await services.GetRequiredService<ApplicationDbContext>()
                .Set<Appointment>().AsNoTracking().SingleAsync(Token);
            Assert.Equal(2, saved.Version);
            return true;
        });
    }

    private static void AssertClean(ApplicationDbContext db)
    {
        Assert.Null(db.Database.CurrentTransaction);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Equal(ConnectionState.Closed, db.Database.GetDbConnection().State);
    }

    // Only the pre-commit boundary is faulted. BEGIN IMMEDIATE, EF writes and rollback stay real.
    private sealed class FailBeforeCommit(IWriteTransaction inner, Func<Task> verifySaved) : IWriteTransaction
    {
        public IOException Failure { get; } = new("Synthetic failure after SQLite save, before commit.");

        public async Task<IWriteScope> BeginAsync(CancellationToken cancellationToken = default) =>
            new Scope(await inner.BeginAsync(cancellationToken), verifySaved, Failure);

        private sealed class Scope(IWriteScope inner, Func<Task> verifySaved, IOException failure) : IWriteScope
        {
            public Task SaveAsync(CancellationToken cancellationToken = default) => inner.SaveAsync(cancellationToken);
            public async Task CommitAsync(CancellationToken cancellationToken = default)
            {
                await verifySaved();
                throw failure;
            }
            public ValueTask DisposeAsync() => inner.DisposeAsync();
        }
    }
}
