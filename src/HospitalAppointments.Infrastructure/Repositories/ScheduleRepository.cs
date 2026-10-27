using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Domain.Facilities;
using HospitalAppointments.Domain.Scheduling;
using HospitalAppointments.Domain.Staff;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointments.Infrastructure.Repositories;

public sealed class ScheduleRepository(ApplicationDbContext database) : IScheduleRepository
{
    public async Task<IReadOnlyList<OpeningWindow>> OpeningAsync(long branchId, DayOfWeek weekday, CancellationToken token) =>
        await database.Set<OpeningWindow>().AsNoTracking().Where(w => w.BranchId == branchId && w.Weekday == weekday).ToListAsync(token);
    public async Task<IReadOnlyList<TimeInterval>> AvailabilityAsync(long staffId, long branchId, DateTime start, DateTime end, CancellationToken token) =>
        await database.Set<AvailabilityWindow>().AsNoTracking().Where(w => w.StaffId == staffId && w.BranchId == branchId && w.Start < end && start < w.End)
            .Select(w => new TimeInterval(w.Start, w.End)).ToListAsync(token);
}
