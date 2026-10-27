using HospitalAppointments.Application.Security;
using HospitalAppointments.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace HospitalAppointments.Infrastructure.Demo;

/// <summary>Registered only in the explicitly enabled Demo environment.</summary>
public sealed class DemoAccountSeeder(UserManager<ApplicationUser> users)
{
    // Published synthetic local-demo credentials; never use them for a real deployment.
    public const string Password = "KlinikDemo10!";

    public async Task SeedAsync()
    {
        await AddIfMissingAsync("patient", "Demo Hasta", RoleNames.Patient);
        await AddIfMissingAsync("staff", "Demo Sağlık Çalışanı", RoleNames.Staff);
        await AddIfMissingAsync("manager", "Demo Yönetici", RoleNames.Manager);
    }

    private async Task AddIfMissingAsync(string username, string displayName, string role)
    {
        if (await users.FindByNameAsync(username) is not null) return;
        var user = new ApplicationUser(username, displayName);
        RequireSuccess(await users.CreateAsync(user, Password));
        RequireSuccess(await users.AddToRoleAsync(user, role));
    }

    internal static void RequireSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException("Identity initialization failed: " +
                string.Join(", ", result.Errors.Select(error => error.Code)));
    }
}
