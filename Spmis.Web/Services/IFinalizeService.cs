using Spmis.Web.Models.ClassRecord;

namespace Spmis.Web.Services;

public interface IFinalizeService
{
    // null = not this faculty member's class.
    Task<FinalizeReviewViewModel?> GetReviewAsync(Guid userId, int classOfferingId);

    Task<FinalizeResult> FinalizeAsync(Guid userId, int classOfferingId, bool confirmed);
}