using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Transactions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace HospitalAppointments.Web.Models;

public sealed class BookingExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is AccountAccessDeniedException)
        {
            context.Result = new ForbidResult(); context.ExceptionHandled = true; return;
        }
        var (status, message) = context.Exception switch
        {
            BookingNotFoundException => (404, "Aradığınız kayıt bulunamadı."),
            BookingException exception => (409, exception.Message),
            DatabaseBusyException => (503, "Sistem şu anda meşgul. Lütfen kısa süre sonra yeniden deneyin."),
            ConfigurationException exception => (409, exception.Message),
            PersistenceConflictException => (409, "Aynı bilgilerle bir kayıt zaten var veya kayıt kullanımda. Bilgileri kontrol edin."),
            ArgumentException => (400, "Geçerli ad, süre, ücret ve pozitif saat aralıkları girin. Hiçbir değişiklik kaydedilmedi."),
            _ => (0, "")
        };
        if (status == 0) return;
        if (status == 503) context.HttpContext.Response.Headers.RetryAfter = "1";
        if (context.RouteData.Values["controller"] is string controller && controller.EndsWith("Management", StringComparison.Ordinal))
        {
            context.Result = new ViewResult
            {
                StatusCode = status, ViewName = "~/Views/Shared/ManagementError.cshtml",
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), context.ModelState) { Model = new[] { message } }
            };
            context.ExceptionHandled = true; return;
        }
        context.Result = new ContentResult { StatusCode = status, ContentType = "text/plain; charset=utf-8", Content = message };
        context.ExceptionHandled = true;
    }
}
