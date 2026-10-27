using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Domain.Accounts;

namespace HospitalAppointments.Application.Services;

public sealed class AccountService(
    IAccountRepository accounts, IAuthenticationGateway authentication, ICurrentActor actor)
{
    public Task<bool> SignInAsync(string? username, string? password)
    {
        // Bound both raw and normalized identity before handing it to the framework.
        if (string.IsNullOrWhiteSpace(username) || username.Length > 80 ||
            username.Trim().ToUpperInvariant().Length > 80 ||
            string.IsNullOrEmpty(password) || password.Length > 128)
            return Task.FromResult(false);

        return authentication.SignInAsync(username.Trim(), password);
    }

    public Task SignOutAsync() => authentication.SignOutAsync();

    public async Task<AccountProfile> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        if (actor.AccountId is not > 0)
            throw new AccountAccessDeniedException();
        return await accounts.FindEnabledAsync(actor.AccountId.Value, cancellationToken)
            ?? throw new AccountAccessDeniedException();
    }

    public async Task<AccountProfile> GetWorkspaceAsync(AccountRole requiredRole, CancellationToken cancellationToken = default)
    {
        var profile = await GetProfileAsync(cancellationToken);
        if (!profile.Roles.Contains(requiredRole))
            throw new AccountAccessDeniedException();
        return profile;
    }
}
