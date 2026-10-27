using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Domain.Appointments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalAppointments.Web.Controllers;

[Authorize]
public sealed class AppointmentController(AppointmentService appointments) : Controller
{
    [HttpGet("appointments/{id:long}")]
    public async Task<IActionResult> Detail(long id, CancellationToken token) => View(await appointments.DetailAsync(id, token));

    [Authorize(Policy = RoleNames.Patient), HttpGet("patient/appointments")]
    public async Task<IActionResult> PatientList(CancellationToken token)
    {
        ViewData["Title"] = "Randevularım";
        return View("List", await appointments.ListAsync(true, token));
    }

    [Authorize(Roles = RoleNames.Staff + "," + RoleNames.Manager), HttpGet("work/appointments")]
    public async Task<IActionResult> WorkList(CancellationToken token)
    {
        ViewData["Title"] = "Randevu değerlendirme";
        return View("List", await appointments.ListAsync(false, token));
    }

    [Authorize(Roles = RoleNames.Staff + "," + RoleNames.Manager), HttpPost("appointments/{id:long}/decision")]
    public async Task<IActionResult> Decide(long id, AppointmentStatus decision, CancellationToken token)
    {
        if (!ModelState.IsValid) return BadRequest("Onay veya ret seçin.");
        await appointments.DecideAsync(id, decision, token);
        TempData["Message"] = decision == AppointmentStatus.Approved ? "Randevu onaylandı." : "Randevu reddedildi; ayrılan saat serbest bırakıldı.";
        return RedirectToAction(nameof(Detail), new { id });
    }
}
