using Spmis.Web.Models.ClassRecord;

namespace Spmis.Web.Services;

public interface IExcuseService
{
    Task<ExcuseListViewModel> GetListAsync(Guid userId, string? filter);
    Task<ExcuseDecideViewModel?> GetDecideAsync(Guid userId, int decisionId);
    Task<ExcuseResult> DecideAsync(Guid userId, ExcuseDecideViewModel vm);
    Task<(string Stored, string Original)?> GetFileInfoAsync(Guid userId, int decisionId);
}