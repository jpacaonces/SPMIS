using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Models.Admin;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class TermsController : Controller
{
    private readonly ITermAdminService _terms;
    public TermsController(ITermAdminService terms) => _terms = terms;

    public async Task<IActionResult> Index() => View(await _terms.GetListAsync());

    [HttpGet]
    public IActionResult Create() => View(new TermCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TermCreateViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _terms.CreateAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
            return View(model);
        }
        TempData["Success"] = "Term created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> SetCurrent(int id)
    {
        var vm = await _terms.GetSetCurrentViewAsync(id);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCurrent(TermCurrentViewModel model)
    {
        var result = await _terms.SetCurrentAsync(model.TermId, model.Reason);
        if (!result.Success)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
            var vm = await _terms.GetSetCurrentViewAsync(model.TermId);
            if (vm is null) return NotFound();
            vm.Reason = model.Reason;
            return View(vm);
        }
        TempData["Success"] = "Current term changed.";
        return RedirectToAction(nameof(Index));
    }
}