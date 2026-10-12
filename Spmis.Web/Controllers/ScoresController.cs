using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Data.Auth;
using Spmis.Web.Models.ClassRecord;
using Spmis.Web.Services;

namespace Spmis.Web.Controllers;

[Authorize(Roles = RoleNames.Faculty)]
public class ScoresController : Controller
{
    private readonly IScoreEntryService _service;

    public ScoresController(IScoreEntryService service) => _service = service;

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /Scores/Entry/57   (57 = assessment id)
    [HttpGet]
    public async Task<IActionResult> Entry(int id)
    {
        var vm = await _service.LoadAsync(CurrentUserId(), id);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Entry(ScoreEntryViewModel vm)
    {
        var userId = CurrentUserId();
        if (!await _service.FillDisplayAsync(userId, vm)) return NotFound();

        var result = await _service.SaveAsync(userId, vm);
        if (result.Success)
        {
            TempData["Success"] = $"{result.Changed} score(s) saved.";
            return RedirectToAction(nameof(Entry), new { id = vm.AssessmentId });
        }

        ModelState.AddModelError(result.Field ?? "", result.Error!);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Similarity(int assessmentId, int enrollmentId)
    {
        var vm = await _service.GetSimilarityAsync(CurrentUserId(), assessmentId, enrollmentId);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Similarity(SimilarityViewModel vm)
    {
        var userId = CurrentUserId();
        var fresh = await _service.GetSimilarityAsync(userId, vm.AssessmentId, vm.EnrollmentId);
        if (fresh is null) return NotFound();

        if (ModelState.IsValid)
        {
            var result = await _service.AddSimilarityAsync(userId, vm);
            if (result.Success)
            {
                TempData["Success"] = "Similarity check recorded.";
                return RedirectToAction(nameof(Similarity),
                    new { assessmentId = vm.AssessmentId, enrollmentId = vm.EnrollmentId });
            }
            ModelState.AddModelError(result.Field ?? "", result.Error!);
        }

        fresh.Percent = vm.Percent;
        fresh.Note = vm.Note;
        return View(fresh);
    }

}