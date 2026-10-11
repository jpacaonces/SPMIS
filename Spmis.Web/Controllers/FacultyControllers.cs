using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Faculty)]
public class FacultyController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}