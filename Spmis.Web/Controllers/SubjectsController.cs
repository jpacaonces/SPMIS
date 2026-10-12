using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Models.Admin;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class SubjectsController : Controller
{
    private readonly ISubjectAdminService _subjects;
    public SubjectsController(ISubjectAdminService subjects) => _subjects = subjects;

    public async Task<IActionResult> Index() => View(await _subjects.GetListAsync());

    [HttpGet]
    public IActionResult Create() => View(new SubjectCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubjectCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _subjects.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
            return View(model);
        }
        TempData["Success"] = "Subject created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Status(int id)
    {
        var vm = await _subjects.GetStatusViewAsync(id);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(SubjectStatusViewModel model)
    {
        var result = await _subjects.ChangeStatusAsync(model.SubjectId, model.Activate, model.Reason);
        if (!result.Success)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
            var vm = await _subjects.GetStatusViewAsync(model.SubjectId);
            if (vm is null) return NotFound();
            vm.Activate = model.Activate;
            vm.Reason = model.Reason;
            return View(vm);
        }
        TempData["Success"] = model.Activate ? "Subject reactivated." : "Subject deactivated.";
        return RedirectToAction(nameof(Index));
    }
}