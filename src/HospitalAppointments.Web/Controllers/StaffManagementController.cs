using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalAppointments.Web.Controllers;

[Authorize(Policy = RoleNames.Manager), Route("manager/facilities/{facilityId:long}/staff")]
public sealed class StaffManagementController(StaffManagementService management, ManagementQueryService queries) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(long facilityId, CancellationToken token) => View(await queries.PageAsync(facilityId, token));
    private IActionResult Back(long facilityId) => Redirect("/manager/facilities/" + facilityId + "/staff");
    [HttpPost("specialties")]
    public async Task<IActionResult> Specialty(long facilityId, NameInput input, CancellationToken token)
    {
        if (!ModelState.IsValid) return ManagementFeedback.Invalid(this);
        await management.AddSpecialtyAsync(facilityId, input.Name, token); return Back(facilityId);
    }
    [HttpPost("doctors")]
    public async Task<IActionResult> Doctor(long facilityId, StaffRegisterInput input, CancellationToken token)
    {
        if (input.SpecialtyId is not > 0) ModelState.AddModelError(nameof(input.SpecialtyId), "Doktor için bir uzmanlık seçin.");
        if (!ModelState.IsValid) return ManagementFeedback.Invalid(this);
        await management.RegisterDoctorAsync(facilityId, input.Name, input.SpecialtyId!.Value, input.Username, input.Password, token); return Back(facilityId);
    }
    [HttpPost("healthcare")]
    public async Task<IActionResult> Healthcare(long facilityId, StaffRegisterInput input, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(input.JobTitle)) ModelState.AddModelError(nameof(input.JobTitle), "Sağlık personeli için meslek / görev girin.");
        if (!ModelState.IsValid) return ManagementFeedback.Invalid(this);
        await management.RegisterHealthcareStaffAsync(facilityId, input.Name, input.JobTitle!, input.Username, input.Password, token); return Back(facilityId);
    }
    [HttpPost("doctors/{id:long}")]
    public async Task<IActionResult> UpdateDoctor(long facilityId, long id, StaffEditInput input, CancellationToken token)
    {
        if (input.SpecialtyId is not > 0) ModelState.AddModelError(nameof(input.SpecialtyId), "Doktor için bir uzmanlık seçin.");
        if (!ModelState.IsValid) return ManagementFeedback.Invalid(this);
        await management.UpdateDoctorAsync(facilityId, id, input.Name, input.SpecialtyId!.Value, input.Active, token); return Back(facilityId);
    }
    [HttpPost("healthcare/{id:long}")]
    public async Task<IActionResult> UpdateHealthcare(long facilityId, long id, StaffEditInput input, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(input.JobTitle)) ModelState.AddModelError(nameof(input.JobTitle), "Sağlık personeli için meslek / görev girin.");
        if (!ModelState.IsValid) return ManagementFeedback.Invalid(this);
        await management.UpdateHealthcareStaffAsync(facilityId, id, input.Name, input.JobTitle!, input.Active, token); return Back(facilityId);
    }
    [HttpPost("offerings")]
    public async Task<IActionResult> Assign(long facilityId, OfferingInput input, CancellationToken token)
    {
        if (!ModelState.IsValid) return ManagementFeedback.Invalid(this);
        await management.AssignServiceAsync(facilityId, input.StaffId!.Value, input.ServiceId!.Value, token); return Back(facilityId);
    }
    [HttpPost("offerings/{id:long}/active")]
    public async Task<IActionResult> OfferingActive(long facilityId, long id, bool active, CancellationToken token)
    {
        await management.ChangeOfferingActiveAsync(facilityId, id, active, token); return Back(facilityId);
    }
}
