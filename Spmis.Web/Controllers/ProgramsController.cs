using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Models.Admin;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class ProgramsController : Controller
{
    private readonly IProgramAdminService _programs;
    public ProgramsController(IProgramAdminService programs) => _programs = programs;

    public async Task<IActionResult> Index() => View(await _programs.GetListAsync());

    [HttpGet]
    public async Task<IActionResult> Create() =>
        View(new ProgramCreateViewModel { Departments = await _programs.GetDepartmentOptionsAsync() });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProgramCreateViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _programs.CreateAsync(model);
            if (result.Success)
            {
                TempData["Success"] = "Program created.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
        }
        model.Departments = await _programs.GetDepartmentOptionsAsync();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Status(int id)
    {
        var vm = await _programs.GetStatusViewAsync(id);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(ProgramStatusViewModel model)
    {
        var result = await _programs.ChangeStatusAsync(model.ProgramId, model.Activate, model.Reason);
        if (!result.Success)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
            var vm = await _programs.GetStatusViewAsync(model.ProgramId);
            if (vm is null) return NotFound();
            vm.Activate = model.Activate;
            vm.Reason = model.Reason;
            return View(vm);
        }
        TempData["Success"] = model.Activate ? "Program reactivated." : "Program deactivated.";
        return RedirectToAction(nameof(Index));
    }
}