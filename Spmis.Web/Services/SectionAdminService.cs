using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Models;
using Spmis.Web.Models.Admin;

namespace Spmis.Web.Services;

public class SectionAdminService : ISectionAdminService
{
    private readonly SpmisDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IReasonService _reasons;
    private readonly ILogger<SectionAdminService> _logger;

    public SectionAdminService(SpmisDbContext db, IAuditLogger audit,
        IReasonService reasons, ILogger<SectionAdminService> logger)
    {
        _db = db;
        _audit = audit;
        _reasons = reasons;
        _logger = logger;
    }

    // BSIT31E1 = program + year + semester + shift code + section number.
    private static string BuildCode(string program, int year, int sem, string shift, int no) =>
        $"{program}{year}{sem}{shift}{no}";

    public async Task<List<SectionListItem>> GetListAsync()
    {
        var rows = await (
            from s in _db.Sections.AsNoTracking()
            join p in _db.Programs on s.ProgramId equals p.Id
            join sh in _db.Shifts on s.ShiftId equals sh.Id
            orderby p.Code, s.YearLevel, s.Semester, sh.Code, s.SectionNo
            select new
            {
                s.Id,
                ProgramCode = p.Code,
                s.YearLevel,
                s.Semester,
                ShiftCode = sh.Code,
                ShiftName = sh.Name,
                s.SectionNo,
                IsActive = s.IsActive == true,
                ClassesThisTerm = _db.ClassOfferings.Count(c =>
                    c.SectionId == s.Id && c.IsActive == true &&
                    _db.AcademicTerms.Any(t => t.Id == c.TermId && t.IsCurrent == true))
            }).ToListAsync();

        return rows.Select(r => new SectionListItem
        {
            Id = (int)r.Id,
            Code = BuildCode(r.ProgramCode, r.YearLevel, r.Semester, r.ShiftCode, r.SectionNo),
            Program = r.ProgramCode,
            YearLevel = r.YearLevel,
            Semester = r.Semester,
            Shift = r.ShiftName,
            IsActive = r.IsActive,
            ClassesThisTerm = r.ClassesThisTerm
        }).ToList();
    }

    public async Task LoadOptionsAsync(SectionCreateViewModel m)
    {
        m.Programs = await _db.Programs.AsNoTracking()
            .Where(p => p.IsActive == true)
            .OrderBy(p => p.Code)
            .Select(p => new SelectListItem { Value = p.Id.ToString(), Text = p.Code + " - " + p.Name })
            .ToListAsync();

        m.Shifts = await _db.Shifts.AsNoTracking()
            .OrderBy(s => s.Code)
            .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Code + " - " + s.Name })
            .ToListAsync();
    }

    public async Task<SectionResult> CreateAsync(SectionCreateViewModel m)
    {
        if (m.ProgramId is null || m.YearLevel is null || m.Semester is null ||
            m.ShiftId is null || m.SectionNo is null)
            return new(false, string.Empty, "All fields are required.");

        var program = await _db.Programs.FirstOrDefaultAsync(p => p.Id == m.ProgramId);
        if (program is null || program.IsActive != true)
            return new(false, nameof(m.ProgramId), "Choose an active program.");

        var shift = await _db.Shifts.FirstOrDefaultAsync(s => s.Id == m.ShiftId);
        if (shift is null)
            return new(false, nameof(m.ShiftId), "Choose a valid shift.");

        // Rule: the shift must exist for that year level (year 1-2: M/A, year 3-4: A/E).
        var shiftOk = await _db.YearLevelShifts
            .AnyAsync(y => y.YearLevel == m.YearLevel && y.ShiftId == m.ShiftId);
        if (!shiftOk)
            return new(false, nameof(m.ShiftId),
                $"The {shift.Name} shift is not offered for year {m.YearLevel}.");

        var exists = await _db.Sections.AnyAsync(s =>
            s.ProgramId == m.ProgramId && s.YearLevel == m.YearLevel &&
            s.Semester == m.Semester && s.ShiftId == m.ShiftId && s.SectionNo == m.SectionNo);

        var code = BuildCode(program.Code, m.YearLevel.Value, m.Semester.Value,
            shift.Code, m.SectionNo.Value);
        if (exists)
            return new(false, string.Empty, $"Section {code} already exists.");

        _db.Sections.Add(new Section
        {
            ProgramId = program.Id,
            YearLevel = (byte)m.YearLevel.Value,
            Semester = (byte)m.Semester.Value,
            ShiftId = shift.Id,
            SectionNo = (byte)m.SectionNo.Value,
            IsActive = true
        });

        _audit.Add("SectionCreated", "Section", code,
            newValues: new { code, program = program.Code, m.YearLevel, m.Semester, shift = shift.Code, m.SectionNo });

        try
        {
            await _db.SaveChangesAsync();   // section + log row: one transaction
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Creating section {Code} failed.", code);
            return new(false, string.Empty, "The section could not be saved. Nothing was changed.");
        }
        return new(true, null, null);
    }

    private async Task<string?> GetBlockerAsync(uint sectionId)
    {
        var n = await _db.ClassOfferings.CountAsync(c =>
            c.SectionId == sectionId && c.IsActive == true &&
            _db.AcademicTerms.Any(t => t.Id == c.TermId && t.IsCurrent == true));

        return n == 0 ? null
            : $"Cannot deactivate: {n} active class offering(s) this term belong to this section.";
    }

    private async Task<string?> CodeOfAsync(Section s)
    {
        var program = await _db.Programs.AsNoTracking().Where(p => p.Id == s.ProgramId)
            .Select(p => p.Code).FirstAsync();
        var shift = await _db.Shifts.AsNoTracking().Where(x => x.Id == s.ShiftId)
            .Select(x => x.Code).FirstAsync();
        return BuildCode(program, s.YearLevel, s.Semester, shift, s.SectionNo);
    }

    public async Task<SectionStatusViewModel?> GetStatusViewAsync(int id)
    {
        var s = await _db.Sections.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return null;

        var active = s.IsActive == true;
        return new SectionStatusViewModel
        {
            SectionId = id,
            Code = (await CodeOfAsync(s))!,
            IsActive = active,
            Activate = !active,
            Blocker = active ? await GetBlockerAsync(s.Id) : null
        };
    }

    public async Task<SectionResult> ChangeStatusAsync(
        int id, bool activate, ReasonInput reasonInput)
    {
        var context = activate ? "Reactivate" : "Deactivate";

        var s = await _db.Sections.FirstOrDefaultAsync(x => x.Id == id);
        if (s is null)
            return new(false, string.Empty, "Section not found.");

        var currentlyActive = s.IsActive == true;
        if (currentlyActive == activate)
            return new(false, string.Empty, activate
                ? "This section is already active. Nothing was changed."
                : "This section is already inactive. Nothing was changed.");

        if (activate)
        {
            var programActive = await _db.Programs
                .AnyAsync(p => p.Id == s.ProgramId && p.IsActive == true);
            if (!programActive)
                return new(false, string.Empty,
                    "Cannot reactivate: the section's program is inactive.");
        }
        else
        {
            var blocker = await GetBlockerAsync(s.Id);
            if (blocker is not null)
                return new(false, string.Empty, blocker);
        }

        var reason = await _reasons.ValidateAsync(context, reasonInput);
        if (!reason.Success)
            return new(false, "Reason.ReasonId", reason.Error!);

        s.IsActive = activate;

        _audit.Add(activate ? "SectionReactivated" : "SectionDeactivated",
            "Section", await CodeOfAsync(s),
            oldValues: new { isActive = currentlyActive },
            newValues: new { isActive = activate },
            reason: reason.Text);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Status change for section {Id} failed.", id);
            return new(false, string.Empty, "The change could not be saved. Nothing was changed.");
        }
        return new(true, null, null);
    }
}