namespace Spmis.Web.Services;

public interface IAuditLogger
{
    // Adds a log row to the current unit of work. Does NOT save.
    // The caller's own SaveChangesAsync() commits it together with the change.
    void Add(
        string actionType,
        string entityType,
        string? entityId,
        object? oldValues = null,
        object? newValues = null,
        string? reason = null,
        Guid? actingUserId = null,
        string? actingRole = null);

    // For events with no business change to pair with (e.g. a failed login).
    // Saves everything pending in the context, so use it only when
    // nothing else is waiting to be saved.
    Task AddAndSaveAsync(
        string actionType,
        string entityType,
        string? entityId,
        object? oldValues = null,
        object? newValues = null,
        string? reason = null,
        Guid? actingUserId = null,
        string? actingRole = null);
}