using System.Text.Json;
using Spmis.Web.Data.Entities;

namespace Spmis.Web.Services;

public class AuditLogger : IAuditLogger
{
    private readonly SpmisDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AuditLogger(SpmisDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public void Add(
        string actionType,
        string entityType,
        string? entityId,
        object? oldValues = null,
        object? newValues = null,
        string? reason = null,
        Guid? actingUserId = null,
        string? actingRole = null)
    {
        // occurred_at is left alone: the database fills it with its own clock.
        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = actingUserId ?? _currentUser.UserId,
            Role = actingRole ?? _currentUser.Role,
            ActionType = actionType,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = Serialize(oldValues),
            NewValues = Serialize(newValues),
            Reason = reason
        });
    }

    public async Task AddAndSaveAsync(
        string actionType,
        string entityType,
        string? entityId,
        object? oldValues = null,
        object? newValues = null,
        string? reason = null,
        Guid? actingUserId = null,
        string? actingRole = null)
    {
        Add(actionType, entityType, entityId, oldValues, newValues, reason,
            actingUserId, actingRole);
        await _db.SaveChangesAsync();
    }

    private static string? Serialize(object? value) =>
        value is null ? null : JsonSerializer.Serialize(value);
}
