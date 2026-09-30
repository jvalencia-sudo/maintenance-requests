using MaintenanceRequests.Application.Abstractions;

namespace MaintenanceRequests.Api.Http;

/// <summary>
/// Reads the caller from the X-User-Id header. There is no authentication in scope:
/// the header only identifies who performs a change so it can be recorded in the history.
/// </summary>
internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public const string HeaderName = "X-User-Id";

    public int? UserId =>
        int.TryParse(httpContextAccessor.HttpContext?.Request.Headers[HeaderName], out var id) ? id : null;
}
