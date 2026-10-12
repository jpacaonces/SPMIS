using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Grading;
using Spmis.Web.Models.Portal;
using EngineRow = Spmis.Web.Grading.TransmutationRow;   // the entity has the same name
using EngineTable = Spmis.Web.Grading.TransmutationTable;
namespace Spmis.Web.Services;

public class WhatIfService : IWhatIfService
{
    private readonly SpmisDbContext _db;
    private readonly IGradeCalculationService _grades;

    public WhatIfService(SpmisDbContext db, IGradeCalculationService grades)
    {
        _db = db;
        _grades = grades;
    }

    private Task<uint?> StudentIdAsync(Guid userId) =>
        _db.Students.AsNoTracking()
            .Where(s => s.UserId == userId && s.IsActive == true)
            .Select(s => (uint?)s.Id).FirstOrDefaultAsync();

    public async Task<WhatIfViewModel?> BuildAsync(
        Guid userId, int enrollmentId, IReadOnlyDictionary<string, string?>? assumed)
    {
        var studentId = await StudentIdAsync(userId);
        if (studentId is null) return null;

        // Layer 2 of authorization: the enrollment must be THIS student's.
        var id = (uint)enrollmentId;
        var e = await _db.Enrollments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.StudentId == studentId && x.Status == "Enrolled");
        if (e is null) return null;

        var c = await _db.ClassOfferings.AsNoTracking().FirstAsync(x => x.Id == e.ClassOfferingId);
        var header = await _db.Subjects.AsNoTracking().Where(s => s.Id == c.SubjectId)
            .Select(s => s.Code + " - " + s.Title).FirstAsync();

        var vm = new WhatIfViewModel { EnrollmentId = enrollmentId, Header = header };

        if (c.Status == "Finalized")
        {
            vm.Blocked = "This class is finalized. Your grade is final, so there is nothing to predict.";
            return vm;
        }

        var row = await _grades.GetStudentGradeAsync(enrollmentId);
        if (row is null) return null;
        var periods = row.Semester.Periods;

        // Transmutation table of THIS class (same source as the real calculation).
        var tableRows = await _db.TransmutationRows.AsNoTracking()
            .Where(r => r.TableId == c.TransmutationTableId)
            .Select(r => new { r.MinRaw, r.ReportedGrade, r.IsPassing })
            .ToListAsync();
        var table = new EngineTable(
            tableRows.Select(r => new EngineRow(r.MinRaw, r.ReportedGrade, r.IsPassing == true)));
        var passingRows = tableRows.Where(r => r.IsPassing == true).ToList();
        if (passingRows.Count == 0)
        {
            vm.Blocked = "This class has no passing grade defined, so a prediction is not possible.";
            return vm;
        }
        var passRaw = passingRows.Min(r => r.MinRaw);

        var open = periods.Where(p => p.Status != PeriodStatus.Complete).ToList();
        if (open.Count == 0)
        {
            vm.Blocked = "All your grading periods are already complete. Your grade will be final once the teacher finalizes the class.";
            return vm;
        }

        foreach (var p in periods)
        {
            string? typed = null;
            if (assumed is not null && p.Status != PeriodStatus.Complete)
                assumed.TryGetValue(p.Code, out typed);

            vm.Periods.Add(new WhatIfPeriodRow
            {
                Code = p.Code,
                Status = p.Status.ToString(),
                CurrentRaw = p.RawGrade,
                IsLocked = p.Status == PeriodStatus.Complete,
                Assumed = typed
            });
        }

        if (assumed is null) return vm;   // first visit: show the form only

        // Baseline for blank boxes: average of the periods that already have a raw grade.
        var known = periods.Where(p => p.RawGrade is not null).Select(p => p.RawGrade!.Value).ToList();
        decimal? baseline = known.Count > 0 ? known.Average() : null;

        // Parse what the student typed (server-side: the browser is never trusted).
        var chosen = new Dictionary<string, decimal>();
        foreach (var r in vm.Periods.Where(r => !r.IsLocked))
        {
            if (string.IsNullOrWhiteSpace(r.Assumed))
            {
                if (baseline is null)
                {
                    vm.Error = $"You have no grade yet, so enter a value for {r.Code}.";
                    return vm;
                }
                chosen[r.Code] = baseline.Value;
            }
            else if (decimal.TryParse(r.Assumed.Trim(), NumberStyles.Number,
                         CultureInfo.InvariantCulture, out var v) && v >= 0m && v <= 100m)
            {
                chosen[r.Code] = v;
            }
            else
            {
                vm.Error = $"{r.Code}: enter a number from 0 to 100.";
                return vm;
            }
        }

        var engine = new GradingEngine(new GradingOptions());

        WhatIfScenario Run(string label, Func<PeriodResult, decimal> pick)
        {
            // Open periods get the assumed raw grade; complete periods stay as they are.
            var list = periods.Select(p => p.Status == PeriodStatus.Complete
                ? p
                : new PeriodResult(p.Code, PeriodStatus.Complete, null, null, pick(p), [])).ToList();
            var sem = engine.ComputeSemester(list, table);
            return new WhatIfScenario
            {
                Label = label,
                Raw = sem.RawGrade!.Value,
                Reported = sem.ReportedGrade!.Value,
                IsPassing = sem.IsPassing == true
            };
        }

        vm.Scenarios.Add(Run("Your assumption", p => chosen[p.Code]));
        vm.Scenarios.Add(Run("Best case (100 in every open period)", _ => 100m));
        vm.Scenarios.Add(Run("Worst case (0 in every open period)", _ => 0m));

        // Average needed on the open periods to reach the passing raw grade.
        var fixedSum = periods.Where(p => p.Status == PeriodStatus.Complete).Sum(p => p.RawGrade!.Value);
        var need = (passRaw * periods.Count - fixedSum) / open.Count;
        need = Math.Ceiling(need * 100m) / 100m;   // round UP so the advice is never too optimistic

        vm.NeededMessage =
            need <= 0m ? "You pass even with 0 in every remaining period."
            : need > 100m ? "Passing is no longer reachable this semester, even with 100 in every remaining period."
            : $"To reach the passing raw grade ({passRaw:0.##}) you need an average of about {need:0.00} across the {open.Count} remaining period(s).";

        return vm;
    }
}