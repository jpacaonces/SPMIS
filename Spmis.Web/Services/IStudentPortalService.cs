using Spmis.Web.Models.Portal;

namespace Spmis.Web.Services;

public interface IStudentPortalService
{
    Task<MyClassesViewModel> GetMyClassesAsync(Guid userId);

    // null = not this student's enrollment.
    Task<MyClassDetailViewModel?> GetClassDetailAsync(Guid userId, int enrollmentId);
}