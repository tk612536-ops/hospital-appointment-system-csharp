using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Application.Transactions;
using HospitalAppointments.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalAppointments.Web.Controllers;

[Authorize(Policy = RoleNames.Patient)]
public sealed class BookingController(BookingPageService pages, AppointmentService appointments) : Controller
{
    [HttpGet("patient/book")]
    public async Task<IActionResult> Index(long? facilityId, long? branchId, long? serviceId, long? offeringId, DateOnly? date, CancellationToken token)
    {
        if (!ModelState.IsValid) return BadRequest("Geçerli bir tarih ve seçim girin.");
        var page = await pages.PageAsync(facilityId, branchId, serviceId, offeringId, date, token);
        return View(new BookingInput { Page = page });
    }

    [HttpPost("patient/book")]
    public async Task<IActionResult> Book(BookingInput input, CancellationToken token)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var id = await appointments.BookAsync(new(input.OfferingId!.Value, input.Date!.Value, input.StartTime!.Value, input.CatalogVersion!.Value), token);
                TempData["Message"] = "Randevunuz kaydedildi. Seçtiğiniz saat onay beklerken sizin için ayrılmıştır.";
                return RedirectToAction(nameof(AppointmentController.Detail), "Appointment", new { id });
            }
            catch (BookingException exception) { ModelState.AddModelError("", exception.Message); Response.StatusCode = 409; }
            catch (DatabaseBusyException) { ModelState.AddModelError("", "Sistem şu anda meşgul. Seçimlerinizi kontrol edip yeniden deneyin."); Response.StatusCode = 503; Response.Headers.RetryAfter = "1"; }
        }
        else Response.StatusCode = 400;
        input.Page = await pages.PageAsync(null, null, null, input.OfferingId, input.Date, token);
        return View("Index", input);
    }
}
