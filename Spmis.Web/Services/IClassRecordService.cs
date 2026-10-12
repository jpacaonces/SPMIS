using Spmis.Web.Models.ClassRecord;

namespace Spmis.Web.Services;

public interface IClassRecordService
{
    Task<MyClassesViewModel> GetMyClassesAsync(Guid userId);

    // Null when the class does not exist OR is not taught by this faculty member.
    Task<ClassRecordViewModel?> GetClassRecordAsync(Guid userId, int classOfferingId);
}