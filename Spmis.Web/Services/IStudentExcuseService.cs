using Spmis.Web.Models.Portal;

namespace Spmis.Web.Services;

public interface IStudentExcuseService
{
    Task<List<MyLetterItem>> GetMyLettersAsync(Guid userId);
    Task<LetterResult> SubmitAsync(Guid userId, ExcuseSubmitViewModel vm);

    // null = not this student's letter.
    Task<(string Stored, string Original)?> GetFileInfoAsync(Guid userId, int letterId);
}