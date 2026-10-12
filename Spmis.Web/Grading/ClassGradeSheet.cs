namespace Spmis.Web.Grading;

public sealed record StudentGradeRow(
    int EnrollmentId,
    string StudentNo,
    string Name,
    SemesterResult Semester);

public sealed record ClassGradeSheet(
    int ClassOfferingId,
    IReadOnlyList<StudentGradeRow> Students);