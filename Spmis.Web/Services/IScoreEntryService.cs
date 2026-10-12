using Spmis.Web.Models.ClassRecord;

namespace Spmis.Web.Services;

public interface IScoreEntryService
{
    // null = not this faculty member's assessment.
    Task<ScoreEntryViewModel?> LoadAsync(Guid userId, int assessmentId);

    // Refills header and student names on a model that came back from a POST.
    Task<bool> FillDisplayAsync(Guid userId, ScoreEntryViewModel vm);

    Task<ScoreSaveResult> SaveAsync(Guid userId, ScoreEntryViewModel vm);

    Task<SimilarityViewModel?> GetSimilarityAsync(Guid userId, int assessmentId, int enrollmentId);
    Task<ScoreSaveResult> AddSimilarityAsync(Guid userId, SimilarityViewModel vm);
}