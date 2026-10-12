using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class GradeCheckController : Controller
{
    private readonly IGradeCalculationService _grades;
    public GradeCheckController(IGradeCalculationService grades) => _grades = grades;

    // /GradeCheck/Class/1?take=3
    public async Task<IActionResult> Class(int id, int take = 3)
    {
        var sheet = await _grades.GetClassSheetAsync(id);
        if (sheet is null) return NotFound();

        var inv = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        foreach (var s in sheet.Students.Take(Math.Clamp(take, 1, 50)))
        {
            sb.Append($"{id} {s.StudentNo} ");
            foreach (var p in s.Semester.Periods)
                sb.Append($"{p.Status.ToString()[..4]}:{p.RawGrade?.ToString("0.0000", inv) ?? "-"} ");

            var sem = s.Semester;
            sb.AppendLine(sem.RawGrade is null
                ? "| sem NoData"
                : $"| sem {sem.RawGrade.Value.ToString("0.00", inv)} " +
                  $"{sem.ReportedGrade!.Value.ToString("0.00", inv)} {sem.Status} {sem.PeriodsUsed}");
        }
        return Content(sb.ToString(), "text/plain");
    }
}