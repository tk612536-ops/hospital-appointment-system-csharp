using System.Net;
using System.Text.RegularExpressions;
using HospitalAppointments.Infrastructure.Demo;

namespace HospitalAppointments.IntegrationTests;

internal static class BrowserForms
{
    internal static async Task<string> TokenAsync(HttpClient browser, string path)
    {
        var page = await browser.GetAsync(path, TestContext.Current.CancellationToken);
        page.EnsureSuccessStatusCode();
        var html = await page.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "The real MVC form must contain an antiforgery token.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    internal static async Task<HttpResponseMessage> LoginAsync(HttpClient browser, string username, string password = DemoAccountSeeder.Password,
        string? returnUrl = null)
    {
        var token = await TokenAsync(browser, "/account/login");
        return await browser.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = username, ["Password"] = password, ["__RequestVerificationToken"] = token,
            ["ReturnUrl"] = returnUrl ?? ""
        }), TestContext.Current.CancellationToken);
    }
}
