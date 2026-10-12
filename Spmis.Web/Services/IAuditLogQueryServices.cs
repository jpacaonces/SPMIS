using Spmis.Web.Models.Admin;

namespace Spmis.Web.Services;

// READ-ONLY. Writing log rows is IAuditLogger's job, and only inside a transaction.
public interface IAuditLogQueryService
{
    Task<AuditLogListViewModel> GetPageAsync(
        DateOnly? dateFrom, DateOnly? dateTo,
        string? actionType, string? role, string? search, int page);
}