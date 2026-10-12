using Spmis.Web.Models.Analytics;

namespace Spmis.Web.Services;

public interface IProgramAnalyticsService
{
    Task<ProgramDashboardViewModel> GetDashboardAsync(Guid userId);

    // null = the class does not exist or is not in this head's program.
    Task<ClassRiskViewModel?> GetClassAsync(Guid userId, int classOfferingId);
}