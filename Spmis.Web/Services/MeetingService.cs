using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Entities;
using Spmis.Web.Models.ClassRecord;

namespace Spmis.Web.Services;



public class MeetingService : IMeetingService
{
    private const string FinalizedMessage =
        "This class has been finalized. No further changes are allowed.";
    private readonly SpmisDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IReasonService _reasons;
    private readonly ILogger<MeetingService> _logger;

    public MeetingService(SpmisDbContext db, IAuditLogger audit,
        IReasonService reasons, ILogger<MeetingService> logger)
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
            .Select(f => (uint?)f.Id).FirstOrDefaultAsync();
        if (facultyId is null) return null;

        var id = (uint)classId;
        return await _db.ClassOfferings.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && c.FacultyId == facultyId
                                      && c.IsActive == true);
    }

    private Task<string> HeaderAsync(ClassOffering c) =>
        _db.Subjects.AsNoTracking().Where(s => s.Id == c.SubjectId)
            .Select(s => s.Code + " - " + s.Title).FirstAsync();

    private Task<bool> TermIsCurrentAsync(ClassOffering c) =>
        _db.AcademicTerms.AsNoTracking()
            .AnyAsync(t => t.Id == c.TermId && t.IsCurrent == true);

    private async Task<List<(DateOnly Start, DateOnly End, string Name)>> PeriodsAsync(uint termId)
    {
        var rows = await (
            from tp in _db.TermPeriods.AsNoTracking()
            where tp.TermId == termId
            join g in _db.GradingPeriods on tp.GradingPeriodId equals g.Id
            orderby g.SortOrder
            select new { tp.StartDate, tp.EndDate, g.Name }).ToListAsync();
        return rows.Select(r => (r.StartDate, r.EndDate, r.Name)).ToList();
    }

    private static string PeriodOf(List<(DateOnly Start, DateOnly End, string Name)> periods, DateOnly d)
    {
        foreach (var p in periods)
            if (d >= p.Start && d <= p.End) return p.Name;
        return "—";
    }

    public async Task<MeetingsViewModel?> GetAsync(Guid userId, int classOfferingId)
    {
        var c = await OwnedClassAsync(userId, classOfferingId);
        if (c is null) return null;

        var periods = await PeriodsAsync(c.TermId);
        var rows = await (
            from m in _db.ClassMeetings.AsNoTracking()
            where m.ClassOfferingId == c.Id
            join a in _db.Assessments on m.LinkedAssessmentId equals a.Id into aj
            from a in aj.DefaultIfEmpty()
            orderby m.MeetingDate descending
            select new
            {
                m.Id,
                m.MeetingDate,
                m.Topic,
                Linked = a == null ? null : a.Title,
                m.IsActive,
                Records = _db.AttendanceRecords.Count(r => r.MeetingId == m.Id)
            }).ToListAsync();

        return new MeetingsViewModel
        {
            ClassOfferingId = classOfferingId,
            Header = await HeaderAsync(c),
            AttendanceMode = c.AttendanceMode,
            Items = rows.Select(r => new MeetingListItem
            {
                Id = (int)r.Id,
                Date = r.MeetingDate,
                PeriodName = PeriodOf(periods, r.MeetingDate),
                Topic = r.Topic,
                LinkedAssessment = r.Linked,
                IsActive = r.IsActive == true,
                RecordCount = r.Records
            }).ToList()
        };
    }

    public async Task<bool> LoadFormAsync(Guid userId, MeetingCreateViewModel vm)
    {
        var c = await OwnedClassAsync(userId, vm.ClassOfferingId);
        if (c is null) return false;

        vm.Header = await HeaderAsync(c);
        vm.AttendanceMode = c.AttendanceMode;
        vm.Assessments = c.AttendanceMode != "ActivityBased"
            ? new()
            : await (
                from a in _db.Assessments.AsNoTracking()
                join cc in _db.ClassComponents on a.ClassComponentId equals cc.Id
                where cc.ClassOfferingId == c.Id && a.IsActive == true
                orderby a.Title
                select new SelectListItem(a.Title + " (" + cc.Name + ")", a.Id.ToString()))
                .ToListAsync();
        return true;
    }

    public async Task<MeetingResult> CreateAsync(Guid userId, MeetingCreateViewModel vm)
    {
        var c = await OwnedClassAsync(userId, vm.ClassOfferingId);
        if (c is null) return new(false, "", "This class is not available.");

        if (c.Status == "Finalized")
            return new(false, "", FinalizedMessage);

        if (!await TermIsCurrentAsync(c))
            return new(false, "", "Meetings can only be added in the current term.");

        if (vm.MeetingDate is null)
            return new(false, nameof(vm.MeetingDate), "Choose a date.");
        var date = vm.MeetingDate.Value;

        var periods = await PeriodsAsync(c.TermId);
        if (PeriodOf(periods, date) == "—")
            return new(false, nameof(vm.MeetingDate),
                "The date must fall inside one of this term's grading periods.");

        if (await _db.ClassMeetings.AnyAsync(m =>
                m.ClassOfferingId == c.Id && m.MeetingDate == date))
            return new(false, nameof(vm.MeetingDate), "This class already has a meeting on that date.");

        var topic = string.IsNullOrWhiteSpace(vm.Topic) ? null : vm.Topic.Trim();
        if (topic is not null && topic.Length > 150)
            return new(false, nameof(vm.Topic), "Topic is at most 150 characters.");

        uint? linked = null;
        if (c.AttendanceMode == "ActivityBased")
        {
            if (vm.LinkedAssessmentId is null)
                return new(false, nameof(vm.LinkedAssessmentId),
                    "In an activity-based class, link the meeting to an assessment.");

            var aid = (uint)vm.LinkedAssessmentId.Value;
            var ok = await (
                from a in _db.Assessments.AsNoTracking()
                join cc in _db.ClassComponents on a.ClassComponentId equals cc.Id
                where a.Id == aid && a.IsActive == true && cc.ClassOfferingId == c.Id
                select a.Id).AnyAsync();
            if (!ok)
                return new(false, nameof(vm.LinkedAssessmentId), "Choose an assessment of this class.");
            linked = aid;
        }
        // Meetings-mode class: the link stays null, whatever the form posted.

        _db.ClassMeetings.Add(new ClassMeeting
        {
            ClassOfferingId = c.Id,
            MeetingDate = date,
            Topic = topic,
            LinkedAssessmentId = linked,
            IsActive = true
        });

        _audit.Add("MeetingCreated", "ClassMeeting", $"{c.Id}:{date:yyyy-MM-dd}",
            newValues: new { classOfferingId = c.Id, date = date.ToString("yyyy-MM-dd"), topic, linked });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not create meeting for class {Id}", c.Id);
            return new(false, "", "The meeting could not be saved. Nothing was changed.");
        }
        return new(true, null, null);
    }

    private async Task<(ClassMeeting M, ClassOffering O)?> OwnedMeetingAsync(Guid userId, int meetingId)
    {
        var id = (uint)meetingId;
        var m = await _db.ClassMeetings.FirstOrDefaultAsync(x => x.Id == id);   // tracked
        if (m is null) return null;
        var o = await OwnedClassAsync(userId, (int)m.ClassOfferingId);
        return o is null ? null : (m, o);
    }

    public async Task<MeetingStatusViewModel?> GetStatusViewAsync(Guid userId, int meetingId, bool activate)
    {
        var x = await OwnedMeetingAsync(userId, meetingId);
        if (x is null) return null;

        return new MeetingStatusViewModel
        {
            MeetingId = meetingId,
            ClassOfferingId = (int)x.Value.O.Id,
            Date = x.Value.M.MeetingDate,
            Topic = x.Value.M.Topic,
            IsActive = x.Value.M.IsActive == true,
            Activate = activate,
            RecordCount = await _db.AttendanceRecords.CountAsync(r => r.MeetingId == x.Value.M.Id)
        };
    }

    public async Task<MeetingResult> ChangeStatusAsync(Guid userId, MeetingStatusViewModel vm)
    {
        var x = await OwnedMeetingAsync(userId, vm.MeetingId);
        if (x is null) return new(false, "", "This meeting is not available.");
        var (m, o) = x.Value;

        if (!await TermIsCurrentAsync(o))
            return new(false, "", "Meetings can only be changed in the current term.");

        if (o.Status == "Finalized")
            return new(false, "", FinalizedMessage);

        var currentlyActive = m.IsActive == true;
        if (currentlyActive == vm.Activate)
            return new(false, "", vm.Activate
                ? "This meeting is already active. Nothing was changed."
                : "This meeting is already cancelled. Nothing was changed.");

        var reason = await _reasons.ValidateAsync(vm.Activate ? "Reactivate" : "Deactivate", vm.Reason);
        if (!reason.Success)
            return new(false, "Reason.ReasonId", reason.Error!);

        m.IsActive = vm.Activate;

        _audit.Add(vm.Activate ? "MeetingReactivated" : "MeetingCancelled",
            "ClassMeeting", m.Id.ToString(),
            oldValues: new { isActive = currentlyActive },
            newValues: new { isActive = vm.Activate, date = m.MeetingDate.ToString("yyyy-MM-dd") },
            reason: reason.Text);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Could not change meeting {Id}", m.Id);
            return new(false, "", "The change could not be saved. Nothing was changed.");
        }
        return new(true, null, null);
    }
}