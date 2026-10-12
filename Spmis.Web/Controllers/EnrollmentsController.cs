using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Models.Admin;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class EnrollmentsController : Controller
{
    private readonly IEnrollmentAdminService _enrollments;
    public EnrollmentsController(IEnrollmentAdminService enrollments) => _enrollments = enrollments;

    [HttpGet]
    public async Task<IActionResult> Roster(int id)
    {
        var vm = await _enrollments.GetRosterAsync(id);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int id, EnrollInput enroll)
    {
        // The query/route "id" is the class; the form only supplies the student number.
        if (!ModelState.IsValid)
        {
            var bad = await _enrollments.GetRosterAsync(id);
            if (bad is null) return NotFound();
            bad.Enroll = enroll;
            return View(nameof(Roster), bad);
        }

        var result = await _enrollments.EnrollAsync(id, enroll.StudentNo);
        if (!result.Success)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
            var vm = await _enrollments.GetRosterAsync(id);
            if (vm is null) return NotFound();
            vm.Enroll = enroll;
            return View(nameof(Roster), vm);
        }
        TempData["Success"] = "Student enrolled.";
        return RedirectToAction(nameof(Roster), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Status(int id, string to)
    {
        var vm = await _enrollments.GetStatusViewAsync(id, to);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(EnrollmentStatusViewModel model)
    {
        var result = await _enrollments.ChangeStatusAsync(model.EnrollmentId, model.NewStatus, model.Reason);
        if (!result.Success)
        {
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
            var vm = await _enrollments.GetStatusViewAsync(model.EnrollmentId, model.NewStatus);
            if (vm is null) return NotFound();
            vm.Reason = model.Reason;
            return View(vm);
        }
        TempData["Success"] = model.NewStatus == "Dropped" ? "Student dropped." : "Student reinstated.";
        return RedirectToAction(nameof(Roster), new { id = model.ClassOfferingId });
    }

    [HttpGet]
    public async Task<IActionResult> Bulk()
    {
        var vm = new BulkEnrollViewModel();
        await _enrollments.LoadBulkOptionsAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Bulk(BulkEnrollViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _enrollments.BulkEnrollAsync(model);
            if (result.Success)
            {
                var msg = $"Created {result.Created} enrollment(s): {result.Students} student(s) in " +
                          $"{result.Classes} class(es). Skipped {result.Skipped} already on record.";
                if (result.DroppedStudents is { Count: > 0 })
                    msg += $" Dropped earlier (reinstate on the roster): {string.Join(", ", result.DroppedStudents)}.";
                TempData["Success"] = msg;
                return RedirectToAction("Index", "ClassOfferings");
            }
            ModelState.AddModelError(result.Field ?? string.Empty, result.Error!);
        }
        await _enrollments.LoadBulkOptionsAsync(model);
        return View(model);
    }

}