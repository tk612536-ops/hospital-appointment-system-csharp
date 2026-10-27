using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Domain.Staff;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HospitalAppointments.Infrastructure.Repositories;

public sealed class BookingCatalogRepository(ApplicationDbContext database) : IBookingCatalogRepository
{
    private IQueryable<StaffOffering> Graph() => database.Set<StaffOffering>()
        .Include(o => o.Service).ThenInclude(s => s.Department).ThenInclude(d => d.Branch).ThenInclude(b => b.Facility)
        .Include(o => o.Staff).ThenInclude(s => s.Facility)
        .Include(o => o.Staff).ThenInclude(s => ((Doctor)s).Specialty);
    public async Task<IReadOnlyList<StaffOffering>> ListAsync(CancellationToken token) =>
        await Graph().AsNoTrackingWithIdentityResolution().OrderBy(o => o.Id).ToListAsync(token);
    public Task<StaffOffering?> FindAsync(long id, CancellationToken token) => Graph().SingleOrDefaultAsync(o => o.Id == id, token);
}
