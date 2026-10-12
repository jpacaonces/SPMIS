using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Models.ClassRecord;

namespace Spmis.Web.Services;

public class ClassRecordService : IClassRecordService
{
    private readonly SpmisDbContext _db;
    private readonly IGradeCalculationService _grades;

    public ClassRecordService(SpmisDbContext db, IGradeCalculationService grades)
    {
        _db = db;
        _grades = grades;
    }

    private static string SectionCode(string program, int year, int sem, string shift, int no) =>
        $"{program}{year}{sem}{shift}{no}";

    private Task<uint?> GetFacultyIdAsync(Guid userId) =>
        _db.Faculties.AsNoTracking()
            .Where(f => f.UserId == userId && f.IsActive == true)
            .Select(f => (uint?)f.Id)
            .FirstOrDefaultAsync();

    public async Task<MyClassesViewModel> GetMyClassesAsync(Guid userId)
    {
        var vm = new MyClassesViewModel();

        var facultyId = await GetFacultyIdAsync(userId);
        if (facultyId is null)
        {
            vm.NoFacultyProfile = true;
            return vm;
        }

        var term = await _db.AcademicTerms.AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsCurrent == true);
        if (term is null) return vm;
        vm.TermLabel = $"{term.SchoolYear} Sem {term.Semester}";

        var rows = await (
            from c in _db.ClassOfferings.AsNoTracking()
            where c.FacultyId == facultyId && c.TermId == term.Id && c.IsActive == true
            join sec in _db.Sections on c.SectionId equals sec.Id
            join p in _db.Programs on sec.ProgramId equals p.Id
            join sh in _db.Shifts on sec.ShiftId equals sh.Id
            join sub in _db.Subjects on c.SubjectId equals sub.Id
            orderby p.Code, sec.YearLevel, sec.Semester, sh.Code, sec.SectionNo, sub.Code
            select new
            {
                c.Id,
                ProgramCode = p.Code,
                sec.YearLevel,
                sec.Semester,
                ShiftCode = sh.Code,
                sec.SectionNo,
                SubjectCode = sub.Code,
                SubjectTitle = sub.Title,
                c.AttendanceMode,
                c.Status,
                Enrolled = _db.Enrollments.Count(e =>
                    e.ClassOfferingId == c.Id && e.Status == "Enrolled")
            }).ToListAsync();

        vm.Items = rows.Select(r => new MyClassItem
        {
            Id = (int)r.Id,
            SectionCode = SectionCode(r.ProgramCode, r.YearLevel, r.Semester, r.ShiftCode, r.SectionNo),
            SubjectCode = r.SubjectCode,
            SubjectTitle = r.SubjectTitle,
            AttendanceMode = r.AttendanceMode,
            Enrolled = r.Enrolled,
            Status = r.Status
        }).ToList();
        return vm;
    }

    public async Task<ClassRecordViewModel?> GetClassRecordAsync(Guid userId, int classOfferingId)
    {
        if (classOfferingId <= 0) return null;

        var facultyId = await GetFacultyIdAsync(userId);
        if (facultyId is null) return null;

        var classId = (uint)classOfferingId;

        // The ownership rule lives in this query: someone else's class simply is not found.
        var header = await (
            from c in _db.ClassOfferings.AsNoTracking()
            where c.Id == classId && c.FacultyId == facultyId
            join sec in _db.Sections on c.SectionId equals sec.Id
            join p in _db.Programs on sec.ProgramId equals p.Id
            join sh in _db.Shifts on sec.ShiftId equals sh.Id
            join sub in _db.Subjects on c.SubjectId equals sub.Id
            join t in _db.AcademicTerms on c.TermId equals t.Id
            select new
            {
                ProgramCode = p.Code,
                sec.YearLevel,
                sec.Semester,
                ShiftCode = sh.Code,
                sec.SectionNo,
                SubjectCode = sub.Code,
                SubjectTitle = sub.Title,
                t.SchoolYear,
                TermSemester = t.Semester,
                c.AttendanceMode,
                c.Status,
                c.ClassStandingWeight,
                c.MajorExamWeight
            }).FirstOrDefaultAsync();
        if (header is null) return null;

        var sheet = await _grades.GetClassSheetAsync(classOfferingId);
        if (sheet is null) return null;

        return new ClassRecordViewModel
        {
            ClassOfferingId = classOfferingId,
            SectionCode = SectionCode(header.ProgramCode, header.YearLevel, header.Semester,
                header.ShiftCode, header.SectionNo),
            SubjectCode = header.SubjectCode,
            SubjectTitle = header.SubjectTitle,
            TermLabel = $"{header.SchoolYear} Sem {header.TermSemester}",
            AttendanceMode = header.AttendanceMode,
            Status = header.Status,
            ClassStandingWeight = header.ClassStandingWeight,
            MajorExamWeight = header.MajorExamWeight,
            Sheet = sheet
        };
    }
}