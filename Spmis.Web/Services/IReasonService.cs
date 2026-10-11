using Spmis.Web.Data.Entities;
using Spmis.Web.Models;

namespace Spmis.Web.Services;

public record ReasonResult(bool Success, string? Error, string? Text);

public interface IReasonService
{
    // The active reasons allowed for one context, e.g. "ScoreEdit".
    Task<IReadOnlyList<ChangeReason>> GetOptionsAsync(string context);

    // Re-checks everything on the server. On success, Text is the final
    // wording to store in activity_logs.reason.
    Task<ReasonResult> ValidateAsync(string context, ReasonInput input);
}
