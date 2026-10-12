using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Models.Portal;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Student)]
public class MyExcusesController : Controller
{
    private readonly IStudentExcuseService _service;
    private readonly IExcuseFileStore _files;

    public MyExcusesController(IStudentExcuseService service, IExcuseFileStore files)
    {
        _service = service;
        _files = files;
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index() =>
        View(await _service.GetMyLettersAsync(CurrentUserId()));

    [HttpGet]
    public IActionResult Submit() => View(new ExcuseSubmitViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(6_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6_000_000)]
    public async Task<IActionResult> Submit(ExcuseSubmitViewModel vm)
    {
        if (ModelState.IsValid)
        {
            var result = await _service.SubmitAsync(CurrentUserId(), vm);
            if (result.Success)
            {
                TempData["Success"] = "Your letter was submitted. Each teacher will decide for their own class.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(result.Field ?? "", result.Error!);
        }
        return View(vm);
    }

    // GET /MyExcuses/Download/12   (12 = letter id)
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
}