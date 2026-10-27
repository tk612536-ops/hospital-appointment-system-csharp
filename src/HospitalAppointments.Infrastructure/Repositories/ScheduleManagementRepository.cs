using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Scheduling;
using HospitalAppointments.Domain.Staff;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointments.Infrastructure.Repositories;

public sealed class ScheduleManagementRepository(ApplicationDbContext db) : IScheduleManagementRepository
{
    public Task<AvailabilityWindow?> WindowAsync(long id, CancellationToken token) => db.Set<AvailabilityWindow>()
        .Include(w => w.Branch).ThenInclude(b => b.Facility).Include(w => w.Staff).ThenInclude(s => s.Facility).SingleOrDefaultAsync(w => w.Id == id, token);
    public async Task<IReadOnlyList<AvailabilityWindow>> FutureShiftsAsync(long branchId, DateTime now, CancellationToken token) =>
        await db.Set<AvailabilityWindow>().AsNoTracking().Where(w => w.BranchId == branchId && w.End > now).ToListAsync(token);
    public async Task<IReadOnlyList<TimeInterval>> StaffWindowsAsync(long staffId, long branchId, long excludedId, CancellationToken token) =>
        await db.Set<AvailabilityWindow>().AsNoTracking().Where(w => w.StaffId == staffId && w.BranchId == branchId && w.Id != excludedId)
            .Select(w => new TimeInterval(w.Start, w.End)).ToListAsync(token);
    public Task<bool> StaffOverlapsAsync(long staffId, long excludedId, DateTime start, DateTime end, CancellationToken token) =>
        db.Set<AvailabilityWindow>().AnyAsync(w => w.StaffId == staffId && w.Id != excludedId && w.Start < end && start < w.End, token);
    public async Task<IReadOnlyList<Appointment>> ReservationsAsync(long branchId, DateTime now, CancellationToken token) =>
        await db.Set<Appointment>().AsNoTracking().Include(a => a.Offering)
            .Where(a => a.Offering.Service.Department.BranchId == branchId && a.End > now && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved)).ToListAsync(token);
    public async Task ReplaceHoursAsync(long branchId, DayOfWeek day, IReadOnlyList<OpeningWindow> replacements, CancellationToken token)
    {
        await db.Set<OpeningWindow>().Where(w => w.BranchId == branchId && w.Weekday == day).ExecuteDeleteAsync(token);
        db.Set<OpeningWindow>().AddRange(replacements);
    }
    public void AddWindow(AvailabilityWindow window) => db.Add(window);
    public void RemoveWindow(AvailabilityWindow window) => db.Remove(window);
}
