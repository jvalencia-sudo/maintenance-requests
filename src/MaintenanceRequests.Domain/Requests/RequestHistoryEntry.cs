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

    public Guid Id { get; private set; }

    public HistoryEventType EventType { get; private set; }

    public RequestStatus? FromStatus { get; private set; }

    public RequestStatus? ToStatus { get; private set; }

    public Guid? PreviousAssigneeId { get; private set; }

    public Guid? NewAssigneeId { get; private set; }

    public Guid ActorId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    internal static RequestHistoryEntry Created(Guid actorId, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        EventType = HistoryEventType.Created,
        ToStatus = RequestStatus.Pending,
        ActorId = actorId,
        OccurredAt = now
    };

    internal static RequestHistoryEntry StatusChanged(
        RequestStatus from, RequestStatus to, Guid actorId, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        EventType = HistoryEventType.StatusChanged,
        FromStatus = from,
        ToStatus = to,
        ActorId = actorId,
        OccurredAt = now
    };

    internal static RequestHistoryEntry AssigneeChanged(
        Guid? previousAssigneeId, Guid newAssigneeId, Guid actorId, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        EventType = HistoryEventType.AssigneeChanged,
        PreviousAssigneeId = previousAssigneeId,
        NewAssigneeId = newAssigneeId,
        ActorId = actorId,
        OccurredAt = now
    };
}
