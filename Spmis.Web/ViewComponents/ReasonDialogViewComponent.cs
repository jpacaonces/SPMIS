using Microsoft.AspNetCore.Mvc;
using Spmis.Web.Models;
using Spmis.Web.Services;

namespace Spmis.Web.ViewComponents;

public class ReasonDialogViewComponent : ViewComponent
{
    private readonly IReasonService _reasons;

    public ReasonDialogViewComponent(IReasonService reasons)
    {
        _reasons = reasons;
    }

    public async Task<IViewComponentResult> InvokeAsync(
        string context, string formId, ReasonInput? selected = null)
    {
        var model = new ReasonDialogViewModel
        {
            FormId = formId,
            Options = await _reasons.GetOptionsAsync(context),
            Selected = selected ?? new ReasonInput(),
            ReasonError = FirstError("Reason.ReasonId"),
            DetailsError = FirstError("Reason.Details")
        };
        return View(model);
    }

    private string? FirstError(string key) =>
        ViewContext.ViewData.ModelState.TryGetValue(key, out var entry)
            ? entry.Errors.FirstOrDefault()?.ErrorMessage
            : null;
}
