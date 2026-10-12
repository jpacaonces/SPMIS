using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Models;
using Spmis.Web.Models.Admin;
using static Spmis.Web.Services.IEnrollmentAdminService;

namespace Spmis.Web.Services;

public class EnrollmentAdminService : IEnrollmentAdminService
{
    private readonly SpmisDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IReasonService _reasons;
    private readonly ILogger<EnrollmentAdminService> _logger;

    public EnrollmentAdminService(SpmisDbContext db, IAuditLogger audit,
        IReasonService reasons, ILogger<EnrollmentAdminService> logger)
    {
        _db = db;
        _audit = audit;
        _reasons = reasons;
        _logger = logger;
    }

    private async Task<string> ClassLabelAsync(ClassOffering c)
    {
        var sec = await _db.Sections.AsNoTracking().FirstAsync(s => s.Id == c.SectionId);
        var program = await _db.Programs.Where(p => p.Id == sec.ProgramId).Select(p => p.Code).FirstAsync();
        var shift = await _db.Shifts.Where(s => s.Id == sec.ShiftId).Select(s => s.Code).FirstAsync();
        var subject = await _db.Subjects.Where(s => s.Id == c.SubjectId).Select(s => s.Code).FirstAsync();
        return $"{program}{sec.YearLevel}{sec.Semester}{shift}{sec.SectionNo}/{subject}";
    }

    // Why this class cannot accept roster changes right now (null = it can).
    private async Task<string?> GetClassLockAsync(ClassOffering c)
    {
        if (c.IsActive != true) return "This class is inactive.";
        if (c.Status != "Open") return "This class is finalized. Its roster can no longer change.";
        var isCurrent = await _db.AcademicTerms
            .AnyAsync(t => t.Id == c.TermId && t.IsCurrent == true);
        return isCurrent ? null : "This class is not in the current term.";
    }

    public async Task<RosterViewModel?> GetRosterAsync(int classOfferingId)
    {
        var c = await _db.ClassOfferings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == classOfferingId);
        if (c is null) return null;

        var term = await _db.AcademicTerms.AsNoTracking().FirstAsync(t => t.Id == c.TermId);
        var faculty = await (
            from f in _db.Faculties.AsNoTracking()
            where f.Id == c.FacultyId
            join u in _db.Users on f.UserId equals u.Id
            select u.LastName + ", " + u.FirstName).FirstAsync();

        var items = await (
            from e in _db.Enrollments.AsNoTracking()
            where e.ClassOfferingId == classOfferingId
            join s in _db.Students on e.StudentId equals s.Id
            join u in _db.Users on s.UserId equals u.Id
            orderby u.LastName, u.FirstName
            select new RosterItem
            {
                EnrollmentId = (int)e.Id,
                StudentNo = s.StudentNo,
                Name = u.LastName + ", " + u.FirstName,
                Status = e.Status
            }).ToListAsync();

        var lock_ = await GetClassLockAsync(c);
        return new RosterViewModel
        {
            ClassOfferingId = classOfferingId,
            ClassLabel = await ClassLabelAsync(c),
            TermLabel = $"{term.SchoolYear} Sem {term.Semester}",
            Faculty = faculty,
            CanEdit = lock_ is null,
            CannotEditReason = lock_,
            EnrolledCount = items.Count(i => i.Status == "Enrolled"),
            Items = items
        };
    }

    public async Task<EnrollmentResult> EnrollAsync(int classOfferingId, string studentNo)
    {
        var c = await _db.ClassOfferings.FirstOrDefaultAsync(x => x.Id == classOfferingId);
        if (c is null)
            return new(false, string.Empty, "Class not found.");

        var lockReason = await GetClassLockAsync(c);
        if (lockReason is not null)
            return new(false, string.Empty, lockReason);

        var no = (studentNo ?? "").Trim();
        if (no.Length == 0)
            return new(false, "Enroll.StudentNo", "Enter a student number.");

        var student = await _db.Students.FirstOrDefaultAsync(s => s.StudentNo == no);
        if (student is null)
            return new(false, "Enroll.StudentNo", $"No student with number {no}.");
        if (student.IsActive != true)
            return new(false, "Enroll.StudentNo", $"Student {no} is inactive.");

        // A student takes classes of their own program.
        var sectionProgramId = await _db.Sections
            .Where(s => s.Id == c.SectionId).Select(s => s.ProgramId).FirstAsync();
        if (student.ProgramId != sectionProgramId)
            return new(false, "Enroll.StudentNo",
                $"Student {no} belongs to a different program than this class's section.");

        var existing = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.StudentId == student.Id && e.ClassOfferingId == c.Id);
        if (existing is not null)
            return new(false, "Enroll.StudentNo", existing.Status == "Enrolled"
                ? $"Student {no} is already enrolled in this class."
                : $"Student {no} was dropped from this class. Use Reinstate on the roster.");

        var label = await ClassLabelAsync(c);
        _db.Enrollments.Add(new Enrollment
        {
            StudentId = student.Id,
            ClassOfferingId = c.Id,
            Status = "Enrolled"
        });

        _audit.Add("StudentEnrolled", "Enrollment", $"{no}@{label}",
            newValues: new { studentNo = no, @class = label, status = "Enrolled" });

        try
        {
            await _db.SaveChangesAsync();   // enrollment + log row: one transaction
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Enrolling {StudentNo} in {Class} failed.", no, label);
            return new(false, string.Empty, "The enrollment could not be saved. Nothing was changed.");
        }
        return new(true, null, null);
    }

    public async Task<EnrollmentStatusViewModel?> GetStatusViewAsync(int enrollmentId, string newStatus)
    {
        if (newStatus is not ("Dropped" or "Enrolled")) return null;

        var e = await _db.Enrollments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == enrollmentId);
        if (e is null) return null;

        var c = await _db.ClassOfferings.AsNoTracking().FirstAsync(x => x.Id == e.ClassOfferingId);
        var student = await (
            from s in _db.Students.AsNoTracking()
            where s.Id == e.StudentId
            join u in _db.Users on s.UserId equals u.Id
            select s.StudentNo + " - " + u.LastName + ", " + u.FirstName).FirstAsync();

        return new EnrollmentStatusViewModel
        {
            EnrollmentId = enrollmentId,
            ClassOfferingId = (int)c.Id,
            StudentLabel = student,
            ClassLabel = await ClassLabelAsync(c),
            CurrentStatus = e.Status,
            NewStatus = newStatus,
            Blocker = await GetClassLockAsync(c)
        };
    }

    public async Task<EnrollmentResult> ChangeStatusAsync(
        int enrollmentId, string newStatus, ReasonInput reasonInput)
    {
        if (newStatus is not ("Dropped" or "Enrolled"))
            return new(false, string.Empty, "Invalid request.");

        var e = await _db.Enrollments.FirstOrDefaultAsync(x => x.Id == enrollmentId);
        if (e is null)
            return new(false, string.Empty, "Enrollment not found.");

        // Decide from the CURRENT database state, not from what the page said.
        if (e.Status == newStatus)
            return new(false, string.Empty, newStatus == "Dropped"
                ? "This student is already dropped. Nothing was changed."
                : "This student is already enrolled. Nothing was changed.");

        var c = await _db.ClassOfferings.FirstAsync(x => x.Id == e.ClassOfferingId);
        var lockReason = await GetClassLockAsync(c);
        if (lockReason is not null)
            return new(false, string.Empty, lockReason);

        if (newStatus == "Enrolled")
        {
            var studentActive = await _db.Students
                .AnyAsync(s => s.Id == e.StudentId && s.IsActive == true);
            if (!studentActive)
                return new(false, string.Empty, "Cannot reinstate: the student is inactive.");
        }

        var reason = await _reasons.ValidateAsync("AdminCorrection", reasonInput);
        if (!reason.Success)
            return new(false, "Reason.ReasonId", reason.Error!);

        var old = e.Status;
        e.Status = newStatus;

        var studentNo = await _db.Students.Where(s => s.Id == e.StudentId)
            .Select(s => s.StudentNo).FirstAsync();
        _audit.Add(newStatus == "Dropped" ? "StudentDropped" : "StudentReinstated",
            "Enrollment", $"{studentNo}@{await ClassLabelAsync(c)}",
            oldValues: new { status = old },
            newValues: new { status = newStatus },
            reason: reason.Text);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Status change for enrollment {Id} failed.", enrollmentId);
            return new(false, string.Empty, "The change could not be saved. Nothing was changed.");
        }
        return new(true, null, null);
    }

    public async Task LoadBulkOptionsAsync(BulkEnrollViewModel m)
    {
        var term = await _db.AcademicTerms.AsNoTracking().FirstOrDefaultAsync(t => t.IsCurrent == true);
        m.TermLabel = term is null ? null : $"{term.SchoolYear} Sem {term.Semester}";
        if (term is null) { m.Sections = []; return; }

        var rows = await (
            from sec in _db.Sections.AsNoTracking()
            where sec.IsActive == true
            join p in _db.Programs on sec.ProgramId equals p.Id
            join sh in _db.Shifts on sec.ShiftId equals sh.Id
            let classes = _db.ClassOfferings.Count(c =>
                c.SectionId == sec.Id && c.TermId == term.Id &&
                c.IsActive == true && c.Status == "Open")
            where classes > 0
            orderby p.Code, sec.YearLevel, sec.Semester, sh.Code, sec.SectionNo
            select new
            {
                sec.Id,
                ProgramCode = p.Code,
                sec.YearLevel,
                sec.Semester,
                ShiftCode = sh.Code,
                sec.SectionNo,
                classes
            }).ToListAsync();

        m.Sections = rows.Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
        {
            Value = r.Id.ToString(),
            Text = $"{r.ProgramCode}{r.YearLevel}{r.Semester}{r.ShiftCode}{r.SectionNo} ({r.classes} classes)"
        }).ToList();
    }

    public async Task<BulkEnrollResult> BulkEnrollAsync(BulkEnrollViewModel m)
    {
        var term = await _db.AcademicTerms.FirstOrDefaultAsync(t => t.IsCurrent == true);
        if (term is null)
            return new(false, string.Empty, "There is no current term.");

        var section = await _db.Sections.FirstOrDefaultAsync(s => s.Id == m.SectionId);
        if (section is null || section.IsActive != true)
            return new(false, nameof(m.SectionId), "Choose an active section.");

        var classes = await _db.ClassOfferings
            .Where(c => c.SectionId == section.Id && c.TermId == term.Id
                     && c.IsActive == true && c.Status == "Open")
            .ToListAsync();
        if (classes.Count == 0)
            return new(false, nameof(m.SectionId),
                "This section has no active, open classes this term.");

        // Parse: split on newlines, commas, semicolons and spaces; drop duplicates.
        var numbers = (m.StudentNumbers ?? "")
            .Split(['\n', '\r', ',', ';', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .Select(n => n.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (numbers.Count == 0)
            return new(false, nameof(m.StudentNumbers), "Enter at least one student number.");
        if (numbers.Count > 100)
            return new(false, nameof(m.StudentNumbers), "A batch is limited to 100 students.");

        var students = await _db.Students.Where(s => numbers.Contains(s.StudentNo)).ToListAsync();

        // Validate everything first. Any problem rejects the whole batch.
        var problems = new List<string>();
        foreach (var no in numbers)
        {
            var s = students.FirstOrDefault(x =>
                string.Equals(x.StudentNo, no, StringComparison.OrdinalIgnoreCase));
            if (s is null) problems.Add($"{no}: no such student");
            else if (s.IsActive != true) problems.Add($"{no}: inactive");
            else if (s.ProgramId != section.ProgramId) problems.Add($"{no}: different program");
        }
        if (problems.Count > 0)
        {
            var shown = string.Join("; ", problems.Take(15));
            var more = problems.Count > 15 ? $" ... and {problems.Count - 15} more" : "";
            return new(false, nameof(m.StudentNumbers),
                $"Nothing was enrolled. Fix these first: {shown}{more}");
        }

        var studentIds = students.Select(s => s.Id).ToList();
        var classIds = classes.Select(c => c.Id).ToList();
        var existing = await _db.Enrollments
            .Where(e => studentIds.Contains(e.StudentId) && classIds.Contains(e.ClassOfferingId))
            .ToListAsync();

        int created = 0, skipped = 0;
        var droppedNotes = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var s in students)
        {
            foreach (var c in classes)
            {
                var ex = existing.FirstOrDefault(e => e.StudentId == s.Id && e.ClassOfferingId == c.Id);
                if (ex is null)
                {
                    _db.Enrollments.Add(new Enrollment
                    {
                        StudentId = s.Id,
                        ClassOfferingId = c.Id,
                        Status = "Enrolled"
                    });
                    created++;
                }
                else
                {
                    skipped++;
                    if (ex.Status == "Dropped") droppedNotes.Add(s.StudentNo);
                }
            }
        }

        if (created == 0)
            return new(false, string.Empty,
                "Nothing to enroll: every student is already enrolled or was dropped in these classes.");

        var programCode = await _db.Programs.Where(p => p.Id == section.ProgramId)
            .Select(p => p.Code).FirstAsync();
        var shiftCode = await _db.Shifts.Where(x => x.Id == section.ShiftId)
            .Select(x => x.Code).FirstAsync();
        var secCode = $"{programCode}{section.YearLevel}{section.Semester}{shiftCode}{section.SectionNo}";

        _audit.Add("BulkEnrollment", "Section", secCode,
            newValues: new
            {
                term = $"{term.SchoolYear} Sem {term.Semester}",
                students = students.Select(s => s.StudentNo).OrderBy(x => x).ToList(),
                classes = classes.Count,
                enrollmentsCreated = created,
                skippedExisting = skipped
            });

        try
        {
            await _db.SaveChangesAsync();   // every enrollment + the log row: one transaction
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Bulk enrollment into {Section} failed.", secCode);
            return new(false, string.Empty, "The batch could not be saved. Nothing was changed.");
        }

        return new(true, null, null, created, skipped, students.Count, classes.Count,
            droppedNotes.ToList());
    }

}