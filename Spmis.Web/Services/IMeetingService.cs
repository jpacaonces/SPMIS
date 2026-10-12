using Spmis.Web.Models.ClassRecord;

namespace Spmis.Web.Services;

public interface IMeetingService
{
    Task<MeetingsViewModel?> GetAsync(Guid userId, int classOfferingId);
    Task<bool> LoadFormAsync(Guid userId, MeetingCreateViewModel vm);
    Task<MeetingResult> CreateAsync(Guid userId, MeetingCreateViewModel vm);
    Task<MeetingStatusViewModel?> GetStatusViewAsync(Guid userId, int meetingId, bool activate);
    Task<MeetingResult> ChangeStatusAsync(Guid userId, MeetingStatusViewModel vm);
}