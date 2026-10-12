using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Models;
using Spmis.Web.Models.Admin;
using AcademicProgram = Spmis.Web.Data.Entities.Program;

namespace Spmis.Web.Services;

public class ProgramAdminService : IProgramAdminService
{
    private readonly SpmisDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IReasonService _reasons;
    private readonly ILogger<ProgramAdminService> _logger;

    public ProgramAdminService(SpmisDbContext db, IAuditLogger audit,
        IReasonService reasons, ILogger<ProgramAdminService> logger)
    {
        _db = db;
        _audit = audit;
        _reasons = reasons;
        _logger = logger;
    }

    public Task<List<ProgramListItem>> GetListAsync() =>
        _db.Programs
            .AsNoTracking()
            .OrderBy(p => p.Code)
            .Select(p => new ProgramListItem
            {
                Id = (int)p.Id,
                Code = p.Code,
                Name = p.Name,
                Department = _db.Departments
                    .Where(d => d.Id == p.DepartmentId)
                    .Select(d => d.Code).FirstOrDefault() ?? "",
                IsActive = p.IsActive == true,
                ActiveStudents = _db.Students
                    .Count(s => s.ProgramId == p.Id && s.IsActive == true),
                ActiveSections = _db.Sections
                    .Count(s => s.ProgramId == p.Id && s.IsActive == true)
            })
            .ToListAsync();

    public Task<List<SelectListItem>> GetDepartmentOptionsAsync() =>
        _db.Departments
            .AsNoTracking()
            .Where(d => d.IsActive == true)
            .OrderBy(d => d.Code)
            .Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.Code + " - " + d.Name
            })
            .ToListAsync();

    public async Task<ProgramResult> CreateAsync(ProgramCreateViewModel m)
    {
        var code = (m.Code ?? "").Trim().ToUpperInvariant();
        var name = (m.Name ?? "").Trim();

        if (!Regex.IsMatch(code, "^[A-Z0-9]{2,10}$"))
            return new(false, nameof(m.Code), "Code must be 2-10 letters or digits, no spaces.");
        if (name.Length == 0)
            return new(false, nameof(m.Name), "Program name is required.");

        var dept = await _db.Departments
            .FirstOrDefaultAsync(d => d.Id == m.DepartmentId);
        if (dept is null || dept.IsActive != true)
            return new(false, nameof(m.DepartmentId), "Choose an active department.");

        if (await _db.Programs.AnyAsync(p => p.Code == code))
            return new(false, nameof(m.Code), $"A program with code {code} already exists.");

        _db.Programs.Add(new AcademicProgram
        {
            Code = code,
            Name = name,
            DepartmentId = dept.Id,
            IsActive = true
        });

        _audit.Add("ProgramCreated", "Program", code,
            newValues: new { code, name, department = dept.Code });

        try
        {
            await _db.SaveChangesAsync();   // program + log row: one transaction
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Creating program {Code} failed.", code);
            return new(false, string.Empty, "The program could not be saved. Nothing was changed.");
        }
        return new(true, null, null);
    }

    // Why deactivation is not allowed right now (null = allowed).
    private async Task<string?> GetBlockerAsync(uint programId)
    {
        var students = await _db.Students
            .CountAsync(s => s.ProgramId == programId && s.IsActive == true);
        var heads = await _db.ProgramHeads
            .CountAsync(h => h.ProgramId == programId && h.IsActive == true);
        var sections = await _db.Sections
            .CountAsync(s => s.ProgramId == programId && s.IsActive == true);

        if (students == 0 && heads == 0 && sections == 0) return null;
        return $"Cannot deactivate: {students} active student(s), {heads} active program head(s) " +
               $"and {sections} active section(s) still belong to this program.";
    }

    public async Task<ProgramStatusViewModel?> GetStatusViewAsync(int id)
    {
        var p = await _db.Programs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return null;

        var active = p.IsActive == true;
        return new ProgramStatusViewModel
        {
            ProgramId = id,
            Code = p.Code,
            Name = p.Name,
            IsActive = active,
            Activate = !active,
            Blocker = active ? await GetBlockerAsync(p.Id) : null
        };
    }

    public async Task<ProgramResult> ChangeStatusAsync(
        int id, bool activate, ReasonInput reasonInput)
    {
        var context = activate ? "Reactivate" : "Deactivate";

        var p = await _db.Programs.FirstOrDefaultAsync(x => x.Id == id);
        if (p is null)
            return new(false, string.Empty, "Program not found.");

        // Decide from the CURRENT database state, not from what the page said.
        var currentlyActive = p.IsActive == true;
        if (currentlyActive == activate)
            return new(false, string.Empty, activate
                ? "This program is already active. Nothing was changed."
                : "This program is already inactive. Nothing was changed.");

        if (activate)
        {
            var deptActive = await _db.Departments
                .AnyAsync(d => d.Id == p.DepartmentId && d.IsActive == true);
            if (!deptActive)
                return new(false, string.Empty,
                    "Cannot reactivate: the program's department is inactive.");
        }
        else
        {
            var blocker = await GetBlockerAsync(p.Id);
            if (blocker is not null)
                return new(false, string.Empty, blocker);
        }

        var reason = await _reasons.ValidateAsync(context, reasonInput);
        if (!reason.Success)
            return new(false, "Reason.ReasonId", reason.Error!);

        p.IsActive = activate;

        _audit.Add(activate ? "ProgramReactivated" : "ProgramDeactivated",
            "Program", p.Code,
            oldValues: new { isActive = currentlyActive },
            newValues: new { isActive = activate },
            reason: reason.Text);

        try
        {
            await _db.SaveChangesAsync();   // change + log row: one transaction
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Status change for program {Id} failed.", id);
            return new(false, string.Empty, "The change could not be saved. Nothing was changed.");
        }
        return new(true, null, null);
    }
}