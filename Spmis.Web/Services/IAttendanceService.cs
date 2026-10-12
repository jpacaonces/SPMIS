using Spmis.Web.Models.ClassRecord;

namespace Spmis.Web.Services;

public interface IAttendanceService
{
    // null = not this faculty member's meeting, or not a Meetings-mode class.
    Task<AttendanceViewModel?> LoadAsync(Guid userId, int meetingId);
    Task<bool> FillDisplayAsync(Guid userId, AttendanceViewModel vm);
    Task<AttendanceSaveResult> SaveAsync(Guid userId, AttendanceViewModel vm);
}