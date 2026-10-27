using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using HospitalAppointments.Domain.Appointments;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalAppointments.IntegrationTests;

public sealed class CrossProcessBookingTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task TwoKestrelProcessesSharingOneFileCannotDoubleBook()
    {
        using var scenario = await BookingScenario.CreateAsync();
        using var first = Start(scenario, Port()); using var second = Start(scenario, Port());
        try
        {
            await Task.WhenAll(ReadyAsync(first.Client), ReadyAsync(second.Client));
            Assert.NotEqual(first.Process.Id, second.Process.Id);
            Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.LoginAsync(first.Client, "patient")).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.LoginAsync(second.Client, "patient2")).StatusCode);
            var token1 = await BrowserForms.TokenAsync(first.Client, "/patient/book");
            var token2 = await BrowserForms.TokenAsync(second.Client, "/patient/book");
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task<HttpResponseMessage> Submit(HttpClient client, string token, string hour)
            {
                await start.Task.WaitAsync(Token);
                return await client.PostAsync("/patient/book", new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["OfferingId"] = scenario.North.ToString(), ["Date"] = "2030-01-09", ["StartTime"] = hour,
                    ["CatalogVersion"] = scenario.Version.ToString(), ["__RequestVerificationToken"] = token
                }), Token);
            }
            var a = Submit(first.Client, token1, "10:00"); var b = Submit(second.Client, token2, "10:15"); start.SetResult();
            var outcomes = await Task.WhenAll(a, b).WaitAsync(TimeSpan.FromSeconds(20), Token);
            Assert.Single(outcomes, r => r.StatusCode == HttpStatusCode.Redirect);
            Assert.Single(outcomes, r => r.StatusCode == HttpStatusCode.Conflict);
            using var verification = scenario.Factory.Services.CreateScope();
            Assert.Single(await verification.ServiceProvider.GetRequiredService<ApplicationDbContext>().Set<Appointment>().ToListAsync(Token));
        }
        finally { first.Stop(); second.Stop(); }
    }

    private static int Port()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return port;
    }

    private static RunningApp Start(BookingScenario scenario, int port)
    {
        var root = new DirectoryInfo(Path.GetDirectoryName(scenario.Factory.DatabasePath)!);
        while (!File.Exists(Path.Combine(root.FullName, "HospitalAppointments.sln"))) root = root.Parent!;
        var webRoot = Path.Combine(root.FullName, "src", "HospitalAppointments.Web");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(webRoot, "bin", configuration, "net10.0", "HospitalAppointments.Web.dll");
        var info = new ProcessStartInfo("dotnet") { WorkingDirectory = webRoot, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { dll, "--environment", "Demo", "--Database:Path", scenario.Factory.DatabasePath, "--urls", "https://localhost:" + port }) info.ArgumentList.Add(argument);
        var process = Process.Start(info) ?? throw new InvalidOperationException("Child host failed to start.");
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false, UseCookies = true,
            // Test-only known localhost hosts; no machine trust or browser settings are changed.
            ServerCertificateCustomValidationCallback = (request, _, _, _) => request.RequestUri!.IsLoopback
        };
        return new RunningApp(process, new HttpClient(handler) { BaseAddress = new Uri("https://localhost:" + port), Timeout = TimeSpan.FromSeconds(10) });
    }

    private static async Task ReadyAsync(HttpClient client)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            try { if ((await client.GetAsync("/", Token)).IsSuccessStatusCode) return; }
            catch (HttpRequestException) { }
            await Task.Delay(100, Token); // Startup readiness polling, not race synchronization.
        }
        throw new TimeoutException("Child Kestrel host did not become ready.");
    }

    private sealed class RunningApp : IDisposable
    {
        public Process Process { get; }
        public HttpClient Client { get; }
        private readonly Task<string> stdout;
        private readonly Task<string> stderr;
        public RunningApp(Process process, HttpClient client)
        {
            Process = process; Client = client;
            stdout = process.StandardOutput.ReadToEndAsync(Token); stderr = process.StandardError.ReadToEndAsync(Token);
        }
        public void Stop() { if (!Process.HasExited) Process.Kill(entireProcessTree: true); }
        public void Dispose() { Stop(); Client.Dispose(); Process.Dispose(); }
    }
}
