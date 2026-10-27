namespace HospitalAppointments.Application.Security;

public interface IAuthenticationGateway
{
    Task<bool> SignInAsync(string username, string password);
    Task SignOutAsync();
}
