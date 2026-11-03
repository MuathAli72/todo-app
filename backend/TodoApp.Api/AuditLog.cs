using System.Diagnostics;

/// <summary>Writes account and security events (logins, lockouts, logouts...) to the audit trail.</summary>
/// <remarks>
/// Data changes (tasks, routines, ticks) are recorded automatically in TodoDbContext.SaveChanges.
/// This is for the events that aren't data changes, so the endpoints call it themselves.
/// </remarks>
public class AuditLog
{
    private readonly TodoDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLog(TodoDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Records one account event. userId is null when no account is involved (e.g. a login with an unknown email).
    /// By default the target is the account itself; pass targetType/targetId for something else (e.g. an endpoint).
    /// </summary>
    public async Task RecordAsync(string action, string outcome, string? userId, string targetType = "Account", string? targetId = null)
    {
        _db.AuditEvents.Add(new AuditEvent
        {
            OccurredAt = DateTime.UtcNow,
            UserId = userId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId ?? userId,
            Outcome = outcome,
            TraceId = Activity.Current?.TraceId.ToString(),
            IpAddress = ClientIp(),
        });
        await _db.SaveChangesAsync();
    }
    // The visitor's real IP. Cloudflare (in front of Render) sets CF-Connecting-IP and overwrites
    // anything the visitor sends, so it can't be faked. When it's missing (e.g. on your laptop),
    // we store no IP rather than a wrong one. A misleading IP is worse than none in an audit trail.
    private string? ClientIp() =>
        _httpContextAccessor.HttpContext?.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
}
