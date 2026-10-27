using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Transactions;
using HospitalAppointments.Infrastructure.Identity;
using HospitalAppointments.Infrastructure.Persistence;
using HospitalAppointments.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalAppointments.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string databasePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        var connection = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath, ForeignKeys = true, DefaultTimeout = 5
        };
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection.ToString()));
        services.AddIdentity<ApplicationUser, IdentityRole<long>>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        }).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        services.AddScoped<IUserValidator<ApplicationUser>, AccountValidator>();
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "HospitalAppointments.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.LoginPath = "/account/login";
            options.AccessDeniedPath = "/account/access-denied";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = true;
        });
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IAuthenticationGateway, IdentityAuthenticationGateway>();
        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<IBookingCatalogRepository, BookingCatalogRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<IBookingAccessRepository, BookingAccessRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IWriteTransaction, SqliteWriteTransaction>();
        services.AddScoped<IFacilityManagementRepository, FacilityManagementRepository>();
        services.AddScoped<ICatalogManagementRepository, CatalogManagementRepository>();
        services.AddScoped<IStaffManagementRepository, StaffManagementRepository>();
        services.AddScoped<IScheduleManagementRepository, ScheduleManagementRepository>();
        services.AddScoped<IManagementReadRepository, ManagementReadRepository>();
        services.AddScoped<IStaffAccountProvisioner, StaffAccountProvisioner>();
        return services;
    }
}
