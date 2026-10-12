using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Models;
using Spmis.Web.Models.Admin;

namespace Spmis.Web.Services;

public class TermAdminService : ITermAdminService
{
    private readonly SpmisDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IReasonService _reasons;
    private readonly ILogger<TermAdminService> _logger;

    public TermAdminService(SpmisDbContext db, IAuditLogger audit,
        IReasonService reasons, ILogger<TermAdminService> logger)
    {
        _db = db;
        _audit = audit;
        _reasons = reasons;
        _logger = logger;
    }

    private static string Label(string schoolYear, int semester) =>
        $"{schoolYear} Sem {semester}";

    public Task<List<TermListItem>> GetListAsync() =>
        _db.AcademicTerms
            .AsNoTracking()
            .OrderByDescending(t => t.SchoolYear)
            .ThenByDescending(t => t.Semester)
            .Select(t => new TermListItem
            {
                Id = (int)t.Id,
                SchoolYear = t.SchoolYear,
                Semester = t.Semester,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                IsCurrent = t.IsCurrent == true,
                ClassCount = _db.ClassOfferings.Count(c => c.TermId == t.Id)
            })
            .ToListAsync();

    public async Task<TermResult> CreateAsync(TermCreateViewModel m)
    {
        var sy = (m.SchoolYear ?? "").Trim();

        // Rule 1: format YYYY-YYYY and the second year is the first + 1.
        var parts = sy.Split('-');
        var formatOk = parts.Length == 2
            && int.TryParse(parts[0], out var y1)
            && int.TryParse(parts[1], out var y2)
            && parts[0].Length == 4 && parts[1].Length == 4
            && y2 == y1 + 1;
        if (!formatOk)
            return new(false, nameof(m.SchoolYear),
                "School year must look like 2026-2027 (second year = first + 1).");

        if (m.Semester is not (1 or 2))
            return new(false, nameof(m.Semester), "Semester must be 1 or 2.");

        if (m.StartDate is null || m.EndDate is null)
            return new(false, nameof(m.StartDate), "Both dates are required.");

        // Rule 2: same rule as the CHECK in the database, checked first for a clear message.
        if (m.EndDate <= m.StartDate)
            return new(false, nameof(m.EndDate), "End date must be after the start date.");

        // Rule 3: no duplicate school year + semester.
        var exists = await _db.AcademicTerms
            .AnyAsync(t => t.SchoolYear == sy && t.Semester == m.Semester);
        if (exists)
            return new(false, string.Empty, $"{Label(sy, m.Semester)} already exists.");

        var term = new AcademicTerm
        {
            SchoolYear = sy,
            Semester = (byte)m.Semester,
            StartDate = m.StartDate.Value,
            EndDate = m.EndDate.Value,
            IsCurrent = false   // becoming current is a separate, logged action
        };
        _db.AcademicTerms.Add(term);

        // The new id does not exist yet, so the label identifies the record.
        _audit.Add("TermCreated", "AcademicTerm", Label(sy, m.Semester),
            newValues: new { sy, m.Semester, m.StartDate, m.EndDate });

        try
        {
            await _db.SaveChangesAsync();   // term + log row: one transaction
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Creating term {Label} failed.", Label(sy, m.Semester));
            return new(false, string.Empty, "The term could not be saved. Nothing was changed.");
        }
        return new(true, null, null);
    }

    public async Task<TermCurrentViewModel?> GetSetCurrentViewAsync(int id)
    {
        var term = await _db.AcademicTerms.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id);
        if (term is null) return null;

        var current = await _db.AcademicTerms.AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsCurrent == true);

        return new TermCurrentViewModel
        {
            TermId = id,
            Label = Label(term.SchoolYear, term.Semester),
            CurrentLabel = current is null ? null : Label(current.SchoolYear, current.Semester)
        };
    }

    public async Task<TermResult> SetCurrentAsync(int id, ReasonInput reasonInput)
    {
        var target = await _db.AcademicTerms.FirstOrDefaultAsync(t => t.Id == id);
        if (target is null)
            return new(false, string.Empty, "Term not found.");

        // Decide from the CURRENT database state, not from what the page said.
        if (target.IsCurrent == true)
            return new(false, string.Empty, "This term is already the current term. Nothing was changed.");

        var reason = await _reasons.ValidateAsync("AdminCorrection", reasonInput);
        if (!reason.Success)
            return new(false, "Reason.ReasonId", reason.Error!);

        var previous = await _db.AcademicTerms
            .Where(t => t.IsCurrent == true && t.Id != id)
            .ToListAsync();

        foreach (var p in previous) p.IsCurrent = false;
        target.IsCurrent = true;

        _audit.Add("TermSetCurrent", "AcademicTerm",
            Label(target.SchoolYear, target.Semester),
            oldValues: new { current = previous.Select(p => Label(p.SchoolYear, p.Semester)) },
            newValues: new { current = Label(target.SchoolYear, target.Semester) },
            reason: reason.Text);

        try
        {
            await _db.SaveChangesAsync();   // all flips + log row: one transaction
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Setting current term {Id} failed.", id);
            return new(false, string.Empty, "The change could not be saved. Nothing was changed.");
        }
        return new(true, null, null);
    }
}