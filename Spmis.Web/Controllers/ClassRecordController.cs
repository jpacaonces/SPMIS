using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Faculty)]
public class ClassRecordController : Controller
{
    private readonly IClassRecordService _records;
    public ClassRecordController(IClassRecordService records) => _records = records;

    private Guid? CurrentUserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public async Task<IActionResult> Index()
    {
        var userId = CurrentUserId();
        if (userId is null) return Forbid();
        return View(await _records.GetMyClassesAsync(userId.Value));
    }

    public async Task<IActionResult> Show(int id)
    {
        var userId = CurrentUserId();
        if (userId is null) return Forbid();

        var vm = await _records.GetClassRecordAsync(userId.Value, id);
        return vm is null ? NotFound() : View(vm);
    }
}