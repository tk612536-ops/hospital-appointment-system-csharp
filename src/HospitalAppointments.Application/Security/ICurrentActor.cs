namespace HospitalAppointments.Application.Security;

public interface ICurrentActor
{
    long? AccountId { get; }
}
