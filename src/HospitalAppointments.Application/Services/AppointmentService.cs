using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Transactions;
using HospitalAppointments.Domain.Accounts;
using HospitalAppointments.Domain.Appointments;

namespace HospitalAppointments.Application.Services;

public sealed class AppointmentService(IBookingCatalogRepository catalog, IAppointmentRepository appointments,
    BookingAccessService access, AvailabilityService availability, IWriteTransaction transactions, ClinicTime time)
{
    public async Task<long> BookAsync(BookAppointmentCommand command, CancellationToken token = default)
    {
        await access.PatientAsync(token); // Cheap untracked identity/profile preflight.
        await using var transaction = await transactions.BeginAsync(token);
        var patient = await access.PatientAsync(token);
        var offering = await catalog.FindAsync(command.OfferingId, token) ?? throw new BookingNotFoundException();
        if (offering.Service.Version != command.CatalogVersion)
            throw new BookingException("Hizmet koşulları değişti. Güncel süre ve ücreti inceleyip yeniden seçim yapın.");
        var interval = (await availability.LoadAsync(offering, patient, command.Date, token)).Require(command.StartTime);
        var appointment = new Appointment(patient, offering, interval, time.UtcNow);
        appointments.Add(appointment);
        await transaction.SaveAsync(token);
        await transaction.CommitAsync(token);
        return appointment.Id;
    }

    public async Task DecideAsync(long id, AppointmentStatus decision, CancellationToken token = default)
    {
        if (decision is not (AppointmentStatus.Approved or AppointmentStatus.Rejected)) throw new BookingException("Onay veya ret seçin.");
        var actor = await access.ActorAsync(token);
        var preflight = await appointments.FindAsync(id, false, token) ?? throw new BookingNotFoundException();
        if (!await access.CanDecideAsync(actor, preflight, token)) throw new AccountAccessDeniedException();
        await using var transaction = await transactions.BeginAsync(token);
        actor = await access.ActorAsync(token);
        var appointment = await appointments.FindAsync(id, true, token) ?? throw new BookingNotFoundException();
        if (!await access.CanDecideAsync(actor, appointment, token)) throw new AccountAccessDeniedException();
        if (appointment.Status != AppointmentStatus.Pending) throw new BookingException("Bu randevu hakkında daha önce karar verilmiş.");
        if (decision == AppointmentStatus.Approved) appointment.Approve(actor.Id, time.UtcNow);
        else appointment.Reject(actor.Id, time.UtcNow);
        await transaction.SaveAsync(token);
        await transaction.CommitAsync(token);
    }

    public async Task<AppointmentView> DetailAsync(long id, CancellationToken token = default)
    {
        var actor = await access.ActorAsync(token);
        var appointment = await appointments.FindAsync(id, false, token) ?? throw new BookingNotFoundException();
        await access.RequireViewAsync(actor, appointment, token);
        return await ViewAsync(appointment, actor, token);
    }

    public async Task<IReadOnlyList<AppointmentView>> ListAsync(bool patientList, CancellationToken token = default)
    {
        var actor = await access.ActorAsync(token);
        if (patientList ? !actor.Roles.Contains(AccountRole.Patient) : !actor.Roles.Overlaps([AccountRole.Staff, AccountRole.Manager]))
            throw new AccountAccessDeniedException();
        var rows = await appointments.ForAccountAsync(actor.Id, patientList, !patientList && actor.Roles.Contains(AccountRole.Staff),
            !patientList && actor.Roles.Contains(AccountRole.Manager), token);
        var result = new List<AppointmentView>();
        foreach (var row in rows) result.Add(await ViewAsync(row, actor, token));
        return result;
    }

    private async Task<AppointmentView> ViewAsync(Appointment a, AccountProfile actor, CancellationToken token) => new(
        a.Id, a.Patient.Name, a.BookedServiceName, a.BookedStaffName, a.Offering.Staff.ProfessionalDescription,
        a.Offering.Branch.Facility.Name, a.Offering.Branch.Name, time.Local(a.Start), time.Local(a.End),
        a.BookedDurationMinutes, a.BookedFee, a.BookedCurrency, a.Status, a.DecidedByAccountId,
        a.DecidedAt is null ? null : time.Local(a.DecidedAt.Value), a.Status == AppointmentStatus.Pending && await access.CanDecideAsync(actor, a, token));
}
