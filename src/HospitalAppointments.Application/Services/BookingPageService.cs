using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Repositories;

namespace HospitalAppointments.Application.Services;

public sealed class BookingPageService(IBookingCatalogRepository catalog, BookingAccessService access, AvailabilityService availability, ClinicTime time)
{
    public async Task<BookingPage> PageAsync(long? facilityId, long? branchId, long? serviceId, long? offeringId, DateOnly? date, CancellationToken token = default)
    {
        var patient = await access.PatientAsync(token);
        var all = (await catalog.ListAsync(token)).Where(o => o.IsBookable).ToArray();
        var day = date ?? time.Today.AddDays(1);
        if (all.Length == 0) return new([], [], [], [], null, null, null, null, day, 0, 0, "TRY", 0, []);
        var requested = all.FirstOrDefault(o => o.Id == offeringId);
        facilityId ??= requested?.Branch.FacilityId;
        var facilities = all.Select(o => new Choice(o.Branch.FacilityId, o.Branch.Facility.Name)).Distinct().ToArray();
        var facility = facilities.FirstOrDefault(f => f.Id == facilityId) ?? facilities[0];
        var atFacility = all.Where(o => o.Branch.FacilityId == facility.Id).ToArray();
        branchId ??= requested?.Branch.Id;
        var branches = atFacility.Select(o => new Choice(o.Branch.Id, o.Branch.Name)).Distinct().ToArray();
        var branch = branches.FirstOrDefault(b => b.Id == branchId) ?? branches[0];
        var atBranch = atFacility.Where(o => o.Branch.Id == branch.Id).ToArray();
        serviceId ??= requested?.ServiceId;
        var services = atBranch.Select(o => new Choice(o.ServiceId, o.Service.Name)).Distinct().ToArray();
        var service = services.FirstOrDefault(s => s.Id == serviceId) ?? services[0];
        var eligible = atBranch.Where(o => o.ServiceId == service.Id).ToArray();
        var offering = eligible.FirstOrDefault(o => o.Id == offeringId) ?? eligible[0];
        // Both concrete staff types are rendered through the real abstract contract.
        var staff = eligible.Select(o => new Choice(o.Id, o.Staff.Name + " · " + o.Staff.ProfessionalDescription)).ToArray();
        var starts = (await availability.LoadAsync(offering, patient, day, token)).Starts();
        return new(facilities, branches, services, staff, facility.Id, branch.Id, service.Id, offering.Id, day,
            offering.Service.DurationMinutes, offering.Service.Fee, offering.Service.Currency, offering.Service.Version, starts);
    }
}
