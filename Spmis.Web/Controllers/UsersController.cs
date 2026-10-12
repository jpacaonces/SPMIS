using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Models.Admin;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class UsersController : Controller
{
    private readonly IUserAdminService _users;

    public UsersController(IUserAdminService users)
    {
        _users = users;
    }

    public async Task<IActionResult> Index(
        string? search, string? role, string? status, int page = 1)
    {
        var model = await _users.GetPageAsync(search, role, status, page);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new UserCreateViewModel();
        await _users.LoadLookupsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _users.CreateAsync(model);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Account created for {model.Email.Trim()}.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var (key, message) in result.Errors)
                ModelState.AddModelError(key, message);
        }

        await _users.LoadLookupsAsync(model);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Status(Guid id)
    {
        var model = await _users.GetStatusViewAsync(id);
        if (model is null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(UserStatusInput input)
    {
        var activate = input.Action == "reactivate";
        var result = await _users.ChangeStatusAsync(input.UserId, activate, input.Reason);

        if (result.Succeeded)
        {
            TempData["Success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // Show the same page again, with the reason the admin already picked.
        var model = await _users.GetStatusViewAsync(input.UserId);
        if (model is null)
            return NotFound();

        model.Reason = input.Reason;
        ModelState.AddModelError(result.ErrorKey ?? string.Empty, result.Error!);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await _users.GetEditViewAsync(id);
        if (model is null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _users.UpdateAsync(model);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Saved changes for {model.Email.Trim()}.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var (key, message) in result.Errors)
                ModelState.AddModelError(key, message);
        }

        // Role, flags and dropdowns are not posted: rebuild them.
        await _users.PrepareEditViewAsync(model);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        var model = await _users.GetPasswordResetViewAsync(id);
        if (model is null)
            return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(UserPasswordResetViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _users.ResetPasswordAsync(model);
            if (result.Succeeded)
            {
                TempData["Success"] = "Password reset. Give the new temporary password to the user in person.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var (key, message) in result.Errors)
                ModelState.AddModelError(key, message);
        }

        // Name and email are not posted: reload them for display.
        var display = await _users.GetPasswordResetViewAsync(model.UserId);
        if (display is null)
            return NotFound();

        model.FullName = display.FullName;
        model.Email = display.Email;
        return View(model);
    }
}