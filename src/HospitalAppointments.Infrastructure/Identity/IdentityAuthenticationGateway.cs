using HospitalAppointments.Application.Security;
using Microsoft.AspNetCore.Identity;

namespace HospitalAppointments.Infrastructure.Identity;

public sealed class IdentityAuthenticationGateway(
    UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) : IAuthenticationGateway
{
    public async Task<bool> SignInAsync(string username, string password)
    {
        var user = await users.FindByNameAsync(username);
        if (user is null || !user.IsEnabled || (await users.GetRolesAsync(user)).Count == 0)
            return false;
        var result = await signIn.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: true);
        return result.Succeeded;
    }

    public Task SignOutAsync() => signIn.SignOutAsync();
}
