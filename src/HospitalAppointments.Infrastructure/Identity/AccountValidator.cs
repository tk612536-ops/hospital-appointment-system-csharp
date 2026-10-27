using Microsoft.AspNetCore.Identity;

namespace HospitalAppointments.Infrastructure.Identity;

public sealed class AccountValidator : IUserValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user)
    {
        var valid = !string.IsNullOrWhiteSpace(user.UserName) && user.UserName.Length <= 80 &&
            manager.NormalizeName(user.UserName).Length <= 80 &&
            !string.IsNullOrWhiteSpace(user.DisplayName) && user.DisplayName.Length <= 120;
        return Task.FromResult(valid ? IdentityResult.Success : IdentityResult.Failed(
            new IdentityError { Code = "InvalidAccountDetails", Description = "Account details are invalid." }));
    }
}
