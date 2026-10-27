using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Domain.Accounts;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointments.Infrastructure.Repositories;

public sealed class AccountRepository(ApplicationDbContext database) : IAccountRepository
{
    public async Task<AccountProfile?> FindEnabledAsync(long accountId, CancellationToken cancellationToken = default)
    {
        var user = await database.Users.AsNoTracking()
            .Where(u => u.Id == accountId && u.IsEnabled)
            .Select(u => new { u.Id, u.DisplayName }).SingleOrDefaultAsync(cancellationToken);
        if (user is null) return null;

        var names = await (from userRole in database.UserRoles
                           join role in database.Roles on userRole.RoleId equals role.Id
                           where userRole.UserId == accountId
                           select role.Name).ToListAsync(cancellationToken);
        var roles = Enum.GetValues<AccountRole>().Where(role => names.Contains(RoleNames.For(role))).ToArray();
        return roles.Length == 0 ? null : new AccountProfile(user.Id, user.DisplayName, roles);
    }
}
