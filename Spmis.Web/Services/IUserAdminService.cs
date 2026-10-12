using Spmis.Web.Models;
using Spmis.Web.Models.Admin;

namespace Spmis.Web.Services;

public record CreateUserResult(
    bool Succeeded,
    List<(string Key, string Message)> Errors,
    Guid? UserId);

public record StatusChangeResult(
    bool Succeeded,
    string? ErrorKey,
    string? Error,
    string? Message);

public record UpdateUserResult(
    bool Succeeded,
    List<(string Key, string Message)> Errors);

public interface IUserAdminService
{
    Task<UserListViewModel> GetPageAsync(
        string? search, string? role, string? status, int page);

    Task LoadLookupsAsync(UserCreateViewModel model);

    Task<CreateUserResult> CreateAsync(UserCreateViewModel model);

    Task<UserStatusViewModel?> GetStatusViewAsync(Guid id);

    Task<StatusChangeResult> ChangeStatusAsync(
        Guid id, bool activate, ReasonInput reason);

    Task<UserEditViewModel?> GetEditViewAsync(Guid id);

    Task PrepareEditViewAsync(UserEditViewModel model);

    Task<UpdateUserResult> UpdateAsync(UserEditViewModel model);

    // Null when the account does not exist.
    Task<UserPasswordResetViewModel?> GetPasswordResetViewAsync(Guid id);

    Task<UpdateUserResult> ResetPasswordAsync(UserPasswordResetViewModel model);
}