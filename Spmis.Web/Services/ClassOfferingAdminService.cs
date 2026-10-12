using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Models;
using Spmis.Web.Models.Admin;

namespace Spmis.Web.Services;

public class ClassOfferingAdminService : IClassOfferingAdminService
{
    private const int PageSize = 25;

    private readonly SpmisDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IReasonService _reasons;
    private readonly ILogger<ClassOfferingAdminService> _logger;

    public ClassOfferingAdminService(SpmisDbContext db, IAuditLogger audit,
        IReasonService reasons, ILogger<ClassOfferingAdminService> logger)
    {
        _db = db;
        _audit = audit;
        _reasons = reasons;
        _logger = logger;
    }

    private static string SectionCode(string program, int year, int sem, string shift, int no) =>
        $"{program}{year}{sem}{shift}{no}";

    private static string TermLabel(AcademicTerm t) => $"{t.SchoolYear} Sem {t.Semester}";

    private Task<AcademicTerm?> GetCurrentTermAsync() =>
        _db.AcademicTerms.AsNoTracking().FirstOrDefaultAsync(t => t.IsCurrent == true);

    public async Task<ClassOfferingListViewModel> GetListAsync(string? search, int page)
    {
        var vm = new ClassOfferingListViewModel { Search = search?.Trim() };
        var term = await GetCurrentTermAsync();
        if (term is null) return vm;
        vm.TermLabel = TermLabel(term);

        var query =
            from c in _db.ClassOfferings.AsNoTracking()
            where c.TermId == term.Id
            join sec in _db.Sections on c.SectionId equals sec.Id
            join p in _db.Programs on sec.ProgramId equals p.Id
            join sh in _db.Shifts on sec.ShiftId equals sh.Id
            join sub in _db.Subjects on c.SubjectId equals sub.Id
            join f in _db.Faculties on c.FacultyId equals f.Id
            join u in _db.Users on f.UserId equals u.Id
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
                u.FirstName,
                u.LastName,
                c.AttendanceMode,
                IsActive = c.IsActive == true,
                Enrolled = _db.Enrollments.Count(e => e.ClassOfferingId == c.Id && e.Status == "Enrolled")
            };

        if (!string.IsNullOrEmpty(vm.Search))
        {
            var q = vm.Search;
            query = query.Where(x => x.SubjectCode.Contains(q) || x.SubjectTitle.Contains(q)
                                  || x.LastName.Contains(q) || x.FirstName.Contains(q)
                                  || x.ProgramCode.Contains(q));
        }

        vm.TotalCount = await query.CountAsync();
        vm.TotalPages = Math.Max(1, (int)Math.Ceiling(vm.TotalCount / (double)PageSize));
        vm.Page = Math.Clamp(page, 1, vm.TotalPages);

        var rows = await query
            .OrderBy(x => x.ProgramCode).ThenBy(x => x.YearLevel).ThenBy(x => x.Semester)
            .ThenBy(x => x.ShiftCode).ThenBy(x => x.SectionNo).ThenBy(x => x.SubjectCode)
            .Skip((vm.Page - 1) * PageSize).Take(PageSize)
            .ToListAsync();

        vm.Items = rows.Select(r => new ClassOfferingListItem
        {
            Id = (int)r.Id,
            SectionCode = SectionCode(r.ProgramCode, r.YearLevel, r.Semester, r.ShiftCode, r.SectionNo),
            SubjectCode = r.SubjectCode,
            SubjectTitle = r.SubjectTitle,
            Faculty = $"{r.LastName}, {r.FirstName}",
            AttendanceMode = r.AttendanceMode,
            Enrolled = r.Enrolled,
            IsActive = r.IsActive
        }).ToList();
        return vm;
    }

    public async Task LoadOptionsAsync(ClassOfferingCreateViewModel m)
    {
        var term = await GetCurrentTermAsync();
        m.TermLabel = term is null ? null : TermLabel(term);

        var secRows = await (
            from sec in _db.Sections.AsNoTracking()
            where sec.IsActive == true
            join p in _db.Programs on sec.ProgramId equals p.Id
            where p.IsActive == true
            join sh in _db.Shifts on sec.ShiftId equals sh.Id
            orderby p.Code, sec.YearLevel, sec.Semester, sh.Code, sec.SectionNo
            select new { sec.Id, p.Code, sec.YearLevel, sec.Semester, ShiftCode = sh.Code, sec.SectionNo })
            .ToListAsync();
        m.Sections = secRows.Select(s => new SelectListItem
        {
            Value = s.Id.ToString(),
            Text = SectionCode(s.Code, s.YearLevel, s.Semester, s.ShiftCode, s.SectionNo)
        }).ToList();

        m.Subjects = await _db.Subjects.AsNoTracking()
            .Where(s => s.IsActive == true).OrderBy(s => s.Code)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Code + " - " + s.Title })
            .ToListAsync();

        m.FacultyOptions = await (
            from f in _db.Faculties.AsNoTracking()
            where f.IsActive == true
            join u in _db.Users on f.UserId equals u.Id
            orderby u.LastName, u.FirstName
            select new SelectListItem
            {
                Value = f.Id.ToString(),
                Text = u.LastName + ", " + u.FirstName + " (" + f.EmployeeNo + ")"
            }).ToListAsync();

        m.Tables = await _db.TransmutationTables.AsNoTracking()
            .Where(t => t.IsActive == true).OrderBy(t => t.Name)
            .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
            .ToListAsync();
    }

    public async Task<ClassOfferingResult> CreateAsync(ClassOfferingCreateViewModel m)
    {
        var term = await _db.AcademicTerms.FirstOrDefaultAsync(t => t.IsCurrent == true);
        if (term is null)
            return new(false, string.Empty, "There is no current term. Set one under Terms first.");

        if (m.AttendanceMode is not ("Meetings" or "ActivityBased"))
            return new(false, nameof(m.AttendanceMode), "Choose a valid attendance mode.");

        var section = await _db.Sections.FirstOrDefaultAsync(s => s.Id == m.SectionId);
        if (section is null || section.IsActive != true)
            return new(false, nameof(m.SectionId), "Choose an active section.");

        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == m.SubjectId);
        if (subject is null || subject.IsActive != true)
            return new(false, nameof(m.SubjectId), "Choose an active subject.");

        var faculty = await _db.Faculties.FirstOrDefaultAsync(f => f.Id == m.FacultyId);
        if (faculty is null || faculty.IsActive != true)
            return new(false, nameof(m.FacultyId), "Choose an active faculty member.");

        var table = await _db.TransmutationTables.FirstOrDefaultAsync(t => t.Id == m.TransmutationTableId);
        if (table is null || table.IsActive != true)
            return new(false, nameof(m.TransmutationTableId), "Choose an active transmutation table.");

        // The class must follow the curriculum slot of its section.
        var inCurriculum = await _db.CurriculumSubjects.AnyAsync(c =>
            c.ProgramId == section.ProgramId && c.YearLevel == section.YearLevel &&
            c.Semester == section.Semester && c.SubjectId == subject.Id && c.IsActive == true);
        if (!inCurriculum)
            return new(false, nameof(m.SubjectId),
                "This subject is not in the curriculum for that section's program, year level and semester.");

        var programCode = await _db.Programs.Where(p => p.Id == section.ProgramId)
            .Select(p => p.Code).FirstAsync();
        var shiftCode = await _db.Shifts.Where(s => s.Id == section.ShiftId)
            .Select(s => s.Code).FirstAsync();
        var secCode = SectionCode(programCode, section.YearLevel, section.Semester,
            shiftCode, section.SectionNo);

        var existing = await _db.ClassOfferings.FirstOrDefaultAsync(c =>
            c.TermId == term.Id && c.SubjectId == subject.Id && c.SectionId == section.Id);
        if (existing is not null)
            return new(false, string.Empty, existing.IsActive == true
                ? $"{secCode} already has {subject.Code} this term."
                : $"{secCode} already has {subject.Code} this term, but it is inactive. Reactivate it instead.");

        // Copy the default grading template into this class.
        var template = await _db.GradingTemplates
            .FirstOrDefaultAsync(g => g.IsDefault == true && g.IsActive == true);
        if (template is null)
            return new(false, string.Empty, "No active default grading template exists.");

        var templateComponents = await _db.TemplateComponents
            .Where(t => t.TemplateId == template.Id && t.IsActive == true)
            .OrderBy(t => t.SortOrder).ToListAsync();

        var offering = new ClassOffering
        {
            TermId = term.Id,
            SubjectId = subject.Id,
            SectionId = section.Id,
            FacultyId = faculty.Id,
            TransmutationTableId = table.Id,
            ClassStandingWeight = template.ClassStandingWeight,
            MajorExamWeight = template.MajorExamWeight,
            AttendanceMode = m.AttendanceMode,
            Status = "Open",
            IsActive = true
        };
        _db.ClassOfferings.Add(offering);

        foreach (var c in BuildComponents(templateComponents, subject.LabUnits > 0))
        {
            c.ClassOffering = offering;
            _db.ClassComponents.Add(c);
        }

        var label = $"{secCode}/{subject.Code}";
        _audit.Add("ClassOfferingCreated", "ClassOffering", label,
            newValues: new
            {
                term = TermLabel(term),
                section = secCode,
                subject = subject.Code,
                facultyId = faculty.Id,
                transmutationTable = table.Name,
                attendanceMode = m.AttendanceMode,
                template = template.Name
            });

        try
        {
            await _db.SaveChangesAsync();   // class + components + log row: one transaction
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Creating class {Label} failed.", label);
            return new(false, string.Empty, "The class could not be saved. Nothing was changed.");
        }
        return new(true, null, null);
    }

    // Copies template components. Lecture-only subjects drop the Laboratory group,
    // and the Lecture weights are rescaled so Class Standing still totals 100.
    private static List<ClassComponent> BuildComponents(
        List<TemplateComponent> template, bool hasLab)
    {
        var rows = template
            .Where(t => hasLab || t.GroupType != "Laboratory")
            .Select(t => new ClassComponent
            {
                ComponentType = t.ComponentType,
                Name = t.Name,
                GroupType = t.GroupType,
                WeightPct = t.WeightPct,
                IsAttendance = t.IsAttendance,
                SortOrder = t.SortOrder,
                IsActive = true
            }).ToList();

        if (!hasLab)
        {
            var standing = rows.Where(r => r.ComponentType == "ClassStanding").ToList();
            var total = standing.Sum(r => r.WeightPct);
            if (total > 0 && standing.Count > 0)
            {
                foreach (var r in standing)
                    r.WeightPct = Math.Round(r.WeightPct * 100m / total, 2);
                // Rounding can leave 99.99 or 100.01: put the difference on the last row.
                var diff = 100m - standing.Sum(r => r.WeightPct);
                standing[^1].WeightPct += diff;
            }
        }
        return rows;
    }

    private async Task<string?> GetBlockerAsync(uint offeringId)
    {
        var n = await _db.Enrollments
            .CountAsync(e => e.ClassOfferingId == offeringId && e.Status == "Enrolled");
        return n == 0 ? null
            : $"Cannot deactivate: {n} student(s) are still enrolled in this class.";
    }

    private async Task<string> LabelOfAsync(ClassOffering c)
    {
        var sec = await _db.Sections.AsNoTracking().FirstAsync(s => s.Id == c.SectionId);
        var program = await _db.Programs.Where(p => p.Id == sec.ProgramId).Select(p => p.Code).FirstAsync();
        var shift = await _db.Shifts.Where(s => s.Id == sec.ShiftId).Select(s => s.Code).FirstAsync();
        var subject = await _db.Subjects.Where(s => s.Id == c.SubjectId).Select(s => s.Code).FirstAsync();
        return $"{SectionCode(program, sec.YearLevel, sec.Semester, shift, sec.SectionNo)}/{subject}";
    }

    public async Task<ClassOfferingStatusViewModel?> GetStatusViewAsync(int id)
    {
        var c = await _db.ClassOfferings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return null;

        var active = c.IsActive == true;
        return new ClassOfferingStatusViewModel
        {
            ClassOfferingId = id,
            Label = await LabelOfAsync(c),
            IsActive = active,
            Activate = !active,
            Blocker = active ? await GetBlockerAsync(c.Id) : null
        };
    }

    public async Task<ClassOfferingResult> ChangeStatusAsync(
        int id, bool activate, ReasonInput reasonInput)
    {
        var context = activate ? "Reactivate" : "Deactivate";

        var c = await _db.ClassOfferings.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null)
            return new(false, string.Empty, "Class not found.");

        var currentlyActive = c.IsActive == true;
        if (currentlyActive == activate)
            return new(false, string.Empty, activate
                ? "This class is already active. Nothing was changed."
                : "This class is already inactive. Nothing was changed.");

        if (activate)
        {
            var parentsActive =
                await _db.Subjects.AnyAsync(s => s.Id == c.SubjectId && s.IsActive == true) &&
                await _db.Sections.AnyAsync(s => s.Id == c.SectionId && s.IsActive == true) &&
                await _db.Faculties.AnyAsync(f => f.Id == c.FacultyId && f.IsActive == true);
            if (!parentsActive)
                return new(false, string.Empty,
                    "Cannot reactivate: the subject, section or faculty of this class is inactive.");
        }
        else
        {
            var blocker = await GetBlockerAsync(c.Id);
            if (blocker is not null)
                return new(false, string.Empty, blocker);
        }

        var reason = await _reasons.ValidateAsync(context, reasonInput);
        if (!reason.Success)
            return new(false, "Reason.ReasonId", reason.Error!);

        c.IsActive = activate;

        _audit.Add(activate ? "ClassOfferingReactivated" : "ClassOfferingDeactivated",
            "ClassOffering", await LabelOfAsync(c),
            oldValues: new { isActive = currentlyActive },
            newValues: new { isActive = activate },
            reason: reason.Text);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Status change for class {Id} failed.", id);
            return new(false, string.Empty, "The change could not be saved. Nothing was changed.");
        }
        return new(true, null, null);
    }
}