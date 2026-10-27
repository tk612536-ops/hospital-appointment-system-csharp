using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalAppointments.Web.Controllers;

[Authorize(Policy = RoleNames.Manager), Route("manager")]
public sealed class FacilityManagementController(FacilityManagementService management, ManagementQueryService queries) : Controller
{
    [HttpGet("facilities"), HttpGet("config")]
    public async Task<IActionResult> Index(CancellationToken token) => View(await queries.FacilitiesAsync(token));
    [HttpGet("facilities/{facilityId:long}")]
    public async Task<IActionResult> Structure(long facilityId, CancellationToken token) => View(await queries.PageAsync(facilityId, token));
    [HttpPost("facilities")]
    public async Task<IActionResult> Create(FacilityCreateInput input, CancellationToken token)
    {
        if (!ModelState.IsValid) return ManagementFeedback.Invalid(this);
        var id = await management.CreateAsync(input.Name, input.Type!.Value, input.FirstBranchName, token);
        TempData["Message"] = "Kurum, ilk şube ve yönetim yetkiniz kaydedildi.";
        return Redirect("/manager/facilities/" + id);
    }
    [HttpPost("facilities/{facilityId:long}")]
    public async Task<IActionResult> Update(long facilityId, FacilityEditInput input, CancellationToken token)
    {
        if (!ModelState.IsValid) return ManagementFeedback.Invalid(this);
        await management.UpdateAsync(facilityId, input.Name, input.Type!.Value, token);
        return Redirect("/manager/facilities/" + facilityId);
    }
    [HttpPost("facilities/{facilityId:long}/branches")]
    public async Task<IActionResult> AddBranch(long facilityId, NameInput input, CancellationToken token)
    {
        if (!ModelState.IsValid) return ManagementFeedback.Invalid(this);
        await management.AddBranchAsync(facilityId, input.Name, token); return Redirect("/manager/facilities/" + facilityId);
    }
    [HttpPost("facilities/{facilityId:long}/branches/{id:long}")]
    public async Task<IActionResult> RenameBranch(long facilityId, long id, NameInput input, CancellationToken token)
    {
        if (!ModelState.IsValid) return ManagementFeedback.Invalid(this);
        await management.RenameBranchAsync(facilityId, id, input.Name, token); return Redirect("/manager/facilities/" + facilityId);
    }
    [HttpPost("facilities/{facilityId:long}/departments")]
    public async Task<IActionResult> AddDepartment(long facilityId, DepartmentInput input, CancellationToken token)
    {
        if (!ModelState.IsValid) return ManagementFeedback.Invalid(this);
        await management.AddDepartmentAsync(facilityId, input.BranchId!.Value, input.Name, token); return Redirect("/manager/facilities/" + facilityId);
    }
    [HttpPost("facilities/{facilityId:long}/departments/{id:long}")]
    public async Task<IActionResult> RenameDepartment(long facilityId, long id, NameInput input, CancellationToken token)
    {
        if (!ModelState.IsValid) return ManagementFeedback.Invalid(this);
        await management.RenameDepartmentAsync(facilityId, id, input.Name, token); return Redirect("/manager/facilities/" + facilityId);
    }
}
