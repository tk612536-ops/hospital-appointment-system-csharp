using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Domain.Accounts;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Domain.Patients;

namespace HospitalAppointments.Application.Services;

public sealed class BookingAccessService(AccountService accounts, IBookingAccessRepository access)
{
    public Task<AccountProfile> ActorAsync(CancellationToken token) => accounts.GetProfileAsync(token);
    public async Task<Patient> PatientAsync(CancellationToken token)
    {
        var actor = await accounts.GetWorkspaceAsync(AccountRole.Patient, token);
        return await access.PatientAsync(actor.Id, token) ?? throw new BookingException("Randevu alabilmek için bir hasta profili gereklidir.");
    }
    public async Task<bool> CanDecideAsync(AccountProfile actor, Appointment appointment, CancellationToken token) =>
        actor.Roles.Contains(AccountRole.Staff) && appointment.Offering.Staff.AccountId == actor.Id ||
        actor.Roles.Contains(AccountRole.Manager) && await access.ManagesAsync(actor.Id, appointment.Offering.Branch.FacilityId, token);
    public async Task RequireViewAsync(AccountProfile actor, Appointment appointment, CancellationToken token)
    {
        if (!(actor.Roles.Contains(AccountRole.Patient) && appointment.Patient.AccountId == actor.Id) && !await CanDecideAsync(actor, appointment, token))
            throw new AccountAccessDeniedException();
    }
}
