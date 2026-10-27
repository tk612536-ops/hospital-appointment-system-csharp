using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Domain.Accounts;

namespace HospitalAppointments.Application.Tests;

public sealed class AccountServiceTests
{
    private sealed class Accounts : IAccountRepository
    {
        public AccountProfile? Profile { get; set; } = new(7, "Demo", [AccountRole.Patient]);
        public long? RequestedId { get; private set; }
        public Task<AccountProfile?> FindEnabledAsync(long id, CancellationToken cancellationToken = default)
        {
            RequestedId = id;
            return Task.FromResult(Profile);
        }
    }

    private sealed class Authentication : IAuthenticationGateway
    {
        public string? Username { get; private set; }
        public bool SignedOut { get; private set; }
        public Task<bool> SignInAsync(string username, string password)
        {
            Username = username;
            return Task.FromResult(true);
        }
        public Task SignOutAsync() { SignedOut = true; return Task.CompletedTask; }
    }

    private sealed class Actor(long? id) : ICurrentActor { public long? AccountId => id; }

    [Fact]
    public async Task LoginTrimsIdentityAndDelegatesPasswordChecking()
    {
        var authentication = new Authentication();
        Assert.True(await new AccountService(new Accounts(), authentication, new Actor(null)).SignInAsync(" PATIENT ", "any-password"));
        Assert.Equal("PATIENT", authentication.Username);
    }

    [Theory]
    [InlineData(null, "password")]
    [InlineData("", "password")]
    [InlineData("  ", "password")]
    [InlineData("\t", "password")]
    [InlineData("patient", null)]
    [InlineData("patient", "")]
    public async Task InvalidLoginShapeNeverReachesIdentity(string? username, string? password)
    {
        var authentication = new Authentication();
        Assert.False(await new AccountService(new Accounts(), authentication, new Actor(null)).SignInAsync(username, password));
        Assert.Null(authentication.Username);
    }

    [Fact]
    public async Task OversizedLoginFieldsAreRejectedBeforeFrameworkAccess()
    {
        var authentication = new Authentication();
        var service = new AccountService(new Accounts(), authentication, new Actor(null));
        Assert.False(await service.SignInAsync(new string('x', 81), "password"));
        Assert.False(await service.SignInAsync("patient", new string('x', 129)));
        Assert.Null(authentication.Username);
    }

    [Theory]
    [InlineData(AccountRole.Staff)]
    [InlineData(AccountRole.Manager)]
    public async Task DirectServiceCallsEnforceTheRequiredRole(AccountRole requiredRole) =>
        await Assert.ThrowsAsync<AccountAccessDeniedException>(() =>
            new AccountService(new Accounts(), new Authentication(), new Actor(7)).GetWorkspaceAsync(requiredRole, TestContext.Current.CancellationToken));

    [Fact]
    public async Task AccountIdentityComesFromTheCurrentActor()
    {
        var accounts = new Accounts();
        var profile = await new AccountService(accounts, new Authentication(), new Actor(7)).GetWorkspaceAsync(AccountRole.Patient, TestContext.Current.CancellationToken);
        Assert.Equal(7, accounts.RequestedId);
        Assert.Equal(7, profile.Id);
    }

    [Fact]
    public async Task MissingAndDisabledAccountsAreDenied()
    {
        var accounts = new Accounts { Profile = null };
        await Assert.ThrowsAsync<AccountAccessDeniedException>(() => new AccountService(accounts, new Authentication(), new Actor(7)).GetProfileAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<AccountAccessDeniedException>(() => new AccountService(accounts, new Authentication(), new Actor(null)).GetProfileAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LogoutUsesTheAuthenticationAdapter()
    {
        var authentication = new Authentication();
        await new AccountService(new Accounts(), authentication, new Actor(7)).SignOutAsync();
        Assert.True(authentication.SignedOut);
    }
}
