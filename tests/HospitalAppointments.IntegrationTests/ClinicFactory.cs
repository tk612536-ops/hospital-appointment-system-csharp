using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalAppointments.IntegrationTests;

public sealed class ClinicFactory : WebApplicationFactory<Program>
{
    private readonly string environment;
    private readonly bool demoEnabled;
    private readonly bool seedBookings;
    private readonly TimeProvider? clock;
    public string DatabasePath { get; }

    public ClinicFactory() : this("Demo", true) { }

    internal ClinicFactory(string environment, bool demoEnabled, string? databasePath = null, bool seedBookings = false, TimeProvider? clock = null)
    {
        this.environment = environment;
        this.demoEnabled = demoEnabled;
        this.seedBookings = seedBookings;
        this.clock = clock;
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "HospitalAppointments.sln"))) root = root.Parent;
        if (root is null) throw new InvalidOperationException("Solution root not found.");
        DatabasePath = databasePath ?? Path.Combine(root.FullName, "TestResults", "integration", Guid.NewGuid().ToString("N"), "clinic.db");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        builder.UseSetting("Database:Path", DatabasePath);
        builder.UseSetting("Demo:Enabled", demoEnabled.ToString());
        builder.UseSetting("Demo:SeedBookings", seedBookings.ToString());
        builder.UseSetting("Logging:LogLevel:Default", "Warning");
        if (clock is not null) builder.ConfigureServices(services => services.AddSingleton(clock));
    }

    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true
    });
}
