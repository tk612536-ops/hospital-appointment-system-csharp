using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalAppointments.Web.Controllers;

[Authorize(Policy = RoleNames.Manager), Route("manager/facilities/{facilityId:long}/services")]
public sealed class CatalogManagementController(CatalogManagementService management, ManagementQueryService queries) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(long facilityId, CancellationToken token) => View(await queries.PageAsync(facilityId, token));
    [HttpPost]
    public async Task<IActionResult> Create(long facilityId, ServiceCreateInput input, CancellationToken token)
    {
        if (!ModelState.IsValid) return ManagementFeedback.Invalid(this);
        await management.CreateAsync(facilityId, input.DepartmentId!.Value, input.Name, input.Duration!.Value, input.Fee!.Value, token);
        return Redirect("/manager/facilities/" + facilityId + "/services");
    }
    [HttpPost("{id:long}")]
    public async Task<IActionResult> Update(long facilityId, long id, ServiceEditInput input, CancellationToken token)
    {
        if (!ModelState.IsValid) return ManagementFeedback.Invalid(this);
        await management.UpdateAsync(facilityId, id, input.Name, input.Duration!.Value, input.Fee!.Value, token);
        return Redirect("/manager/facilities/" + facilityId + "/services");
    }
    [HttpPost("{id:long}/active")]
    public async Task<IActionResult> Active(long facilityId, long id, bool active, CancellationToken token)
    {
        await management.ChangeActiveAsync(facilityId, id, active, token); return Redirect("/manager/facilities/" + facilityId + "/services");
    }
}
