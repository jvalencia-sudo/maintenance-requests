namespace MaintenanceRequests.Domain.Requests;

/// <summary>
/// Single source of truth for the request lifecycle.
/// A status with no outgoing transitions is terminal.
/// </summary>
public static class StatusTransitions
{
    private static readonly IReadOnlyDictionary<RequestStatus, IReadOnlyList<RequestStatus>> Map =
        new Dictionary<RequestStatus, IReadOnlyList<RequestStatus>>
        {
            [RequestStatus.Pending] = [RequestStatus.InProgress, RequestStatus.Cancelled],
            [RequestStatus.InProgress] = [RequestStatus.OnHold, RequestStatus.Resolved, RequestStatus.Cancelled],
            [RequestStatus.OnHold] = [RequestStatus.InProgress, RequestStatus.Cancelled],
            [RequestStatus.Resolved] = [],
            [RequestStatus.Cancelled] = []
        };

    public static IReadOnlyList<RequestStatus> From(RequestStatus status) =>
        Map.TryGetValue(status, out var targets) ? targets : [];

    public static bool IsAllowed(RequestStatus from, RequestStatus to) => From(from).Contains(to);

    public static bool IsTerminal(RequestStatus status) => From(status).Count == 0;
}
