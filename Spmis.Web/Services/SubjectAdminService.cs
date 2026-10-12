using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Models;
using Spmis.Web.Models.Admin;

namespace Spmis.Web.Services;

public class SubjectAdminService : ISubjectAdminService
{
    private readonly SpmisDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IReasonService _reasons;
    private readonly ILogger<SubjectAdminService> _logger;

    public SubjectAdminService(SpmisDbContext db, IAuditLogger audit,
        IReasonService reasons, ILogger<SubjectAdminService> logger)
    {
        _db = db;
        _audit = audit;
        _reasons = reasons;
        _logger = logger;
    }

    public Task<List<SubjectListItem>> GetListAsync() =>
        _db.Subjects
            .AsNoTracking()
            .OrderBy(s => s.Code)
            .Select(s => new SubjectListItem
            {
                Id = (int)s.Id,
                Code = s.Code,
                Title = s.Title,
                LectureUnits = s.LectureUnits,
                LabUnits = s.LabUnits,
                IsActive = s.IsActive == true,
                ActiveClassesThisTerm = _db.ClassOfferings.Count(c =>
                    c.SubjectId == s.Id && c.IsActive == true &&
                    _db.AcademicTerms.Any(t => t.Id == c.TermId && t.IsCurrent == true))
            })
            .ToListAsync();

    public async Task<SubjectResult> CreateAsync(SubjectCreateViewModel m)
    {
        var code = (m.Code ?? "").Trim().ToUpperInvariant();
        var title = (m.Title ?? "").Trim();

        if (!Regex.IsMatch(code, "^[A-Z0-9][A-Z0-9-]{1,19}$"))
            return new(false, nameof(m.Code),
                "Code must be 2-20 letters, digits or hyphens, no spaces.");
        if (title.Length == 0)
            return new(false, nameof(m.Title), "Title is required.");
        if (m.LectureUnits < 0 || m.LabUnits < 0 || m.LectureUnits + m.LabUnits <= 0)
            return new(false, nameof(m.LectureUnits),
                "A subject needs at least 1 unit (lecture + lab).");

        if (await _db.Subjects.AnyAsync(s => s.Code == code))
            return new(false, nameof(m.Code), $"A subject with code {code} already exists.");

        _db.Subjects.Add(new Subject
        {
            Code = code,
            Title = title,
            LectureUnits = (byte)m.LectureUnits,
            LabUnits = (byte)m.LabUnits,
            IsActive = true
        });

        _audit.Add("SubjectCreated", "Subject", code,
            newValues: new { code, title, m.LectureUnits, m.LabUnits });

        try
        {
            await _db.SaveChangesAsync();   // subject + log row: one transaction
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Creating subject {Code} failed.", code);
            return new(false, string.Empty, "The subject could not be saved. Nothing was changed.");
        }
        return new(true, null, null);
    }

    private async Task<string?> GetBlockerAsync(uint subjectId)
    {
        var n = await _db.ClassOfferings.CountAsync(c =>
            c.SubjectId == subjectId && c.IsActive == true &&
            _db.AcademicTerms.Any(t => t.Id == c.TermId && t.IsCurrent == true));

        return n == 0 ? null
            : $"Cannot deactivate: {n} active class offering(s) this term use this subject.";
    }

    public async Task<SubjectStatusViewModel?> GetStatusViewAsync(int id)
    {
        var s = await _db.Subjects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return null;

        var active = s.IsActive == true;
        return new SubjectStatusViewModel
        {
            SubjectId = id,
            Code = s.Code,
            Title = s.Title,
            IsActive = active,
            Activate = !active,
            Blocker = active ? await GetBlockerAsync(s.Id) : null
        };
    }

    public async Task<SubjectResult> ChangeStatusAsync(
        int id, bool activate, ReasonInput reasonInput)
    {
        var context = activate ? "Reactivate" : "Deactivate";

        var s = await _db.Subjects.FirstOrDefaultAsync(x => x.Id == id);
        if (s is null)
            return new(false, string.Empty, "Subject not found.");

        var currentlyActive = s.IsActive == true;
        if (currentlyActive == activate)
            return new(false, string.Empty, activate
                ? "This subject is already active. Nothing was changed."
                : "This subject is already inactive. Nothing was changed.");

        if (!activate)
        {
            var blocker = await GetBlockerAsync(s.Id);
            if (blocker is not null)
                return new(false, string.Empty, blocker);
        }

        var reason = await _reasons.ValidateAsync(context, reasonInput);
        if (!reason.Success)
            return new(false, "Reason.ReasonId", reason.Error!);

        s.IsActive = activate;

        _audit.Add(activate ? "SubjectReactivated" : "SubjectDeactivated",
            "Subject", s.Code,
            oldValues: new { isActive = currentlyActive },
            newValues: new { isActive = activate },
            reason: reason.Text);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Status change for subject {Id} failed.", id);
            return new(false, string.Empty, "The change could not be saved. Nothing was changed.");
        }
        return new(true, null, null);
    }
}