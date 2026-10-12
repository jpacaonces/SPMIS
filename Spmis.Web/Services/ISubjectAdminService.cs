using Spmis.Web.Models;
using Spmis.Web.Models.Admin;

namespace Spmis.Web.Services;

public record SubjectResult(bool Success, string? Field, string? Error);

public interface ISubjectAdminService
{
    Task<List<SubjectListItem>> GetListAsync();
    Task<SubjectResult> CreateAsync(SubjectCreateViewModel model);
    Task<SubjectStatusViewModel?> GetStatusViewAsync(int id);
    Task<SubjectResult> ChangeStatusAsync(int id, bool activate, ReasonInput reasonInput);
}