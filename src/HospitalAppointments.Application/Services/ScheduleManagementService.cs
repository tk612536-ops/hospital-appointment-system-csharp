using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Scheduling;
using HospitalAppointments.Domain.Staff;

namespace HospitalAppointments.Application.Services;

public sealed class ScheduleManagementService(ManagementScope scope, IFacilityManagementRepository facilities,
    IStaffManagementRepository staff, IScheduleManagementRepository schedules, IScheduleRepository opening, ClinicTime time)
{
    public async Task ReplaceOpeningHoursAsync(long facilityId, long branchId, DayOfWeek day, IReadOnlyList<OpeningPeriod> periods, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var branch = await facilities.BranchAsync(branchId, token) ?? throw new BookingNotFoundException();
        ManagementScope.RequireOwner(facilityId, branch.FacilityId);
        if (!Enum.IsDefined(day) || periods is null) throw new ConfigurationException("Geçerli bir gün ve çalışma aralığı girin.");
        var replacements = new List<OpeningWindow>();
        foreach (var period in periods)
        {
            if (period.Opens is null && period.Closes is null) continue;
            if (period.Opens is null || period.Closes is null) throw new ConfigurationException("Her aralığın hem açılış hem kapanış saatini girin.");
            replacements.Add(new OpeningWindow(branch, day, period.Opens.Value, period.Closes.Value));
        }
        var sorted = replacements.OrderBy(w => w.OpensAt).ToArray();
        for (var i = 1; i < sorted.Length; i++)
            if (sorted[i].OpensAt < sorted[i - 1].ClosesAt) throw new ConfigurationException("Çalışma aralıkları çakışamaz; bitişik aralıklar kullanılabilir.");
        var shifts = await schedules.FutureShiftsAsync(branchId, time.UtcNow, token);
        foreach (var shift in shifts.Where(w => time.Local(w.Start).DayOfWeek == day))
            RequireCoverage(shift.Interval, replacements, "Bu saatler mevcut bir çalışan uygunluğunu geçersiz kılıyor. Önce uygunluk kaydını düzenleyin.");
        foreach (var appointment in await schedules.ReservationsAsync(branchId, time.UtcNow, token))
            if (time.Local(appointment.Start).DayOfWeek == day)
                RequireCoverage(appointment.Interval, replacements, "Bu saatler ayrılmış bir randevuyu geçersiz kılıyor.");
        await schedules.ReplaceHoursAsync(branchId, day, replacements, token);
        await transaction.SaveAsync(token); await transaction.CommitAsync(token);
    }

    public async Task<long> AddAvailabilityAsync(long facilityId, long branchId, long staffId, DateOnly date, TimeOnly start, TimeOnly end, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var branch = await facilities.BranchAsync(branchId, token) ?? throw new BookingNotFoundException();
        var employee = await staff.StaffAsync(staffId, token) ?? throw new BookingNotFoundException();
        ManagementScope.RequireOwner(facilityId, branch.FacilityId); ManagementScope.RequireOwner(facilityId, employee.FacilityId);
        var interval = await ValidateIntervalAsync(employee, branch, date, start, end, 0, token);
        var window = new AvailabilityWindow(employee, branch, interval); schedules.AddWindow(window);
        await transaction.SaveAsync(token); await transaction.CommitAsync(token); return window.Id;
    }

    public async Task UpdateAvailabilityAsync(long facilityId, long windowId, DateOnly date, TimeOnly start, TimeOnly end, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var window = await schedules.WindowAsync(windowId, token) ?? throw new BookingNotFoundException();
        ManagementScope.RequireOwner(facilityId, window.Branch.FacilityId); ManagementScope.RequireOwner(facilityId, window.Staff.FacilityId);
        var proposed = await ValidateIntervalAsync(window.Staff, window.Branch, date, start, end, windowId, token);
        await PreserveReservationsAsync(window, proposed, token);
        window.Reschedule(proposed);
        await transaction.SaveAsync(token); await transaction.CommitAsync(token);
    }

    public async Task RemoveAvailabilityAsync(long facilityId, long windowId, CancellationToken token = default)
    {
        await using var transaction = await scope.BeginAsync(facilityId, token);
        var window = await schedules.WindowAsync(windowId, token) ?? throw new BookingNotFoundException();
        ManagementScope.RequireOwner(facilityId, window.Branch.FacilityId); ManagementScope.RequireOwner(facilityId, window.Staff.FacilityId);
        await PreserveReservationsAsync(window, null, token); schedules.RemoveWindow(window);
        await transaction.SaveAsync(token); await transaction.CommitAsync(token);
    }

    private async Task<TimeInterval> ValidateIntervalAsync(StaffMember employee, Branch branch, DateOnly date, TimeOnly start, TimeOnly end, long excludedId, CancellationToken token)
    {
        if (start >= end || date == DateOnly.MaxValue || time.ToUtc(date, start) <= time.UtcNow)
            throw new ConfigurationException("Gelecekte başlayan, aynı gün içinde pozitif bir uygunluk aralığı girin.");
        if (!employee.IsActive || !await staff.HasActiveOfferingAsync(employee.Id, branch.Id, token))
            throw new ConfigurationException("Etkin çalışana önce bu şubede etkin bir hizmet atayın.");
        var interval = time.Interval(date, start, end);
        RequireCoverage(interval, await opening.OpeningAsync(branch.Id, date.DayOfWeek, token), "Uygunluğun tamamı şube çalışma saatlerine sığmalıdır; aralar kapalıdır.");
        if (await schedules.StaffOverlapsAsync(employee.Id, excludedId, interval.Start, interval.End, token))
            throw new ConfigurationException("Çalışanın bu saatlerle çakışan, aynı veya başka şubede bir uygunluğu var.");
        return interval;
    }

    private async Task PreserveReservationsAsync(AvailabilityWindow window, TimeInterval? replacement, CancellationToken token)
    {
        var remaining = (await schedules.StaffWindowsAsync(window.StaffId, window.BranchId, window.Id, token)).ToList();
        if (replacement is not null) remaining.Add(replacement);
        foreach (var appointment in await schedules.ReservationsAsync(window.BranchId, time.UtcNow, token))
            if (appointment.Offering.StaffId == window.StaffId && !appointment.Interval.IsCoveredBy(remaining))
                throw new ConfigurationException("Bu değişiklik gelecekteki veya devam eden ayrılmış bir randevuyu geçersiz kılıyor.");
    }

    private void RequireCoverage(TimeInterval interval, IEnumerable<OpeningWindow> windows, string error)
    {
        var date = DateOnly.FromDateTime(time.Local(interval.Start));
        if (!interval.IsCoveredBy(windows.Select(w => time.Interval(date, w.OpensAt, w.ClosesAt)))) throw new ConfigurationException(error);
    }
}
