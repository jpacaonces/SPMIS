using Spmis.Web.Grading;

namespace Spmis.Web.Services;

public interface IGradeCalculationService
{
    // Grades of every ENROLLED student of one class. Null if the class does not exist.
    Task<ClassGradeSheet?> GetClassSheetAsync(int classOfferingId);

    // Grade of one enrollment (null if it does not exist or the student is dropped).
    Task<StudentGradeRow?> GetStudentGradeAsync(int enrollmentId);
}