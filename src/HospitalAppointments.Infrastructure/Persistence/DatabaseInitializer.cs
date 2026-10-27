using HospitalAppointments.Application.Security;
using HospitalAppointments.Domain.Accounts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointments.Infrastructure.Persistence;

public sealed class DatabaseInitializer(ApplicationDbContext database, RoleManager<IdentityRole<long>> roles)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await database.Database.MigrateAsync(cancellationToken);
        await database.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
        // This protects bootstrap/account writes only; booking's explicit writer protocol arrives in Stage 2.
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        foreach (var role in Enum.GetValues<AccountRole>().Select(RoleNames.For))
            if (!await roles.RoleExistsAsync(role))
                Demo.DemoAccountSeeder.RequireSuccess(await roles.CreateAsync(new IdentityRole<long>(role)));
        await transaction.CommitAsync(cancellationToken);
    }
}
