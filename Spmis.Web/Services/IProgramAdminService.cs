using Microsoft.AspNetCore.Mvc.Rendering;
using Spmis.Web.Models;
using Spmis.Web.Models.Admin;

namespace Spmis.Web.Services;

public record ProgramResult(bool Success, string? Field, string? Error);

public interface IProgramAdminService
{
    Task<List<ProgramListItem>> GetListAsync();
    Task<List<SelectListItem>> GetDepartmentOptionsAsync();
    Task<ProgramResult> CreateAsync(ProgramCreateViewModel model);
    Task<ProgramStatusViewModel?> GetStatusViewAsync(int id);
    Task<ProgramResult> ChangeStatusAsync(int id, bool activate, ReasonInput reasonInput);
}