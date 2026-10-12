using Spmis.Web.Models;
using Spmis.Web.Models.Admin;

namespace Spmis.Web.Services;

public record TermResult(bool Success, string? Field, string? Error);

public interface ITermAdminService
{
    Task<List<TermListItem>> GetListAsync();
    Task<TermResult> CreateAsync(TermCreateViewModel model);
    Task<TermCurrentViewModel?> GetSetCurrentViewAsync(int id);
    Task<TermResult> SetCurrentAsync(int id, ReasonInput reasonInput);
}