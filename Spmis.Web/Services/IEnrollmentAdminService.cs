using Spmis.Web.Models;
using Spmis.Web.Models.Admin;

namespace Spmis.Web.Services;

public record EnrollmentResult(bool Success, string? Field, string? Error);

public interface IEnrollmentAdminService
{
    Task<RosterViewModel?> GetRosterAsync(int classOfferingId);
    Task<EnrollmentResult> EnrollAsync(int classOfferingId, string studentNo);
    Task<EnrollmentStatusViewModel?> GetStatusViewAsync(int enrollmentId, string newStatus);
    Task<EnrollmentResult> ChangeStatusAsync(int enrollmentId, string newStatus, ReasonInput reasonInput);

    public record BulkEnrollResult(
    bool Success, string? Field, string? Error,
    int Created = 0, int Skipped = 0, int Students = 0, int Classes = 0,
    List<string>? DroppedStudents = null);

    Task LoadBulkOptionsAsync(BulkEnrollViewModel model);
    Task<BulkEnrollResult> BulkEnrollAsync(BulkEnrollViewModel model);

}