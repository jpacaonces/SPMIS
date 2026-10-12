using Spmis.Web.Models;
using Spmis.Web.Models.Admin;

namespace Spmis.Web.Services;

public record ClassOfferingResult(bool Success, string? Field, string? Error);

public interface IClassOfferingAdminService
{
    Task<ClassOfferingListViewModel> GetListAsync(string? search, int page);
    Task LoadOptionsAsync(ClassOfferingCreateViewModel model);
    Task<ClassOfferingResult> CreateAsync(ClassOfferingCreateViewModel model);
    Task<ClassOfferingStatusViewModel?> GetStatusViewAsync(int id);
    Task<ClassOfferingResult> ChangeStatusAsync(int id, bool activate, ReasonInput reasonInput);
}