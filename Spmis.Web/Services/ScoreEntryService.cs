using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Models.ClassRecord;

namespace Spmis.Web.Services;

public class ScoreEntryService : IScoreEntryService
{
    private static readonly string[] Statuses =
        { "Pending", "Submitted", "Missing", "Excused", "Violation" };

    private readonly SpmisDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IReasonService _reasons;
    private readonly ILogger<ScoreEntryService> _logger;

    public ScoreEntryService(SpmisDbContext db, IAuditLogger audit,
        IReasonService reasons, ILogger<ScoreEntryService> logger)
    {
        _db = db;
        _audit = audit;
        _reasons = reasons;
        _logger = logger;
    }

    private sealed record Owned(Assessment A, ClassComponent C, ClassOffering O);

    // Assessment -> component -> class -> must belong to THIS faculty member.
    private async Task<Owned?> OwnedAsync(Guid userId, int assessmentId)
    {
        var facultyId = await _db.Faculties.AsNoTracking()
            .Where(f => f.UserId == userId && f.IsActive == true)
            .Select(f => (uint?)f.Id).FirstOrDefaultAsync();
        if (facultyId is null) return null;

        var id = (uint)assessmentId;
        var a = await _db.Assessments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (a is null) return null;

        var comp = await _db.ClassComponents.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == a.ClassComponentId);
        if (comp is null) return null;

        var offering = await _db.ClassOfferings.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == comp.ClassOfferingId
                                      && c.FacultyId == facultyId && c.IsActive == true);
        return offering is null ? null : new Owned(a, comp, offering);
    }

    private async Task<List<(uint EnrollmentId, string StudentNo, string Name)>> EnrolledAsync(uint classId)
    {
        var rows = await (
            from e in _db.Enrollments.AsNoTracking()
            where e.ClassOfferingId == classId && e.Status == "Enrolled"
            join s in _db.Students on e.StudentId equals s.Id
            join u in _db.Users on s.UserId equals u.Id
            orderby s.StudentNo
            select new { e.Id, s.StudentNo, u.LastName, u.FirstName }).ToListAsync();
        return rows.Select(r => (r.Id, r.StudentNo, $"{r.LastName}, {r.FirstName}")).ToList();
    }

    private async Task FillHeaderAsync(Owned x, ScoreEntryViewModel vm)
    {
        vm.AssessmentId = (int)x.A.Id;
        vm.ClassOfferingId = (int)x.O.Id;
        vm.Header = await _db.Subjects.AsNoTracking()
            .Where(s => s.Id == x.O.SubjectId)
            .Select(s => s.Code + " - " + s.Title).FirstAsync();
        vm.Title = x.A.Title;
        vm.ComponentName = x.C.Name;
        vm.PeriodName = await _db.GradingPeriods.AsNoTracking()
            .Where(p => p.Id == x.A.GradingPeriodId).Select(p => p.Name).FirstAsync();
        vm.MaxScore = x.A.MaxScore;
        vm.DueDate = x.A.DueDate;
    }

    public async Task<ScoreEntryViewModel?> LoadAsync(Guid userId, int assessmentId)
    {
        var x = await OwnedAsync(userId, assessmentId);
        if (x is null) return null;

        var vm = new ScoreEntryViewModel();
        await FillHeaderAsync(x, vm);

        var students = await EnrolledAsync(x.O.Id);
        var scores = await _db.Scores.AsNoTracking()
            .Where(s => s.AssessmentId == x.A.Id).ToListAsync();
        var byEnrollment = scores.ToDictionary(s => s.EnrollmentId);

        foreach (var st in students)
        {
            byEnrollment.TryGetValue(st.EnrollmentId, out var s);
            vm.Rows.Add(new ScoreRowInput
            {
                EnrollmentId = (int)st.EnrollmentId,
                ScoreId = s is null ? 0 : (int)s.Id,
                RowVersionTicks = s?.RowVersion.Ticks ?? 0,
                Status = s?.Status ?? "Pending",
                RawScore = s?.RawScore,
                SubmittedAt = s?.SubmittedAt,
                StudentNo = st.StudentNo,
                StudentName = st.Name
            });
        }
        return vm;
    }

    public async Task<bool> FillDisplayAsync(Guid userId, ScoreEntryViewModel vm)
    {
        var x = await OwnedAsync(userId, vm.AssessmentId);
        if (x is null) return false;

        await FillHeaderAsync(x, vm);
        var names = (await EnrolledAsync(x.O.Id)).ToDictionary(s => (int)s.EnrollmentId);
        foreach (var r in vm.Rows)
        {
            if (names.TryGetValue(r.EnrollmentId, out var n))
            {
                r.StudentNo = n.StudentNo;
                r.StudentName = n.Name;
            }
        }
        return true;
    }

    // Compare date-times to the minute: the browser's datetime-local drops seconds.
    private static DateTime? ToMinute(DateTime? d) =>
        d is null ? null : new DateTime(d.Value.Year, d.Value.Month, d.Value.Day,
                                        d.Value.Hour, d.Value.Minute, 0);

    private sealed record Change(Score Score, bool IsNew, bool Recorded,
        string OldStatus, decimal? OldRaw, DateTime? OldAt);

    public async Task<ScoreSaveResult> SaveAsync(Guid userId, ScoreEntryViewModel vm)
    {
        var x = await OwnedAsync(userId, vm.AssessmentId);
        if (x is null) return new(false, "", "This assessment is not available.", 0);

        if (x.A.IsActive != true)
            return new(false, "", "This assessment is inactive. Reactivate it to enter scores.", 0);

        var termIsCurrent = await _db.AcademicTerms.AsNoTracking()
            .AnyAsync(t => t.Id == x.O.TermId && t.IsCurrent == true);
        if (!termIsCurrent)
            return new(false, "", "Scores can only be changed in the current term.", 0);

        var enrolled = (await EnrolledAsync(x.O.Id)).Select(s => s.EnrollmentId).ToHashSet();
        var existing = await _db.Scores.Where(s => s.AssessmentId == x.A.Id)   // tracked
            .ToDictionaryAsync(s => s.EnrollmentId);

        var seen = new HashSet<int>();
        var changes = new List<Change>();
        var now = DateTime.Now;
        now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);

        foreach (var row in vm.Rows)
        {
            if (!seen.Add(row.EnrollmentId) || !enrolled.Contains((uint)row.EnrollmentId))
                return new(false, "", "The form contained a student who is not in this class. Reload the page.", 0);

            if (!Statuses.Contains(row.Status))
                return new(false, "", "Invalid status in the form. Reload the page.", 0);

            existing.TryGetValue((uint)row.EnrollmentId, out var current);

            // Stale-page / two-people-editing checks.
            if (current is null && row.ScoreId != 0)
                return new(false, "", "The scores changed while this page was open. Reload and try again.", 0);
            if (current is not null &&
                (row.ScoreId != (int)current.Id || row.RowVersionTicks != current.RowVersion.Ticks))
                return new(false, "",
                    $"{current.EnrollmentId}: a score was changed by someone else while this page was open. " +
                    "Nothing was saved. Reload the page and try again.", 0);

            // Normalise what the faculty member asked for.
            decimal? newRaw = null;
            DateTime? newAt = null;
            if (row.Status == "Submitted")
            {
                newRaw = row.RawScore;
                if (newRaw is not null)
                {
                    if (newRaw < 0 || newRaw > x.A.MaxScore || decimal.Round(newRaw.Value, 2) != newRaw)
                        return new(false, "", $"Student row {row.EnrollmentId}: the score must be between 0 and " +
                            $"{x.A.MaxScore:0.##} with up to 2 decimals.", 0);
                }
                newAt = row.SubmittedAt ?? now;
            }

            var oldStatus = current?.Status ?? "Pending";
            var oldRaw = current?.RawScore;
            var oldAt = current?.SubmittedAt;

            var changed = oldStatus != row.Status || oldRaw != newRaw
                          || ToMinute(oldAt) != ToMinute(newAt);
            if (!changed) continue;

            // Keep the stored seconds when only the display precision differed.
            if (newAt is not null && ToMinute(oldAt) == ToMinute(newAt)) newAt = oldAt;

            var score = current ?? new Score
            {
                AssessmentId = x.A.Id,
                EnrollmentId = (uint)row.EnrollmentId
            };
            score.Status = row.Status;
            score.RawScore = newRaw;
            score.SubmittedAt = newAt;

            changes.Add(new Change(score, current is null, oldStatus != "Pending",
                oldStatus, oldRaw, oldAt));
        }

        if (changes.Count == 0)
            return new(false, "", "Nothing was changed.", 0);

        // Corrections to already-recorded scores need a reason (decided from the DB).
        string? reasonText = null;
        if (changes.Any(c => c.Recorded))
        {
            var reason = await _reasons.ValidateAsync("ScoreEdit", vm.Reason);
            if (!reason.Success)
                return new(false, "Reason.ReasonId", reason.Error!, 0);
            reasonText = reason.Text;
        }

        foreach (var c in changes)
        {
            if (c.IsNew) _db.Scores.Add(c.Score);

            _audit.Add(c.Recorded ? "ScoreEdited" : "ScoreEntered", "Score",
                $"{x.A.Id}:{c.Score.EnrollmentId}",
                oldValues: new { status = c.OldStatus, raw = c.OldRaw, submittedAt = c.OldAt },
                newValues: new
                {
                    assessmentId = x.A.Id,
                    enrollmentId = c.Score.EnrollmentId,
                    status = c.Score.Status,
                    raw = c.Score.RawScore,
                    submittedAt = c.Score.SubmittedAt
                },
                reason: c.Recorded ? reasonText : null);
        }

        try
        {
            await _db.SaveChangesAsync();    // all scores + all audit rows: one transaction
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrent score edit on assessment {Id}", x.A.Id);
            return new(false, "", "Someone changed these scores at the same time. Nothing was saved. Reload and try again.", 0);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not save scores for assessment {Id}", x.A.Id);
            return new(false, "", "The scores could not be saved. Nothing was changed.", 0);
        }

        return new(true, null, null, changes.Count);
    }

    // ---------- 5C-2: AI similarity ----------

    private async Task<(decimal Accepted, decimal Revision)> BandLimitsAsync()
    {
        var rows = await _db.SystemSettings.AsNoTracking()
            .Where(s => s.SettingKey == "similarity.accepted_max"
                     || s.SettingKey == "similarity.revision_max")
            .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue);

        decimal Read(string key, decimal fallback) =>
            !rows.TryGetValue(key, out var v) || v is null
                ? fallback
                : decimal.Parse(v, System.Globalization.CultureInfo.InvariantCulture);

        var accepted = Read("similarity.accepted_max", 25m);
        var revision = Read("similarity.revision_max", 50m);
        if (accepted < 0 || revision < accepted || revision > 100)
            throw new InvalidOperationException("The similarity band settings are invalid.");
        return (accepted, revision);
    }

    private static string Band(decimal pct, decimal accepted, decimal revision) =>
        pct <= accepted ? "Accepted"
        : pct <= revision ? "Accepted with revision"
        : "Not accepted";

    public async Task<SimilarityViewModel?> GetSimilarityAsync(
        Guid userId, int assessmentId, int enrollmentId)
    {
        var x = await OwnedAsync(userId, assessmentId);
        if (x is null) return null;

        // The student must be enrolled in THIS class.
        var student = (await EnrolledAsync(x.O.Id))
            .Where(s => s.EnrollmentId == (uint)enrollmentId)
            .Select(s => new { s.StudentNo, s.Name })
            .FirstOrDefault();
        if (student is null) return null;

        var (accepted, revision) = await BandLimitsAsync();
        var header = await _db.Subjects.AsNoTracking()
            .Where(s => s.Id == x.O.SubjectId)
            .Select(s => s.Code + " - " + s.Title).FirstAsync();

        var score = await _db.Scores.AsNoTracking()
            .FirstOrDefaultAsync(s => s.AssessmentId == x.A.Id
                                      && s.EnrollmentId == (uint)enrollmentId);

        var vm = new SimilarityViewModel
        {
            AssessmentId = assessmentId,
            EnrollmentId = enrollmentId,
            Header = header,
            AssessmentTitle = x.A.Title,
            StudentNo = student.StudentNo,
            StudentName = student.Name,
            ScoreStatus = score?.Status ?? "Pending",
            CanCheck = score is not null && score.Status == "Submitted",
            AcceptedMax = accepted,
            RevisionMax = revision
        };

        if (score is not null)
        {
            var checks = await (
                from c in _db.SubmissionChecks.AsNoTracking()
                where c.ScoreId == score.Id
                join u in _db.Users on c.CheckedBy equals u.Id
                orderby c.CheckedAt descending, c.Id descending
                select new { c.CheckedAt, c.SimilarityPct, c.Note, u.LastName, u.FirstName })
                .ToListAsync();

            vm.History = checks.Select(c => new SimilarityHistoryItem
            {
                CheckedAt = c.CheckedAt,
                Percent = c.SimilarityPct,
                Band = Band(c.SimilarityPct, accepted, revision),
                CheckedBy = $"{c.LastName}, {c.FirstName}",
                Note = c.Note
            }).ToList();
        }
        return vm;
    }

    public async Task<ScoreSaveResult> AddSimilarityAsync(Guid userId, SimilarityViewModel vm)
    {
        var x = await OwnedAsync(userId, vm.AssessmentId);
        if (x is null) return new(false, "", "This assessment is not available.", 0);

        var termIsCurrent = await _db.AcademicTerms.AsNoTracking()
            .AnyAsync(t => t.Id == x.O.TermId && t.IsCurrent == true);
        if (!termIsCurrent)
            return new(false, "", "Checks can only be recorded in the current term.", 0);

        var enrolled = (await EnrolledAsync(x.O.Id))
            .Any(s => s.EnrollmentId == (uint)vm.EnrollmentId);
        if (!enrolled)
            return new(false, "", "That student is not enrolled in this class.", 0);

        // Decide from the CURRENT database state.
        var score = await _db.Scores.AsNoTracking()
            .FirstOrDefaultAsync(s => s.AssessmentId == x.A.Id
                                      && s.EnrollmentId == (uint)vm.EnrollmentId);
        if (score is null || score.Status != "Submitted")
            return new(false, "", "A similarity check needs a Submitted score.", 0);

        var pct = vm.Percent ?? -1m;
        if (pct < 0 || pct > 100 || decimal.Round(pct, 2) != pct)
            return new(false, nameof(vm.Percent),
                "The percentage must be between 0 and 100, with up to 2 decimals.", 0);

        var note = string.IsNullOrWhiteSpace(vm.Note) ? null : vm.Note.Trim();
        var (accepted, revision) = await BandLimitsAsync();
        var band = Band(pct, accepted, revision);

        _db.SubmissionChecks.Add(new SubmissionCheck
        {
            ScoreId = score.Id,
            SimilarityPct = pct,
            CheckedBy = userId,
            CheckedAt = DateTime.Now,
            Note = note
        });

        _audit.Add("SimilarityChecked", "Score", $"{x.A.Id}:{vm.EnrollmentId}",
            newValues: new { assessmentId = x.A.Id, enrollmentId = vm.EnrollmentId, pct, band, note });

        try
        {
            await _db.SaveChangesAsync();   // check row + audit row, one transaction
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not save similarity check for score {Id}", score.Id);
            return new(false, "", "The check could not be saved. Nothing was changed.", 0);
        }
        return new(true, null, null, 1);
    }

}