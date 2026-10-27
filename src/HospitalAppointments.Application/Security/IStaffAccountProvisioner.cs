namespace HospitalAppointments.Application.Security;

public interface IStaffAccountProvisioner
{
    Task<long> CreateAsync(string username, string displayName, string password, CancellationToken token);
}
