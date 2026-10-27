using System.ComponentModel.DataAnnotations;

namespace HospitalAppointments.Web.Models;

public sealed class LoginInput
{
    [Required(ErrorMessage = "Kullanıcı adınızı girin.")]
    [StringLength(80, ErrorMessage = "Kullanıcı adı en fazla 80 karakter olabilir.")]
    [Display(Name = "Kullanıcı adı")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parolanızı girin.")]
    [StringLength(128, ErrorMessage = "Parola en fazla 128 karakter olabilir.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parola")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
