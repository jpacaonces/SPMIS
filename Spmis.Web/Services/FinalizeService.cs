using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Grading;
using Spmis.Web.Models.ClassRecord;

namespace Spmis.Web.Services;

public class FinalizeService : IFinalizeService
{
    private readonly SpmisDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IGradeCalculationService _grades;
    private readonly ILogger<FinalizeService> _logger;

    public FinalizeService(SpmisDbContext db, IAuditLogger audit,
        IGradeCalculationService grades, ILogger<FinalizeService> logger)
    {
        _db = db;
        _audit = audit;
        _grades = grades;
        _logger = logger;
    }

    // Layer 2 of authorization: the class must belong to THIS faculty member.
    // Tracked, because FinalizeAsync changes its status.
    private async Task<ClassOffering?> OwnedClassAsync(Guid userId, int classId)
    {
        var facultyId = await _db.Faculties.AsNoTracking()
            .Where(f => f.UserId == userId && f.IsActive == true)
            .Select(f => (uint?)f.Id).FirstOrDefaultAsync();
        if (facultyId is null) return null;

        var id = (uint)classId;
        return await _db.ClassOfferings.FirstOrDefaultAsync(c =>
            c.Id == id && c.FacultyId == facultyId && c.IsActive == true);
    }

    private Task<string> HeaderAsync(ClassOffering c) =>
        _db.Subjects.AsNoTracking().Where(s => s.Id == c.SubjectId)
            .Select(s => s.Code + " - " + s.Title).FirstAsync();

    public async Task<FinalizeReviewViewModel?> GetReviewAsync(Guid userId, int classOfferingId)
    {
        var c = await OwnedClassAsync(userId, classOfferingId);
        if (c is null) return null;

        var vm = new FinalizeReviewViewModel
        {
            ClassOfferingId = classOfferingId,
            Header = await HeaderAsync(c),
            IsFinalized = c.Status == "Finalized"
        };

        if (vm.IsFinalized)
        {
            // Show the saved snapshot, not a fresh calculation.
            var snap = await (
                from f in _db.FinalGrades.AsNoTracking()
                join e in _db.Enrollments on f.EnrollmentId equals e.Id
                where e.ClassOfferingId == c.Id
                join s in _db.Students on e.StudentId equals s.Id
                join u in _db.Users on s.UserId equals u.Id
                orderby s.StudentNo
                select new { s.StudentNo, u.LastName, u.FirstName, f.RawGrade, f.ReportedGrade, f.FinalizedAt })
                .ToListAsync();

            vm.FinalizedAt = snap.Select(x => (DateTime?)x.FinalizedAt).Max();
            vm.Rows = snap.Select(x => new FinalizeRow
            {
                StudentNo = x.StudentNo,
                StudentName = $"{x.LastName}, {x.FirstName}",
                RawGrade = x.RawGrade,
                ReportedGrade = x.ReportedGrade,
                IsPassing = x.ReportedGrade <= 3.00m,
                Status = "Final"
            }).ToList();
            return vm;
        }

        var sheet = await _grades.GetClassSheetAsync(classOfferingId);
        if (sheet is null) return null;
        vm.Rows = sheet.Students.Select(s => new FinalizeRow
        {
            StudentNo = s.StudentNo,
            StudentName = s.Name,
            RawGrade = s.Semester.RawGrade,
            ReportedGrade = s.Semester.ReportedGrade,
            IsPassing = s.Semester.IsPassing,
            Status = s.Semester.Status.ToString()
        }).ToList();

        vm.Blockers = await FindBlockersAsync(c, sheet);
        return vm;
    }

    private async Task<List<string>> FindBlockersAsync(ClassOffering c, ClassGradeSheet sheet)
    {
        var blockers = new List<string>();

        var termIsCurrent = await _db.AcademicTerms.AsNoTracking()
            .AnyAsync(t => t.Id == c.TermId && t.IsCurrent == true);
        if (!termIsCurrent)
            blockers.Add("Only classes of the current term can be finalized.");

        if (sheet.Students.Count == 0)
            blockers.Add("The class has no enrolled students.");

        var notFinal = sheet.Students.Count(s => s.Semester.Status != SemesterStatus.Final);
        if (notFinal > 0)
            blockers.Add($"{notFinal} student(s) do not have a complete grade for all four periods yet.");

        var pending = await _db.ExcuseDecisions.AsNoTracking()
            .CountAsync(d => d.ClassOfferingId == c.Id && d.Status == "Pending");
        if (pending > 0)
            blockers.Add($"{pending} excuse letter(s) are still waiting for your decision.");

        return blockers;
    }

    public async Task<FinalizeResult> FinalizeAsync(Guid userId, int classOfferingId, bool confirmed)
    {
        var c = await OwnedClassAsync(userId, classOfferingId);
        if (c is null) return new(false, "This class is not available.");

        if (!confirmed)
            return new(false, "You must confirm that you understand finalizing cannot be undone.");

        // Decide from the CURRENT database state, not from the page.
        if (c.Status == "Finalized")
            return new(false, "This class is already finalized. Nothing was changed.");

        var sheet = await _grades.GetClassSheetAsync(classOfferingId);
        if (sheet is null)
            return new(false, "This class has no grade data. Nothing was changed.");
        var blockers = await FindBlockersAsync(c, sheet);
        if (blockers.Count > 0)
            return new(false, "The class cannot be finalized yet: " + string.Join(" ", blockers));

        // Map enrollment id -> grade, then write one snapshot row per student.
        var now = DateTime.Now;
        now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);

        foreach (var s in sheet.Students)
        {
            _db.FinalGrades.Add(new FinalGrade
            {
                EnrollmentId = (uint)s.EnrollmentId,
                RawGrade = s.Semester.RawGrade!.Value,
                ReportedGrade = s.Semester.ReportedGrade!.Value,
                FinalizedBy = userId,
                FinalizedAt = now
            });
        }

        c.Status = "Finalized";

        _audit.Add("ClassFinalized", "ClassOffering", c.Id.ToString(),
            oldValues: new { status = "Open" },
            newValues: new
            {
                status = "Finalized",
                students = sheet.Students.Count,
                passed = sheet.Students.Count(s => s.Semester.IsPassing == true),
                failed = sheet.Students.Count(s => s.Semester.IsPassing != true)
            });

        try
        {
            await _db.SaveChangesAsync();   // all grades + status + audit row: one transaction
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not finalize class {Id}", c.Id);
            return new(false, "The class could not be finalized. Nothing was changed.");
        }
        return new(true, null);
    }
}