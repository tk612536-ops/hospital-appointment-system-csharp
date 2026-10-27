using HospitalAppointments.Application.Security;
using HospitalAppointments.Application.Services;
using HospitalAppointments.Domain.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HospitalAppointments.Web.Controllers;

[Authorize]
public sealed class WorkspaceController(AccountService accounts) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try { return View(await accounts.GetProfileAsync(cancellationToken)); }
        catch (AccountAccessDeniedException) { return Forbid(); }
    }

    [Authorize(Policy = RoleNames.Patient), HttpGet("patient")]
    public Task<IActionResult> Patient(CancellationToken token) => ShowAsync(AccountRole.Patient, token);

    [Authorize(Policy = RoleNames.Staff), HttpGet("staff")]
    public Task<IActionResult> Staff(CancellationToken token) => ShowAsync(AccountRole.Staff, token);

    [Authorize(Policy = RoleNames.Manager), HttpGet("manager")]
    public Task<IActionResult> Manager(CancellationToken token) => ShowAsync(AccountRole.Manager, token);

    private async Task<IActionResult> ShowAsync(AccountRole role, CancellationToken token)
    {
        try
        {
            var profile = await accounts.GetWorkspaceAsync(role, token);
            ViewData["Role"] = role;
            return View("Role", profile);
        }
        catch (AccountAccessDeniedException) { return Forbid(); }
    }
}
