using HospitalAppointments.Application.Contracts;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace HospitalAppointments.Infrastructure.Identity;

public sealed class StaffAccountProvisioner(UserManager<ApplicationUser> users, ApplicationDbContext db) : IStaffAccountProvisioner
{
    public async Task<long> CreateAsync(string username, string displayName, string password, CancellationToken token)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("Staff identity creation must share the management transaction.");
        token.ThrowIfCancellationRequested();
        if (await users.FindByNameAsync(username) is not null) throw new ConfigurationException("Bu kullanıcı adı zaten kullanılıyor; mevcut hesap bağlanamaz.");
        var user = new ApplicationUser(username, displayName);
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded) throw new ConfigurationException("Hesap oluşturulamadı. Kullanıcı adını ve parola kurallarını kontrol edin: en az 8 karakter, büyük/küçük harf, rakam ve sembol.");
        var assigned = await users.AddToRoleAsync(user, RoleNames.Staff);
        if (!assigned.Succeeded) throw new ConfigurationException("Personel rolü atanamadı; hiçbir kayıt kaydedilmedi.");
        return user.Id;
    }
}
