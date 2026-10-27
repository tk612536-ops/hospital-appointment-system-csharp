using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Application.Transactions;
using HospitalAppointments.Domain.Accounts;
using HospitalAppointments.Domain.Patients;

namespace HospitalAppointments.Application.Tests;

public sealed class ManagementScopeTests
{
    private sealed class Accounts(AccountRole role) : IAccountRepository
    {
        public bool Enabled { get; set; } = true;
        public Task<AccountProfile?> FindEnabledAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult<AccountProfile?>(Enabled ? new(id, "Synthetic", [role]) : null);
    }
    private sealed class Actor : ICurrentActor { public long? AccountId => 1; }
    private sealed class Authentication : IAuthenticationGateway
    {
        public Task<bool> SignInAsync(string username, string password) => throw new NotSupportedException();
        public Task SignOutAsync() => throw new NotSupportedException();
    }
    private sealed class Grants : IBookingAccessRepository
    {
        public bool Allowed { get; set; } = true;
        public Task<bool> ManagesAsync(long accountId, long facilityId, CancellationToken token) => Task.FromResult(Allowed);
        public Task<Patient?> PatientAsync(long accountId, CancellationToken token) => throw new NotSupportedException();
    }
    private sealed class Writes : IWriteTransaction, IWriteScope
    {
        public bool Began { get; private set; }
        public bool Disposed { get; private set; }
        public Action? Acquired { get; set; }
        public Task<IWriteScope> BeginAsync(CancellationToken cancellationToken = default) { Began = true; Acquired?.Invoke(); return Task.FromResult<IWriteScope>(this); }
        public Task SaveAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CommitAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
    }

    [Theory]
    [InlineData(AccountRole.Patient, true)]
    [InlineData(AccountRole.Staff, true)]
    [InlineData(AccountRole.Manager, false)]
    public async Task UnauthorizedCommandsDoNotAcquireTheDatabaseWriter(AccountRole role, bool grant)
    {
        var writes = new Writes(); var scope = new ManagementScope(new(new Accounts(role), new Authentication(), new Actor()), new Grants { Allowed = grant }, writes);
        await Assert.ThrowsAsync<AccountAccessDeniedException>(() => scope.BeginAsync(12, TestContext.Current.CancellationToken)); Assert.False(writes.Began);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AccountOrGrantRevocationWhileWaitingForWriterIsRecheckedAndDisposed(bool revokeGrant)
    {
        var accounts = new Accounts(AccountRole.Manager); var grants = new Grants();
        var writes = new Writes { Acquired = () => { if (revokeGrant) grants.Allowed = false; else accounts.Enabled = false; } };
        var scope = new ManagementScope(new(accounts, new Authentication(), new Actor()), grants, writes);
        await Assert.ThrowsAsync<AccountAccessDeniedException>(() => scope.BeginAsync(12, TestContext.Current.CancellationToken)); Assert.True(writes.Began); Assert.True(writes.Disposed);
    }

    [Fact]
    public async Task ManagerCanCreateFirstFacilityWithoutAPreexistingGrant()
    {
        var writes = new Writes(); var scope = new ManagementScope(new(new Accounts(AccountRole.Manager), new Authentication(), new Actor()), new Grants { Allowed = false }, writes);
        await using (await scope.BeginAsync(null, TestContext.Current.CancellationToken)) { Assert.True(writes.Began); }
        Assert.True(writes.Disposed);
    }
}
