using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Models.ClassRecord;
using Spmis.Web.Models;
namespace Spmis.Web.Services;

public class AssessmentService : IAssessmentService
{
    private readonly SpmisDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IReasonService _reasons;
    private readonly ILogger<AssessmentService> _logger;

    public AssessmentService(SpmisDbContext db, IAuditLogger audit,
        IReasonService reasons, ILogger<AssessmentService> logger)
    {
        _db = db;
        _audit = audit;
        _reasons = reasons;
        _logger = logger;
    }

    // Layer 2 of authorization: the class must belong to THIS faculty member.
    private async Task<ClassOffering?> OwnedClassAsync(Guid userId, int classId)
    {
        var facultyId = await _db.Faculties.AsNoTracking()
            .Where(f => f.UserId == userId && f.IsActive == true)
            .Select(f => (uint?)f.Id)
            .FirstOrDefaultAsync();
        if (facultyId is null) return null;

        var id = (uint)classId;
        return await _db.ClassOfferings.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.FacultyId == facultyId
                                      && c.IsActive == true);
    }

    private async Task<string> HeaderAsync(ClassOffering c)
    {
        var sub = await _db.Subjects.AsNoTracking()
            .Where(s => s.Id == c.SubjectId)
            .Select(s => s.Code + " - " + s.Title)
            .FirstAsync();
        return sub;
    }

    public async Task<AssessmentsViewModel?> GetAsync(Guid userId, int classOfferingId)
    {
        var c = await OwnedClassAsync(userId, classOfferingId);
        if (c is null) return null;

        var items = await (
            from a in _db.Assessments.AsNoTracking()
            join cc in _db.ClassComponents on a.ClassComponentId equals cc.Id
            join gp in _db.GradingPeriods on a.GradingPeriodId equals gp.Id
            where cc.ClassOfferingId == c.Id
            orderby gp.SortOrder, cc.SortOrder, a.Title
            select new AssessmentListItem
            {
                Id = (int)a.Id,
                PeriodName = gp.Name,
                PeriodOrder = gp.SortOrder,
                ComponentName = cc.Name,
                Title = a.Title,
                MaxScore = a.MaxScore,
                DueDate = a.DueDate,
                PassingPercent = a.PassingPercent,
                IsRequired = a.IsRequired == true,
                IsActive = a.IsActive == true,
                ScoreRows = _db.Scores.Count(s => s.AssessmentId == a.Id)
            }).ToListAsync();

        return new AssessmentsViewModel
        {
            ClassOfferingId = classOfferingId,
            Header = await HeaderAsync(c),
            AttendanceMode = c.AttendanceMode,
            Items = items
        };
    }

    private async Task<List<ClassComponent>> AllowedComponentsAsync(ClassOffering c)
    {
        var activityBased = c.AttendanceMode == "ActivityBased";
        return await _db.ClassComponents.AsNoTracking()
            .Where(x => x.ClassOfferingId == c.Id && x.IsActive == true
                        && (activityBased || x.IsAttendance != true))
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
    }

    public async Task<bool> LoadFormAsync(Guid userId, AssessmentCreateViewModel vm)
    {
        var c = await OwnedClassAsync(userId, vm.ClassOfferingId);
        if (c is null) return false;

        vm.Header = await HeaderAsync(c);
        vm.Components = (await AllowedComponentsAsync(c))
            .Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();
        vm.Periods = await _db.GradingPeriods.AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .Select(p => new SelectListItem(p.Name, p.Id.ToString()))
            .ToListAsync();
        return true;
    }

    public async Task<AssessmentResult> CreateAsync(Guid userId, AssessmentCreateViewModel vm)
    {
        var c = await OwnedClassAsync(userId, vm.ClassOfferingId);
        if (c is null)
            return new(false, "", "This class is not available.");

        // Only the current term can be changed.
        var termIsCurrent = await _db.AcademicTerms.AsNoTracking()
            .AnyAsync(t => t.Id == c.TermId && t.IsCurrent == true);
        if (!termIsCurrent)
            return new(false, "", "Assessments can only be added in the current term.");

        var title = (vm.Title ?? "").Trim();
        if (title.Length == 0)
            return new(false, nameof(vm.Title), "Title is required.");

        var max = vm.MaxScore ?? 0m;
        if (max <= 0 || max > 9999.99m || decimal.Round(max, 2) != max)
            return new(false, nameof(vm.MaxScore),
                "Maximum score must be above 0, at most 9999.99, with up to 2 decimals.");

        var pass = vm.PassingPercent ?? -1m;
        if (pass < 0 || pass > 100 || decimal.Round(pass, 2) != pass)
            return new(false, nameof(vm.PassingPercent),
                "Passing percent must be between 0 and 100.");

        // The component must be one of THIS class's allowed components:
        // never trust the id that came from the form.
        var componentId = (uint)(vm.ComponentId ?? 0);
        var component = (await AllowedComponentsAsync(c))
            .FirstOrDefault(x => x.Id == componentId);
        if (component is null)
            return new(false, nameof(vm.ComponentId), "Choose a valid component.");

        var periodId = (uint)(vm.PeriodId ?? 0);
        var period = await _db.GradingPeriods.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == periodId);
        if (period is null)
            return new(false, nameof(vm.PeriodId), "Choose a valid grading period.");

        if (await _db.Assessments.AnyAsync(a =>
                a.ClassComponentId == component.Id &&
                a.GradingPeriodId == period.Id && a.Title == title))
            return new(false, nameof(vm.Title),
                "That component already has an assessment with this title in this period.");

        var entity = new Assessment
        {
            ClassComponentId = component.Id,
            GradingPeriodId = period.Id,
            Title = title,
            MaxScore = max,
            DueDate = vm.DueDate,
            PassingPercent = pass,
            IsRequired = vm.IsRequired,
            IsActive = true
        };
        _db.Assessments.Add(entity);

        _audit.Add("AssessmentCreated", "Assessment", title,
            newValues: new
            {
                classOfferingId = c.Id,
                component = component.Name,
                period = period.Name,
                title,
                max,
                pass,
                dueDate = vm.DueDate,
                required = vm.IsRequired
            });

        try
        {
            await _db.SaveChangesAsync();   // assessment + audit row, one transaction
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not create assessment for class {ClassId}", c.Id);
            return new(false, "", "The assessment could not be saved. Nothing was changed.");
        }

        return new(true, null, null);
    }

    // ---------- 5B-2 ----------

    private sealed record Owned(Assessment A, ClassComponent C, ClassOffering O);

    // Assessment -> component -> class -> must be THIS faculty member's class.
    private async Task<Owned?> OwnedAssessmentAsync(Guid userId, int assessmentId)
    {
        var id = (uint)assessmentId;
        var a = await _db.Assessments.FirstOrDefaultAsync(x => x.Id == id);   // tracked
        if (a is null) return null;

        var comp = await _db.ClassComponents.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == a.ClassComponentId);
        if (comp is null) return null;

        var offering = await OwnedClassAsync(userId, (int)comp.ClassOfferingId);
        return offering is null ? null : new Owned(a, comp, offering);
    }

    private Task<bool> TermIsCurrentAsync(ClassOffering o) =>
        _db.AcademicTerms.AsNoTracking()
            .AnyAsync(t => t.Id == o.TermId && t.IsCurrent == true);

    private Task<string> PeriodNameAsync(uint periodId) =>
        _db.GradingPeriods.AsNoTracking()
            .Where(p => p.Id == periodId).Select(p => p.Name).FirstAsync();

    private Task<int> ScoreRowsAsync(uint assessmentId) =>
        _db.Scores.CountAsync(s => s.AssessmentId == assessmentId);

    public async Task<AssessmentEditViewModel?> GetEditAsync(Guid userId, int assessmentId)
    {
        var x = await OwnedAssessmentAsync(userId, assessmentId);
        if (x is null) return null;

        var vm = new AssessmentEditViewModel
        {
            AssessmentId = assessmentId,
            Title = x.A.Title,
            MaxScore = x.A.MaxScore,
            DueDate = x.A.DueDate,
            PassingPercent = x.A.PassingPercent,
            IsRequired = x.A.IsRequired == true
        };
        await FillEditDisplayAsync(userId, vm);
        return vm;
    }

    public async Task<bool> FillEditDisplayAsync(Guid userId, AssessmentEditViewModel vm)
    {
        var x = await OwnedAssessmentAsync(userId, vm.AssessmentId);
        if (x is null) return false;

        vm.ClassOfferingId = (int)x.O.Id;
        vm.Header = await HeaderAsync(x.O);
        vm.ComponentName = x.C.Name;
        vm.PeriodName = await PeriodNameAsync(x.A.GradingPeriodId);
        vm.ScoreRows = await ScoreRowsAsync(x.A.Id);
        return true;
    }

    public async Task<AssessmentResult> UpdateAsync(Guid userId, AssessmentEditViewModel vm)
    {
        var x = await OwnedAssessmentAsync(userId, vm.AssessmentId);
        if (x is null) return new(false, "", "This assessment is not available.");

        if (!await TermIsCurrentAsync(x.O))
            return new(false, "", "Assessments can only be changed in the current term.");

        var title = (vm.Title ?? "").Trim();
        if (title.Length == 0)
            return new(false, nameof(vm.Title), "Title is required.");

        var max = vm.MaxScore ?? 0m;
        if (max <= 0 || max > 9999.99m || decimal.Round(max, 2) != max)
            return new(false, nameof(vm.MaxScore),
                "Maximum score must be above 0, at most 9999.99, with up to 2 decimals.");

        var pass = vm.PassingPercent ?? -1m;
        if (pass < 0 || pass > 100 || decimal.Round(pass, 2) != pass)
            return new(false, nameof(vm.PassingPercent),
                "Passing percent must be between 0 and 100.");

        var a = x.A;

        if (await _db.Assessments.AnyAsync(o =>
                o.Id != a.Id && o.ClassComponentId == a.ClassComponentId &&
                o.GradingPeriodId == a.GradingPeriodId && o.Title == title))
            return new(false, nameof(vm.Title),
                "Another assessment in this component and period already has this title.");

        // A lower maximum must not invalidate scores that are already entered.
        var highest = await _db.Scores
            .Where(s => s.AssessmentId == a.Id)
            .MaxAsync(s => (decimal?)s.RawScore);
        if (highest is not null && max < highest.Value)
            return new(false, nameof(vm.MaxScore),
                $"A student already has {highest.Value:0.##}. The maximum cannot be lower than that.");

        var changed = a.Title != title || a.MaxScore != max || a.DueDate != vm.DueDate
                      || a.PassingPercent != pass || (a.IsRequired == true) != vm.IsRequired;
        if (!changed)
            return new(false, "", "Nothing was changed.");

        // Decide from CURRENT database state: do scores exist right now?
        var hasScores = await ScoreRowsAsync(a.Id) > 0;
        string? reasonText = null;
        if (hasScores)
        {
            var reason = await _reasons.ValidateAsync("ScoreEdit", vm.Reason);
            if (!reason.Success)
                return new(false, "Reason.ReasonId", reason.Error!);
            reasonText = reason.Text;
        }

        var old = new
        {
            title = a.Title,
            max = a.MaxScore,
            dueDate = a.DueDate,
            pass = a.PassingPercent,
            required = a.IsRequired == true
        };

        a.Title = title;
        a.MaxScore = max;
        a.DueDate = vm.DueDate;
        a.PassingPercent = pass;
        a.IsRequired = vm.IsRequired;

        _audit.Add("AssessmentUpdated", "Assessment", a.Id.ToString(),
            oldValues: old,
            newValues: new { title, max, dueDate = vm.DueDate, pass, required = vm.IsRequired },
            reason: reasonText);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not update assessment {Id}", a.Id);
            return new(false, "", "The change could not be saved. Nothing was changed.");
        }
        return new(true, null, null);
    }

    public async Task<AssessmentStatusViewModel?> GetStatusViewAsync(
        Guid userId, int assessmentId, bool activate)
    {
        var x = await OwnedAssessmentAsync(userId, assessmentId);
        if (x is null) return null;

        return new AssessmentStatusViewModel
        {
            AssessmentId = assessmentId,
            ClassOfferingId = (int)x.O.Id,
            Title = x.A.Title,
            ComponentName = x.C.Name,
            PeriodName = await PeriodNameAsync(x.A.GradingPeriodId),
            ScoreRows = await ScoreRowsAsync(x.A.Id),
            IsActive = x.A.IsActive == true,
            Activate = activate
        };
    }

    public async Task<AssessmentResult> ChangeStatusAsync(Guid userId, AssessmentStatusViewModel vm)
    {
        var x = await OwnedAssessmentAsync(userId, vm.AssessmentId);
        if (x is null) return new(false, "", "This assessment is not available.");

        if (!await TermIsCurrentAsync(x.O))
            return new(false, "", "Assessments can only be changed in the current term.");

        var a = x.A;
        var currentlyActive = a.IsActive == true;
        if (currentlyActive == vm.Activate)
            return new(false, "", vm.Activate
                ? "This assessment is already active. Nothing was changed."
                : "This assessment is already inactive. Nothing was changed.");

        var reason = await _reasons.ValidateAsync(vm.Activate ? "Reactivate" : "Deactivate", vm.Reason);
        if (!reason.Success)
            return new(false, "Reason.ReasonId", reason.Error!);

        a.IsActive = vm.Activate;

        _audit.Add(vm.Activate ? "AssessmentReactivated" : "AssessmentDeactivated",
            "Assessment", a.Id.ToString(),
            oldValues: new { isActive = currentlyActive },
            newValues: new { isActive = vm.Activate, title = a.Title },
            reason: reason.Text);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not change status of assessment {Id}", a.Id);
            return new(false, "", "The change could not be saved. Nothing was changed.");
        }
        return new(true, null, null);
    }

}