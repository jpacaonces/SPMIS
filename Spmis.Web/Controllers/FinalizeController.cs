using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Faculty)]
public class FinalizeController : Controller
{
    private readonly IFinalizeService _service;

    public FinalizeController(IFinalizeService service) => _service = service;

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /Finalize/Review/12   (12 = class offering id)
    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var vm = await _service.GetReviewAsync(CurrentUserId(), id);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, bool confirmed)
    {
        var result = await _service.FinalizeAsync(CurrentUserId(), id, confirmed);
        if (result.Success)
            TempData["Success"] = "The class was finalized. Its grades are now locked.";
        else
            TempData["Error"] = result.Error;

        return RedirectToAction(nameof(Review), new { id });
    }
}