using HospitalAppointments.Domain.Accounts;

namespace HospitalAppointments.Application.Security;

public static class RoleNames
{
    public const string Patient = "PATIENT";
    public const string Staff = "STAFF";
    public const string Manager = "MANAGER";

    public static string For(AccountRole role) => role switch
    {
        AccountRole.Patient => Patient,
        AccountRole.Staff => Staff,
        AccountRole.Manager => Manager,
        _ => throw new ArgumentOutOfRangeException(nameof(role))
    };
}
