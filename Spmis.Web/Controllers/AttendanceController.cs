using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Models.ClassRecord;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Faculty)]
public class AttendanceController : Controller
{
    private readonly IAttendanceService _service;

    public AttendanceController(IAttendanceService service) => _service = service;

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /Attendance/Take/310   (310 = meeting id)
    [HttpGet]
    public async Task<IActionResult> Take(int id)
    {
        var vm = await _service.LoadAsync(CurrentUserId(), id);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Take(AttendanceViewModel vm)
    {
        var userId = CurrentUserId();
        if (!await _service.FillDisplayAsync(userId, vm)) return NotFound();

        var result = await _service.SaveAsync(userId, vm);
        if (result.Success)
        {
            TempData["Success"] = $"{result.Changed} mark(s) saved.";
            return RedirectToAction(nameof(Take), new { id = vm.MeetingId });
        }

        ModelState.AddModelError(result.Field ?? "", result.Error!);
        return View(vm);
    }
}