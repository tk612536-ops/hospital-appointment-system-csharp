using System.ComponentModel.DataAnnotations;
using HospitalAppointments.Domain.Facilities;
using Microsoft.AspNetCore.Mvc;

namespace HospitalAppointments.Web.Models;

public sealed class NameInput
{
    [Required(ErrorMessage = "Ad girin."), StringLength(255, ErrorMessage = "Ad en fazla 255 karakter olabilir.")]
    public string Name { get; set; } = "";
}
public sealed class FacilityCreateInput
{
    [Required(ErrorMessage = "Kurum adı girin."), StringLength(255, ErrorMessage = "Metin en fazla 255 karakter olabilir.")] public string Name { get; set; } = "";
    [Required(ErrorMessage = "Gerekli alanı doldurun."), EnumDataType(typeof(FacilityType), ErrorMessage = "Hastane veya klinik seçin.")] public FacilityType? Type { get; set; }
    [Required(ErrorMessage = "İlk şubenin adını girin."), StringLength(255, ErrorMessage = "Metin en fazla 255 karakter olabilir.")] public string FirstBranchName { get; set; } = "";
}
public sealed class FacilityEditInput
{
    [Required(ErrorMessage = "Kurum adı girin."), StringLength(255, ErrorMessage = "Metin en fazla 255 karakter olabilir.")] public string Name { get; set; } = "";
    [Required(ErrorMessage = "Gerekli alanı doldurun."), EnumDataType(typeof(FacilityType), ErrorMessage = "Hastane veya klinik seçin.")] public FacilityType? Type { get; set; }
}
public sealed class DepartmentInput
{
    [Required(ErrorMessage = "Şube seçin."), Range(1, long.MaxValue, ErrorMessage = "Geçerli bir kayıt seçin.")] public long? BranchId { get; set; }
    [Required(ErrorMessage = "Bölüm adı girin."), StringLength(255, ErrorMessage = "Metin en fazla 255 karakter olabilir.")] public string Name { get; set; } = "";
}
public sealed class ServiceCreateInput
{
    [Required(ErrorMessage = "Bölüm seçin."), Range(1, long.MaxValue, ErrorMessage = "Geçerli bir kayıt seçin.")] public long? DepartmentId { get; set; }
    [Required(ErrorMessage = "Hizmet adı girin."), StringLength(255, ErrorMessage = "Metin en fazla 255 karakter olabilir.")] public string Name { get; set; } = "";
    [Required(ErrorMessage = "Gerekli alanı doldurun."), Range(1, 1439, ErrorMessage = "Süre 1–1439 dakika olmalıdır.")] public int? Duration { get; set; }
    [Required(ErrorMessage = "Ücret girin."), ExactTryFee, ModelBinder(BinderType = typeof(TryFeeModelBinder))] public decimal? Fee { get; set; }
}
public sealed class ServiceEditInput
{
    [Required(ErrorMessage = "Hizmet adı girin."), StringLength(255, ErrorMessage = "Metin en fazla 255 karakter olabilir.")] public string Name { get; set; } = "";
    [Required(ErrorMessage = "Gerekli alanı doldurun."), Range(1, 1439, ErrorMessage = "Süre 1–1439 dakika olmalıdır.")] public int? Duration { get; set; }
    [Required(ErrorMessage = "Ücret girin."), ExactTryFee, ModelBinder(BinderType = typeof(TryFeeModelBinder))] public decimal? Fee { get; set; }
}
public sealed class StaffRegisterInput
{
    [Required(ErrorMessage = "Çalışan adı girin."), StringLength(120, ErrorMessage = "Çalışan adı en fazla 120 karakter olabilir.")] public string Name { get; set; } = "";
    public long? SpecialtyId { get; set; }
    [StringLength(255, ErrorMessage = "Metin en fazla 255 karakter olabilir.")] public string? JobTitle { get; set; }
    [StringLength(80, ErrorMessage = "Kullanıcı adı en fazla 80 karakter olabilir.")] public string? Username { get; set; }
    [StringLength(128, ErrorMessage = "Parola en fazla 128 karakter olabilir."), DataType(DataType.Password)] public string? Password { get; set; }
}
public sealed class StaffEditInput
{
    [Required(ErrorMessage = "Gerekli alanı doldurun."), StringLength(120, ErrorMessage = "Çalışan adı en fazla 120 karakter olabilir.")] public string Name { get; set; } = "";
    public long? SpecialtyId { get; set; }
    [StringLength(255, ErrorMessage = "Metin en fazla 255 karakter olabilir.")] public string? JobTitle { get; set; }
    public bool Active { get; set; }
}
public sealed class OfferingInput
{
    [Required(ErrorMessage = "Gerekli alanı doldurun."), Range(1, long.MaxValue, ErrorMessage = "Geçerli bir kayıt seçin.")] public long? StaffId { get; set; }
    [Required(ErrorMessage = "Gerekli alanı doldurun."), Range(1, long.MaxValue, ErrorMessage = "Geçerli bir kayıt seçin.")] public long? ServiceId { get; set; }
}
public sealed class OpeningPeriodInput
{
    public TimeOnly? Opens { get; set; }
    public TimeOnly? Closes { get; set; }
}
public sealed class HoursInput
{
    [Required(ErrorMessage = "Gerekli alanı doldurun."), Range(1, long.MaxValue, ErrorMessage = "Geçerli bir kayıt seçin.")] public long? BranchId { get; set; }
    [Required(ErrorMessage = "Gerekli alanı doldurun."), EnumDataType(typeof(DayOfWeek), ErrorMessage = "Geçerli bir gün seçin.")] public DayOfWeek? Day { get; set; }
    public List<OpeningPeriodInput> Periods { get; set; } = [];
}
public sealed class AvailabilityCreateInput
{
    [Required(ErrorMessage = "Gerekli alanı doldurun."), Range(1, long.MaxValue, ErrorMessage = "Geçerli bir kayıt seçin.")] public long? BranchId { get; set; }
    [Required(ErrorMessage = "Gerekli alanı doldurun."), Range(1, long.MaxValue, ErrorMessage = "Geçerli bir kayıt seçin.")] public long? StaffId { get; set; }
    [Required(ErrorMessage = "Tarih girin.")] public DateOnly? Date { get; set; }
    [Required(ErrorMessage = "Başlangıç saati girin.")] public TimeOnly? Start { get; set; }
    [Required(ErrorMessage = "Bitiş saati girin.")] public TimeOnly? End { get; set; }
}
public sealed class AvailabilityEditInput
{
    [Required(ErrorMessage = "Tarih ve saat alanlarını doldurun.")] public DateOnly? Date { get; set; }
    [Required(ErrorMessage = "Tarih ve saat alanlarını doldurun.")] public TimeOnly? Start { get; set; }
    [Required(ErrorMessage = "Tarih ve saat alanlarını doldurun.")] public TimeOnly? End { get; set; }
}
public sealed class ExactTryFeeAttribute : ValidationAttribute
{
    public ExactTryFeeAttribute() => ErrorMessage = "Ücret sıfır veya pozitif, en fazla 9.999.999.999,99 TRY ve en çok iki ondalık basamak olmalıdır.";
    public override bool IsValid(object? value) => value is null || value is decimal fee && fee >= 0 && fee <= 9_999_999_999.99m && decimal.Truncate(fee * 100) == fee * 100;
}
