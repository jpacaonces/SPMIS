using Spmis.Web.Models.ClassRecord;

namespace Spmis.Web.Services;

public interface IAssessmentService
{
    Task<AssessmentsViewModel?> GetAsync(Guid userId, int classOfferingId);
    Task<bool> LoadFormAsync(Guid userId, AssessmentCreateViewModel vm);
    Task<AssessmentResult> CreateAsync(Guid userId, AssessmentCreateViewModel vm);

    Task<AssessmentEditViewModel?> GetEditAsync(Guid userId, int assessmentId);
    Task<bool> FillEditDisplayAsync(Guid userId, AssessmentEditViewModel vm);
    Task<AssessmentResult> UpdateAsync(Guid userId, AssessmentEditViewModel vm);

    Task<AssessmentStatusViewModel?> GetStatusViewAsync(Guid userId, int assessmentId, bool activate);
    Task<AssessmentResult> ChangeStatusAsync(Guid userId, AssessmentStatusViewModel vm);
}