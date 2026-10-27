using System.Globalization;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Infrastructure;
using HospitalAppointments.Infrastructure.Demo;
using HospitalAppointments.Infrastructure.Persistence;
using HospitalAppointments.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
var databasePath = Path.GetFullPath(builder.Configuration["Database:Path"] ?? "data/clinic.db", builder.Environment.ContentRootPath);
var webRoot = Path.GetFullPath(builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot"));
if (databasePath.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("The database must be outside wwwroot.");

builder.Services.AddInfrastructure(databasePath);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActor, HttpCurrentActor>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<ClinicTime>();
builder.Services.AddScoped<BookingAccessService>();
builder.Services.AddScoped<AvailabilityService>();
builder.Services.AddScoped<AppointmentService>();
builder.Services.AddScoped<BookingPageService>();
builder.Services.AddScoped<ManagementScope>();
builder.Services.AddScoped<FacilityManagementService>();
builder.Services.AddScoped<CatalogManagementService>();
builder.Services.AddScoped<StaffManagementService>();
builder.Services.AddScoped<ScheduleManagementService>();
builder.Services.AddScoped<ManagementQueryService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"));
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<HospitalAppointments.Web.Models.BookingExceptionFilter>();
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((_, _) => "Geçerli bir değer girin.");
});
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "HospitalAppointments.Antiforgery";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(RoleNames.Patient, policy => policy.RequireRole(RoleNames.Patient));
    options.AddPolicy(RoleNames.Staff, policy => policy.RequireRole(RoleNames.Staff));
    options.AddPolicy(RoleNames.Manager, policy => policy.RequireRole(RoleNames.Manager));
});
var demoEnabled = builder.Environment.IsEnvironment("Demo") && builder.Configuration.GetValue<bool>("Demo:Enabled");
if (demoEnabled) builder.Services.AddScoped<DemoAccountSeeder>();
var seedBookings = demoEnabled && builder.Configuration.GetValue<bool>("Demo:SeedBookings");
if (seedBookings) builder.Services.AddScoped<DemoBookingSeeder>();

var app = builder.Build();
app.UseExceptionHandler("/home/error");
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Demo") && !app.Environment.IsEnvironment("Testing"))
    app.UseHsts();
app.UseHttpsRedirection();
var culture = CultureInfo.GetCultureInfo("tr-TR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = [culture], SupportedUICultures = [culture],
    RequestCultureProviders = []
});
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync();
    if (demoEnabled)
    {
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var transaction = await database.Database.BeginTransactionAsync();
        await scope.ServiceProvider.GetRequiredService<DemoAccountSeeder>().SeedAsync();
        if (seedBookings) await scope.ServiceProvider.GetRequiredService<DemoBookingSeeder>().SeedAsync();
        await transaction.CommitAsync();
    }
}
await app.RunAsync();

public partial class Program;
