using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Models;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogger _audit;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditLogger audit)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _audit = audit;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        const string invalid = "Invalid email or password.";

        var user = await _userManager.FindByNameAsync(model.Email);
        if (user is null)
        {
            // No such account: no user id to attach, so keep the typed email.
            await _audit.AddAndSaveAsync("LoginFailed", "User", null,
                newValues: new { email = Truncate(model.Email, 100) },
                reason: "Unknown email");
            ModelState.AddModelError(string.Empty, invalid);
            return View(model);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault();

        var result = await _signInManager.CheckPasswordSignInAsync(
            user, model.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            await _audit.AddAndSaveAsync("LoginFailed", "User", user.Id.ToString(),
                reason: "Locked out", actingUserId: user.Id, actingRole: role);
            ModelState.AddModelError(string.Empty,
                "Too many failed attempts. Try again in 15 minutes.");
            return View(model);
        }

        if (!result.Succeeded)
        {
            await _audit.AddAndSaveAsync("LoginFailed", "User", user.Id.ToString(),
                reason: "Wrong password", actingUserId: user.Id, actingRole: role);
            ModelState.AddModelError(string.Empty, invalid);
            return View(model);
        }

        if (!user.IsActive)
        {
            await _audit.AddAndSaveAsync("LoginFailed", "User", user.Id.ToString(),
                reason: "Inactive account", actingUserId: user.Id, actingRole: role);
            ModelState.AddModelError(string.Empty,
                "This account is inactive. Contact the administrator.");
            return View(model);
        }

        await _audit.AddAndSaveAsync("LoginSucceeded", "User", user.Id.ToString(),
            actingUserId: user.Id, actingRole: role);

        await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}