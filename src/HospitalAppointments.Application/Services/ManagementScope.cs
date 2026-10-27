using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Transactions;
using HospitalAppointments.Domain.Accounts;

namespace HospitalAppointments.Application.Services;

/// <summary>Shared manager grant checks and the existing writer protocol; not a CRUD framework.</summary>
public sealed class ManagementScope(AccountService accounts, IBookingAccessRepository grants, IWriteTransaction writes)
{
    public Task<AccountProfile> RequireManagerAsync(CancellationToken token) => accounts.GetWorkspaceAsync(AccountRole.Manager, token);

    public async Task RequireFacilityAsync(long facilityId, CancellationToken token)
    {
        var manager = await RequireManagerAsync(token);
        if (!await grants.ManagesAsync(manager.Id, facilityId, token)) throw new AccountAccessDeniedException();
    }

    public async Task<IWriteScope> BeginAsync(long? facilityId, CancellationToken token)
    {
        if (facilityId is null) await RequireManagerAsync(token); else await RequireFacilityAsync(facilityId.Value, token);
        var transaction = await writes.BeginAsync(token);
        try
        {
            if (facilityId is null) await RequireManagerAsync(token); else await RequireFacilityAsync(facilityId.Value, token);
            return transaction;
        }
        catch { await transaction.DisposeAsync(); throw; }
    }

    public static void RequireOwner(long expectedFacility, long actualFacility)
    {
        if (expectedFacility != actualFacility) throw new AccountAccessDeniedException();
    }
}
