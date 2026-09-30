namespace MaintenanceRequests.Domain.Requests;

/// <summary>
/// Immutable record of something that happened to a request.
/// Factories are internal: only the <see cref="MaintenanceRequest"/> aggregate can write history.
/// </summary>
public sealed class RequestHistoryEntry
{
    private RequestHistoryEntry()
    {
    }

    public long Id { get; private set; }

    public HistoryEventType EventType { get; private set; }

    public RequestStatus? FromStatus { get; private set; }

    public RequestStatus? ToStatus { get; private set; }

    public int? PreviousAssigneeId { get; private set; }

    public int? NewAssigneeId { get; private set; }

    public int ActorId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    internal static RequestHistoryEntry Created(int actorId, DateTimeOffset now) => new()
    {
        EventType = HistoryEventType.Created,
        ToStatus = RequestStatus.Pending,
        ActorId = actorId,
        OccurredAt = now
    };

    internal static RequestHistoryEntry StatusChanged(
        RequestStatus from, RequestStatus to, int actorId, DateTimeOffset now) => new()
    {
        EventType = HistoryEventType.StatusChanged,
        FromStatus = from,
        ToStatus = to,
        ActorId = actorId,
        OccurredAt = now
    };

    internal static RequestHistoryEntry AssigneeChanged(
        int? previousAssigneeId, int newAssigneeId, int actorId, DateTimeOffset now) => new()
    {
        EventType = HistoryEventType.AssigneeChanged,
        PreviousAssigneeId = previousAssigneeId,
        NewAssigneeId = newAssigneeId,
        ActorId = actorId,
        OccurredAt = now
    };
}
