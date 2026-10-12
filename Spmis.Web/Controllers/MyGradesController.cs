using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Student)]
public class MyGradesController : Controller
{
    private readonly IStudentPortalService _service;

    public MyGradesController(IStudentPortalService service) => _service = service;

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index() =>
        View(await _service.GetMyClassesAsync(CurrentUserId()));

    // GET /MyGrades/Show/123   (123 = enrollment id)
    public async Task<IActionResult> Show(int id)
    {
        var vm = await _service.GetClassDetailAsync(CurrentUserId(), id);
        return vm is null ? NotFound() : View(vm);
    }
}