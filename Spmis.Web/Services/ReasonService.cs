using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Models;

namespace Spmis.Web.Services;

public class ReasonService : IReasonService
{
    private const string OtherPrefix = "Other";
    private const int MinOtherDetails = 10;

    private readonly SpmisDbContext _db;

    public ReasonService(SpmisDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ChangeReason>> GetOptionsAsync(string context)
    {
        return await _db.ChangeReasons
            .AsNoTracking()
            .Where(r => r.Context == context && r.IsActive == true)
            .OrderBy(r => r.Id)
            .ToListAsync();
    }

    public async Task<ReasonResult> ValidateAsync(string context, ReasonInput input)
    {
        if (input.ReasonId is null)
            return new ReasonResult(false, "Choose a reason.", null);

        // The submitted id must be an ACTIVE reason of THIS context.
        // A tampered form cannot use a reason from another context.
        var options = await GetOptionsAsync(context);
        var reason = options.FirstOrDefault(o => o.Id == input.ReasonId);
        if (reason is null)
            return new ReasonResult(false, "Invalid reason.", null);

        var details = (input.Details ?? string.Empty).Trim();
        if (details.Length > 400)
            return new ReasonResult(false, "Details can be at most 400 characters.", null);

        if (reason.Text.StartsWith(OtherPrefix, StringComparison.Ordinal)
            && details.Length < MinOtherDetails)
        {
            return new ReasonResult(false,
                $"Describe the reason in at least {MinOtherDetails} characters.", null);
        }

        var text = details.Length == 0 ? reason.Text : $"{reason.Text}: {details}";
        return new ReasonResult(true, null, text);
    }
}
