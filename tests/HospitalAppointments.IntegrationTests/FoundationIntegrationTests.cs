using System.Net;
using HospitalAppointments.Application.Repositories;
using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Domain.Accounts;
using HospitalAppointments.Infrastructure.Demo;
using HospitalAppointments.Infrastructure.Identity;
using HospitalAppointments.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HospitalAppointments.IntegrationTests;

public sealed class FoundationIntegrationTests(ClinicFactory factory) : IClassFixture<ClinicFactory>
{
    [Fact]
    public async Task StartupResolvesServicesAndUsesTheMigratedSqliteSchema()
    {
        using var browser = factory.Browser();
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/", TestContext.Current.CancellationToken)).StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Same(db, scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AccountService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAccountRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAuthenticationGateway>());
        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", db.Database.ProviderName);
        Assert.Equal(factory.DatabasePath, db.Database.GetDbConnection().DataSource);
        Assert.Equal(db.Database.GetMigrations().Count(), (await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).Count());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await db.Set<HospitalAppointments.Domain.Appointments.Appointment>().ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal("Europe/Istanbul", scope.ServiceProvider.GetRequiredService<TimeZoneInfo>().Id);
        await db.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "PRAGMA foreign_keys;";
        Assert.Equal(1L, await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
        command.CommandText = "PRAGMA journal_mode;";
        Assert.Equal("wal", await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/account/login")]
    [InlineData("/vendor/bootstrap/bootstrap.min.css")]
    [InlineData("/css/site.css")]
    [InlineData("/favicon.svg")]
    public async Task PublicPagesAndLocalAssetsAreAvailable(string path)
    {
        using var browser = factory.Browser();
        var response = await browser.GetAsync(path, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        if (path == "/")
        {
            var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("lang=\"tr\"", html);
            Assert.Contains("Hizmetleri, çalışan atamalarını", WebUtility.HtmlDecode(html));
            Assert.DoesNotContain("https://cdn", html);
        }
    }

    [Theory]
    [InlineData("/workspace")]
    [InlineData("/patient")]
    [InlineData("/staff")]
    [InlineData("/manager")]
    public async Task UnauthenticatedVisitorsAreRedirectedToLogin(string path)
    {
        using var browser = factory.Browser();
        var response = await browser.GetAsync(path, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("patient", "/patient", "/staff", "/manager")]
    [InlineData("staff", "/staff", "/patient", "/manager")]
    [InlineData("manager", "/manager", "/patient", "/staff")]
    public async Task RealIdentityLoginGrantsOnlyAssignedRole(string username, string allowed, string deniedOne, string deniedTwo)
    {
        using var browser = factory.Browser();
        var login = await BrowserForms.LoginAsync(browser, username);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/workspace", login.Headers.Location!.OriginalString);
        var cookie = login.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("HospitalAppointments.Auth=", StringComparison.Ordinal));
        Assert.Contains("secure", cookie.ToLowerInvariant());
        Assert.Contains("httponly", cookie.ToLowerInvariant());
        Assert.Contains("samesite=lax", cookie.ToLowerInvariant());
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync(allowed, TestContext.Current.CancellationToken)).StatusCode);
        foreach (var denied in new[] { deniedOne, deniedTwo })
        {
            var response = await browser.GetAsync(denied, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("/account/access-denied", response.Headers.Location!.OriginalString);
            Assert.Equal(HttpStatusCode.Forbidden, (await browser.GetAsync(response.Headers.Location, TestContext.Current.CancellationToken)).StatusCode);
        }
        var workspace = await browser.GetStringAsync("/workspace", TestContext.Current.CancellationToken);
        Assert.Contains("href=\"" + allowed + "\"", workspace);
        Assert.DoesNotContain("href=\"" + deniedOne + "\"", workspace);
        Assert.DoesNotContain(DemoAccountSeeder.Password, workspace);
        Assert.Contains("href=\"#main-content\"", workspace);
    }

    [Theory]
    [InlineData("patient", "wrong-password")]
    [InlineData("missing", "KlinikDemo10!")]
    [InlineData("İİİ", "wrong-password")]
    public async Task InvalidCredentialsNeverAuthenticate(string username, string password)
    {
        using var browser = factory.Browser();
        var response = await BrowserForms.LoginAsync(browser, username, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Giriş yapılamadı", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)));
        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/workspace", TestContext.Current.CancellationToken)).StatusCode);
        Assert.DoesNotContain("value=\"" + password + "\"", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task LoginAcceptsTrimmedCaseInsensitiveUsernameAndRejectsExternalReturnUrl()
    {
        using var browser = factory.Browser();
        var response = await BrowserForms.LoginAsync(browser, " PATIENT ", returnUrl: "https://example.com/steal");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/workspace", response.Headers.Location!.OriginalString);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/patient", TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task LoginWithoutAntiforgeryCannotAuthenticate()
    {
        using var browser = factory.Browser();
        var response = await browser.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["Username"] = "patient", ["Password"] = DemoAccountSeeder.Password }), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/workspace", TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task ATokenFromAnotherBrowserCannotAuthenticate()
    {
        using var first = factory.Browser();
        using var second = factory.Browser();
        var foreignToken = await BrowserForms.TokenAsync(first, "/account/login");
        await BrowserForms.TokenAsync(second, "/account/login");
        var response = await second.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = "patient", ["Password"] = DemoAccountSeeder.Password,
            ["__RequestVerificationToken"] = foreignToken
        }), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await second.GetAsync("/patient", TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task AnAccountWithoutAssignedRolesCannotLogin()
    {
        using var isolated = new ClinicFactory();
        using var scope = isolated.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser("unassigned", "Demo Unassigned");
        Assert.True((await users.CreateAsync(user, DemoAccountSeeder.Password)).Succeeded);
        Assert.Null(await scope.ServiceProvider.GetRequiredService<IAccountRepository>().FindEnabledAsync(user.Id, TestContext.Current.CancellationToken));
        using var browser = isolated.Browser();
        Assert.Equal(HttpStatusCode.OK, (await BrowserForms.LoginAsync(browser, "unassigned")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/workspace", TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task LogoutRequiresAntiforgeryAndClearsTheAuthenticationCookie()
    {
        using var browser = factory.Browser();
        await BrowserForms.LoginAsync(browser, "patient");
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await browser.GetAsync("/account/logout", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await browser.PostAsync("/account/logout", new FormUrlEncodedContent([]), TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/patient", TestContext.Current.CancellationToken)).StatusCode);
        var token = await BrowserForms.TokenAsync(browser, "/workspace");
        var response = await browser.PostAsync("/account/logout", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token }), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/patient", TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task PasswordsAreHashedAndSeederNeverReplacesStoredCredentials()
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await users.FindByNameAsync("staff"))!;
        var originalHash = user.PasswordHash;
        var id = user.Id;
        Assert.NotEqual(DemoAccountSeeder.Password, originalHash);
        Assert.True(await users.CheckPasswordAsync(user, DemoAccountSeeder.Password));
        Assert.False(await users.CheckPasswordAsync(user, "wrong"));
        await scope.ServiceProvider.GetRequiredService<DemoAccountSeeder>().SeedAsync();
        var again = (await users.FindByNameAsync("staff"))!;
        Assert.Equal(id, again.Id);
        Assert.Equal(originalHash, again.PasswordHash);
        Assert.Equal(3, await users.Users.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RolesComposeAndNormalizedUsernamesRemainUnique()
    {
        using var isolated = new ClinicFactory();
        using var scope = isolated.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await users.FindByNameAsync("patient"))!;
        Assert.True((await users.AddToRoleAsync(user, RoleNames.Staff)).Succeeded);
        var repository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var profile = (await repository.FindEnabledAsync(user.Id, TestContext.Current.CancellationToken))!;
        Assert.True(profile.Roles.SetEquals([AccountRole.Patient, AccountRole.Staff]));
        Assert.False((await users.CreateAsync(new ApplicationUser("PATIENT", "Duplicate"), DemoAccountSeeder.Password)).Succeeded);
        using var browser = isolated.Browser();
        await BrowserForms.LoginAsync(browser, "patient");
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/staff", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/patient", TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task FiveWrongPasswordsLockOutUntilTheFiveMinuteWindowEnds()
    {
        using var isolated = new ClinicFactory();
        using var browser = isolated.Browser();
        for (var attempt = 0; attempt < 5; attempt++) await BrowserForms.LoginAsync(browser, "patient", "wrong");
        using var scope = isolated.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await users.FindByNameAsync("patient"))!;
        Assert.True(await users.IsLockedOutAsync(user));
        Assert.InRange(user.LockoutEnd!.Value - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(5));
        Assert.Equal(HttpStatusCode.OK, (await BrowserForms.LoginAsync(browser, "patient")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/patient", TestContext.Current.CancellationToken)).StatusCode);
        Assert.True((await users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddSeconds(-1))).Succeeded);
        Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.LoginAsync(browser, "patient")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/patient", TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task DisabledAccountsCannotLoginAndExistingCookiesAreInvalidated()
    {
        using var isolated = new ClinicFactory();
        using var browser = isolated.Browser();
        await BrowserForms.LoginAsync(browser, "patient");
        using var scope = isolated.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = (await users.FindByNameAsync("patient"))!;
        user.Disable();
        Assert.True((await users.UpdateAsync(user)).Succeeded);
        Assert.Null(await scope.ServiceProvider.GetRequiredService<IAccountRepository>().FindEnabledAsync(user.Id, TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.Redirect, (await browser.GetAsync("/patient", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await BrowserForms.LoginAsync(browser, "patient")).StatusCode);
    }

    [Fact]
    public async Task WeakPasswordsAndOversizedUsernamesAreRejected()
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        Assert.False((await users.CreateAsync(new ApplicationUser("weak-user", "Demo"), "123")).Succeeded);
        var oversized = new ApplicationUser("valid-user", "Demo") { UserName = new string('x', 81) };
        Assert.False((await users.CreateAsync(oversized, DemoAccountSeeder.Password)).Succeeded);
        using var browser = factory.Browser();
        Assert.Equal(HttpStatusCode.OK, (await BrowserForms.LoginAsync(browser, new string('x', 81))).StatusCode);
    }

    [Fact]
    public async Task DatabaseAlsoRejectsDuplicateNormalizedUsername()
    {
        using var isolated = new ClinicFactory();
        using var scope = isolated.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var duplicate = new ApplicationUser("different", "Demo") { NormalizedUserName = "PATIENT", SecurityStamp = Guid.NewGuid().ToString() };
        db.Users.Add(duplicate);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RestartPreservesAccountsPasswordChangesAndMigrationHistory()
    {
        string path;
        long accountId;
        string? changedHash;
        using (var first = new ClinicFactory())
        {
            path = first.DatabasePath;
            using var scope = first.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await users.FindByNameAsync("patient"))!;
            accountId = user.Id;
            Assert.True((await users.ChangePasswordAsync(user, DemoAccountSeeder.Password, "ChangedDemo10!")).Succeeded);
            changedHash = user.PasswordHash;
        }
        using var restarted = new ClinicFactory("Demo", true, path);
        using var newScope = restarted.Services.CreateScope();
        var db = newScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var saved = await db.Users.SingleAsync(user => user.UserName == "patient", TestContext.Current.CancellationToken);
        Assert.Equal(3, await db.Users.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(accountId, saved.Id);
        Assert.Equal(changedHash, saved.PasswordHash);
        Assert.Equal(db.Database.GetMigrations().Count(), (await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).Count());
        using var browser = restarted.Browser();
        Assert.Equal(HttpStatusCode.Redirect, (await BrowserForms.LoginAsync(browser, "patient", "ChangedDemo10!")).StatusCode);
    }

    [Theory]
    [InlineData("Production", true)]
    [InlineData("Development", true)]
    [InlineData("Demo", false)]
    public async Task DemoSeedingIsAbsentUnlessEnvironmentAndSettingBothEnableIt(string environment, bool enabled)
    {
        using var nonDemo = new ClinicFactory(environment, enabled);
        using var browser = nonDemo.Browser();
        var html = await browser.GetStringAsync("/account/login", TestContext.Current.CancellationToken);
        Assert.DoesNotContain(DemoAccountSeeder.Password, html);
        using var scope = nonDemo.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Empty(await db.Users.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Equal(3, await db.Roles.CountAsync(TestContext.Current.CancellationToken));
        Assert.Null(scope.ServiceProvider.GetService<DemoAccountSeeder>());
        Assert.Equal(HttpStatusCode.OK, (await BrowserForms.LoginAsync(browser, "patient")).StatusCode);
    }

    [Theory]
    [InlineData("/payments")]
    [InlineData("/medical-records")]
    [InlineData("/data/clinic.db")]
    [InlineData("/h2-console")]
    [InlineData("/unmapped")]
    public async Task UnimplementedFeaturesAndDatabaseFilesAreNotExposed(string path)
    {
        using var browser = factory.Browser();
        Assert.Equal(HttpStatusCode.NotFound, (await browser.GetAsync(path, TestContext.Current.CancellationToken)).StatusCode);
    }
}
