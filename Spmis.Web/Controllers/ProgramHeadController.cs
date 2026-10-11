using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.ProgramHead)]
public class ProgramHeadController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}