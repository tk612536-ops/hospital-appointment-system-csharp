using HospitalAppointments.Domain.Appointments;

namespace HospitalAppointments.Web.Models;

public static class AppointmentLabels
{
    public static string For(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Pending => "Onay Bekliyor",
        AppointmentStatus.Approved => "Onaylandı",
        AppointmentStatus.Rejected => "Reddedildi",
        _ => "Bilinmeyen durum"
    };
}
