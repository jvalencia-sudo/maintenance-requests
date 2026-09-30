using MaintenanceRequests.Domain.Requests;

namespace MaintenanceRequests.Application.Requests;

public sealed record RequestListItemDto(
    int Id,
    string Title,
    RequestCategory Category,
    RequestPriority Priority,
    RequestStatus Status,
    string RequesterName,
    string? AssigneeName,
    DateTimeOffset CreatedAt);

public sealed record RequestDetailDto(
    int Id,
    string Title,
    string Description,
    RequestCategory Category,
    RequestPriority Priority,
    RequestStatus Status,
    int RequesterId,
    string RequesterName,
    int? AssigneeId,
    string? AssigneeName,
    DateTimeOffset CreatedAt,
    uint Version,
    IReadOnlyList<RequestStatus> AllowedTransitions,
    bool CanAssign,
    IReadOnlyList<RequestHistoryItemDto> History);

public sealed record RequestHistoryItemDto(
    long Id,
    HistoryEventType Type,
    RequestStatus? FromStatus,
    RequestStatus? ToStatus,
    int? PreviousAssigneeId,
    string? PreviousAssigneeName,
    int? NewAssigneeId,
    string? NewAssigneeName,
    int ActorId,
    string ActorName,
    DateTimeOffset OccurredAt);

/// <summary>Global totals, independent of any list filter.</summary>
public sealed record RequestSummaryDto(int Total, IReadOnlyDictionary<RequestStatus, int> ByStatus);
