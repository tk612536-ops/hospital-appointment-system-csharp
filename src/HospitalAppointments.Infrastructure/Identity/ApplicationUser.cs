using Microsoft.AspNetCore.Identity;

namespace HospitalAppointments.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<long>
{
    public string DisplayName { get; private set; } = string.Empty;
    public bool IsEnabled { get; private set; } = true;

    private ApplicationUser() { }

    public ApplicationUser(string username, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (username.Trim().Length > 80 || displayName.Trim().Length > 120)
            throw new ArgumentException("Account details exceed their maximum length.");
        UserName = username.Trim();
        DisplayName = displayName.Trim();
    }

    public void Disable()
    {
        IsEnabled = false;
        // Invalidates existing cookies on their next security-stamp validation.
        SecurityStamp = Guid.NewGuid().ToString();
    }
}
