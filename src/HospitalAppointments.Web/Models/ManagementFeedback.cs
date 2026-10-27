using Microsoft.AspNetCore.Mvc;

namespace HospitalAppointments.Web.Models;

public static class ManagementFeedback
{
    public static IActionResult Invalid(Controller controller)
    {
        controller.Response.StatusCode = 400;
        string[] messages = ["Gerekli alanları, ad uzunluklarını, uzmanlık/meslek seçimini, süreyi ve saat biçimlerini kontrol edin. TRY ücretini binlik ayırıcı olmadan, virgülle ve en fazla iki ondalık basamakla girin (ör. 750,50)."];
        return controller.View("~/Views/Shared/ManagementError.cshtml", messages);
    }
}
