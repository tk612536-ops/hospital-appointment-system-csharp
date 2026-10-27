using HospitalAppointments.Application.Services;
using HospitalAppointments.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalAppointments.Web.Controllers;

[Route("account")]
public sealed class AccountController(AccountService accounts) : Controller
{
    [AllowAnonymous, HttpGet("login")]
    public IActionResult Login(string? returnUrl) => View(new LoginInput { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> Login(LoginInput input)
    {
        if (ModelState.IsValid && await accounts.SignInAsync(input.Username, input.Password))
            return LocalRedirect(Url.IsLocalUrl(input.ReturnUrl) ? input.ReturnUrl! : "/workspace");

        ModelState.AddModelError(string.Empty, "Giriş yapılamadı. Kullanıcı adı ve parolanızı kontrol edin; gerekirse birkaç dakika sonra yeniden deneyin.");
        input.Password = string.Empty;
        ModelState.Remove(nameof(LoginInput.Password));
        return View(input);
    }

    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await accounts.SignOutAsync();
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    [AllowAnonymous, HttpGet("access-denied")]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
