using System.Collections.Frozen;

namespace HospitalAppointments.Domain.Accounts;

/// <summary>A credential-free, immutable account snapshot; not a second identity store.</summary>
public sealed class AccountProfile
{
    public long Id { get; }
    public string DisplayName { get; }
    public IReadOnlySet<AccountRole> Roles { get; }

    public AccountProfile(long id, string displayName, IEnumerable<AccountRole> roles)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(roles);
        var roleSet = roles.ToFrozenSet();
        if (displayName.Trim().Length > 120 || roleSet.Count == 0 || roleSet.Any(r => !Enum.IsDefined(r)))
            throw new ArgumentException("A valid display name and at least one recognized role are required.");

        Id = id;
        DisplayName = displayName.Trim();
        Roles = roleSet;
    }
}
