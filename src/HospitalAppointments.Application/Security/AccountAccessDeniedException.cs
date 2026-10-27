namespace HospitalAppointments.Application.Security;

public sealed class AccountAccessDeniedException : Exception
{
    public AccountAccessDeniedException() : base("The current account cannot access this workspace.") { }
}
