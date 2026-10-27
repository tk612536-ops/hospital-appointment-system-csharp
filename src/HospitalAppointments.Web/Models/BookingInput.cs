using System.ComponentModel.DataAnnotations;
using HospitalAppointments.Application.Contracts;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HospitalAppointments.Web.Models;

public sealed class BookingInput
{
    [Required(ErrorMessage = "Bir çalışan seçin.")]
    public long? OfferingId { get; set; }
    [Required(ErrorMessage = "Bir tarih seçin.")]
    public DateOnly? Date { get; set; }
    [Required(ErrorMessage = "Uygun bir başlangıç saati seçin.")]
    public TimeOnly? StartTime { get; set; }
    [Required(ErrorMessage = "Hizmet koşullarını yeniden yükleyin.")]
    public long? CatalogVersion { get; set; }
    [BindNever]
    public BookingPage? Page { get; set; }
}
