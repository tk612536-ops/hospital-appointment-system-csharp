using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalAppointments.Web.Controllers;

[Authorize(Policy = RoleNames.Manager), Route("manager/facilities/{facilityId:long}/schedule")]
public sealed class ScheduleManagementController(ScheduleManagementService management, ManagementQueryService queries) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(long facilityId, long? branchId, DayOfWeek? day, CancellationToken token)
    {
        ViewData["BranchId"] = branchId; ViewData["Day"] = day ?? DayOfWeek.Monday;
        return View(await queries.PageAsync(facilityId, token));
    }
    private IActionResult Back(long facilityId) => Redirect("/manager/facilities/" + facilityId + "/schedule");
    [HttpPost("hours")]
    public async Task<IActionResult> Hours(long facilityId, HoursInput input, CancellationToken token)
    {
        if (!ModelState.IsValid) return ManagementFeedback.Invalid(this);
        await management.ReplaceOpeningHoursAsync(facilityId, input.BranchId!.Value, input.Day!.Value,
            input.Periods.Select(p => new OpeningPeriod(p.Opens, p.Closes)).ToArray(), token);
        return Back(facilityId);
    }
    [HttpPost("availability")]
    public async Task<IActionResult> Add(long facilityId, AvailabilityCreateInput input, CancellationToken token)
    {
        if (!ModelState.IsValid) return ManagementFeedback.Invalid(this);
        await management.AddAvailabilityAsync(facilityId, input.BranchId!.Value, input.StaffId!.Value, input.Date!.Value, input.Start!.Value, input.End!.Value, token);
        return Back(facilityId);
    }
    [HttpPost("availability/{id:long}")]
    public async Task<IActionResult> Update(long facilityId, long id, AvailabilityEditInput input, CancellationToken token)
    {
        if (!ModelState.IsValid) return ManagementFeedback.Invalid(this);
        await management.UpdateAvailabilityAsync(facilityId, id, input.Date!.Value, input.Start!.Value, input.End!.Value, token); return Back(facilityId);
    }
    [HttpPost("availability/{id:long}/remove")]
    public async Task<IActionResult> Remove(long facilityId, long id, CancellationToken token)
    {
        await management.RemoveAvailabilityAsync(facilityId, id, token); return Back(facilityId);
    }
}
