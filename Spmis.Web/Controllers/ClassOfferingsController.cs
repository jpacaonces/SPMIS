using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Models.Admin;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class ClassOfferingsController : Controller
{
    private readonly IClassOfferingAdminService _classes;
    public ClassOfferingsController(IClassOfferingAdminService classes) => _classes = classes;

    public async Task<IActionResult> Index(string? q, int page = 1) =>
        View(await _classes.GetListAsync(q, page));

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new ClassOfferingCreateViewModel();
        await _classes.LoadOptionsAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClassOfferingCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _classes.CreateAsync(model);
            if (result.Success)
            {
                TempData["Success"] = "Class created with its grading components.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
        }
        await _classes.LoadOptionsAsync(model);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Status(int id)
    {
        var vm = await _classes.GetStatusViewAsync(id);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(ClassOfferingStatusViewModel model)
    {
        var result = await _classes.ChangeStatusAsync(model.ClassOfferingId, model.Activate, model.Reason);
        if (!result.Success)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
            var vm = await _classes.GetStatusViewAsync(model.ClassOfferingId);
            if (vm is null) return NotFound();
            vm.Activate = model.Activate;
            vm.Reason = model.Reason;
            return View(vm);
        }
        TempData["Success"] = model.Activate ? "Class reactivated." : "Class deactivated.";
        return RedirectToAction(nameof(Index));
    }
}