using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Models.Admin;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class SectionsController : Controller
{
    private readonly ISectionAdminService _sections;
    public SectionsController(ISectionAdminService sections) => _sections = sections;

    public async Task<IActionResult> Index() => View(await _sections.GetListAsync());

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new SectionCreateViewModel();
        await _sections.LoadOptionsAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SectionCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _sections.CreateAsync(model);
            if (result.Success)
            {
                TempData["Success"] = "Section created.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
        }
        await _sections.LoadOptionsAsync(model);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Status(int id)
    {
        var vm = await _sections.GetStatusViewAsync(id);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(SectionStatusViewModel model)
    {
        var result = await _sections.ChangeStatusAsync(model.SectionId, model.Activate, model.Reason);
        if (!result.Success)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
            var vm = await _sections.GetStatusViewAsync(model.SectionId);
            if (vm is null) return NotFound();
            vm.Activate = model.Activate;
            vm.Reason = model.Reason;
            return View(vm);
        }
        TempData["Success"] = model.Activate ? "Section reactivated." : "Section deactivated.";
        return RedirectToAction(nameof(Index));
    }
}