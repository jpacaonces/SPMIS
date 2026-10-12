using Spmis.Web.Models;
using Spmis.Web.Models.Admin;

namespace Spmis.Web.Services;

public record SectionResult(bool Success, string? Field, string? Error);

public interface ISectionAdminService
{
    Task<List<SectionListItem>> GetListAsync();
    Task LoadOptionsAsync(SectionCreateViewModel model);
    Task<SectionResult> CreateAsync(SectionCreateViewModel model);
    Task<SectionStatusViewModel?> GetStatusViewAsync(int id);
    Task<SectionResult> ChangeStatusAsync(int id, bool activate, ReasonInput reasonInput);
}
