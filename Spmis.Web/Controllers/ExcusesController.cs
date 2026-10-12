using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Models.ClassRecord;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Faculty)]
public class ExcusesController : Controller
{
    private readonly IExcuseService _service;
    private readonly IExcuseFileStore _files;

    public ExcusesController(IExcuseService service, IExcuseFileStore files)
    {
        _service = service;
        _files = files;
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index(string? filter) =>
        View(await _service.GetListAsync(CurrentUserId(), filter));

    // GET /Excuses/Download/57   (57 = decision id)
    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var info = await _service.GetFileInfoAsync(CurrentUserId(), id);
        if (info is null) return NotFound();

        var stream = _files.OpenRead(info.Value.Stored);
        if (stream is null) return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(stream, IExcuseFileStore.ContentTypeFor(info.Value.Stored), info.Value.Original);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(ExcuseDecideViewModel vm)
    {
        var userId = CurrentUserId();
        var fresh = await _service.GetDecideAsync(userId, vm.DecisionId);
        if (fresh is null) return NotFound();

        var result = await _service.DecideAsync(userId, vm);
        if (result.Success)
        {
            TempData["Success"] = vm.Approve == true ? "Letter approved." : "Letter rejected.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(result.Field ?? "", result.Error!);
        fresh.Approve = vm.Approve;
        fresh.Reason = vm.Reason;
        return View(fresh);
    }
}