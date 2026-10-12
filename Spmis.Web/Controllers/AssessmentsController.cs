using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Models.ClassRecord;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Faculty)]
public class AssessmentsController : Controller
{
    private readonly IAssessmentService _service;

    public AssessmentsController(IAssessmentService service) => _service = service;

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /Assessments/Index/12  (12 = class offering id)
    public async Task<IActionResult> Index(int id)
    {
        var vm = await _service.GetAsync(CurrentUserId(), id);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int id)
    {
        var vm = new AssessmentCreateViewModel { ClassOfferingId = id };
        return await _service.LoadFormAsync(CurrentUserId(), vm) ? View(vm) : NotFound();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssessmentCreateViewModel vm)
    {
        var userId = CurrentUserId();
        if (!await _service.LoadFormAsync(userId, vm)) return NotFound();

        if (ModelState.IsValid)
        {
            var result = await _service.CreateAsync(userId, vm);
            if (result.Success)
            {
                TempData["Success"] = $"Assessment “{vm.Title!.Trim()}” was added.";
                return RedirectToAction(nameof(Index), new { id = vm.ClassOfferingId });
            }
            ModelState.AddModelError(result.Field ?? "", result.Error!);
        }
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _service.GetEditAsync(CurrentUserId(), id);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AssessmentEditViewModel vm)
    {
        var userId = CurrentUserId();
        if (!await _service.FillEditDisplayAsync(userId, vm)) return NotFound();

        if (ModelState.IsValid)
        {
            var result = await _service.UpdateAsync(userId, vm);
            if (result.Success)
            {
                TempData["Success"] = $"Assessment “{vm.Title!.Trim()}” was updated.";
                return RedirectToAction(nameof(Index), new { id = vm.ClassOfferingId });
            }
            ModelState.AddModelError(result.Field ?? "", result.Error!);
        }
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Status(int id, bool activate)
    {
        var vm = await _service.GetStatusViewAsync(CurrentUserId(), id, activate);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(AssessmentStatusViewModel vm)
    {
        var userId = CurrentUserId();
        var fresh = await _service.GetStatusViewAsync(userId, vm.AssessmentId, vm.Activate);
        if (fresh is null) return NotFound();

        var result = await _service.ChangeStatusAsync(userId, vm);
        if (result.Success)
        {
            TempData["Success"] = vm.Activate
                ? $"“{fresh.Title}” was reactivated."
                : $"“{fresh.Title}” was deactivated.";
            return RedirectToAction(nameof(Index), new { id = fresh.ClassOfferingId });
        }

        ModelState.AddModelError(result.Field ?? "", result.Error!);
        fresh.Reason = vm.Reason;
        return View(fresh);
    }

}