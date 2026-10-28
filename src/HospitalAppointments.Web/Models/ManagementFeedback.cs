using Microsoft.AspNetCore.Mvc;

namespace HospitalAppointments.Web.Models;

public static class ManagementFeedback
{
    public static IActionResult Invalid(Controller controller)
    {
        controller.Response.StatusCode = 400;
        var messages = controller.ModelState.SelectMany(field => field.Value!.Errors.Select(error =>
            field.Key.EndsWith("Password", StringComparison.OrdinalIgnoreCase)
                ? "Parola en fazla 128 karakter olabilir; yeni hesap için en az 8 karakter, büyük/küçük harf, rakam ve simge kullanın."
                : error.Exception is null && !string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? error.ErrorMessage : "Tarih, saat ve sayısal alanların biçimini kontrol edin.")).Distinct().ToArray();
        if (messages.Length == 0) messages = ["Gerekli alanları ve seçimleri kontrol edin."];
        return controller.View("~/Views/Shared/ManagementError.cshtml", messages);
    }
}
