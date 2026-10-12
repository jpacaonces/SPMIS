using Microsoft.EntityFrameworkCore;
using Spmis.Web.Data.Auth;
using Spmis.Web.Data.Entities;
using Spmis.Web.Models.Admin;

namespace Spmis.Web.Services;

public class AuditLogQueryService : IAuditLogQueryService
{
    private const int PageSize = 25;

    private readonly SpmisDbContext _db;

    public AuditLogQueryService(SpmisDbContext db)
    {
        _db = db;
    }

    public async Task<AuditLogListViewModel> GetPageAsync(
        DateOnly? dateFrom, DateOnly? dateTo,
        string? actionType, string? role, string? search, int page)
    {
        // The Action dropdown lists only types that exist in the table.
        var actionTypes = await _db.ActivityLogs
            .AsNoTracking()
            .Select(l => l.ActionType)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        // Never trust the query string: accept only known values.
        var cleanAction = actionType is not null && actionTypes.Contains(actionType)
            ? actionType : null;
        var cleanRole = RoleNames.All.Contains(role) ? role : null;
        var cleanSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        // If the dates were entered the wrong way round, swap them.
        if (dateFrom is not null && dateTo is not null && dateFrom > dateTo)
            (dateFrom, dateTo) = (dateTo, dateFrom);

        var query = _db.ActivityLogs.AsNoTracking();

        if (dateFrom is not null)
        {
            var start = dateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(l => l.OccurredAt >= start);
        }

        if (dateTo is not null)
        {
            // "To" is inclusive of the whole day: stop before the NEXT midnight.
            var end = dateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(l => l.OccurredAt < end);
        }

        if (cleanAction is not null)
            query = query.Where(l => l.ActionType == cleanAction);

        if (cleanRole is not null)
            query = query.Where(l => l.Role == cleanRole);

        if (cleanSearch is not null)
        {
            query = query.Where(l =>
                (l.User != null && (l.User.UserName.Contains(cleanSearch) ||
                                    l.User.FirstName.Contains(cleanSearch) ||
                                    l.User.LastName.Contains(cleanSearch))) ||
                (l.EntityId != null && l.EntityId.Contains(cleanSearch)) ||
                (l.Reason != null && l.Reason.Contains(cleanSearch)));
        }

        var totalCount = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        var currentPage = Math.Clamp(page, 1, totalPages);

        var items = await query
            .OrderByDescending(l => l.OccurredAt)
            .ThenByDescending(l => l.Id)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(l => new AuditLogItem
            {
                OccurredAt = l.OccurredAt,
                UserLabel = l.User != null
                    ? l.User.LastName + ", " + l.User.FirstName + " (" + l.User.UserName + ")"
                    : "(not signed in)",
                Role = l.Role,
                ActionType = l.ActionType,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                OldValues = l.OldValues,
                NewValues = l.NewValues,
                Reason = l.Reason
            })
            .ToListAsync();

        return new AuditLogListViewModel
        {
            Items = items,
            DateFrom = dateFrom,
            DateTo = dateTo,
            ActionType = cleanAction,
            Role = cleanRole,
            Search = cleanSearch,
            ActionTypes = actionTypes,
            Page = currentPage,
            TotalPages = totalPages,
            TotalCount = totalCount
        };
    }
}