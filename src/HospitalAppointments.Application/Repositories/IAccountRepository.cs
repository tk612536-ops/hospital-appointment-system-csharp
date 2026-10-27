using HospitalAppointments.Domain.Accounts;

namespace HospitalAppointments.Application.Repositories;

public interface IAccountRepository
{
    Task<AccountProfile?> FindEnabledAsync(long accountId, CancellationToken cancellationToken = default);
}
