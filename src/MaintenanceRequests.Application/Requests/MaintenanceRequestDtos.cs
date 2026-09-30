using MaintenanceRequests.Application.Users;
using MaintenanceRequests.Domain.Requests;

namespace MaintenanceRequests.Application.Requests;

public sealed record MaintenanceRequestListItemDto(
    int Id,
    string Title,
    RequestCategory Category,
    RequestPriority Priority,
    RequestStatus Status,
    UserDto? Assignee,
    DateTimeOffset CreatedAt);

public sealed record MaintenanceRequestDetailDto(
    int Id,
    string Title,
    string Description,
    RequestCategory Category,
    RequestPriority Priority,
    RequestStatus Status,
    UserDto Requester,
    UserDto? Assignee,
    DateTimeOffset CreatedAt,
    uint Version,
    IReadOnlyList<RequestStatus> AllowedTransitions,
    bool CanAssign,
    IReadOnlyList<HistoryEntryDto> History);

public sealed record HistoryEntryDto(
    long Id,
    HistoryEventType Type,
    RequestStatus? FromStatus,
    RequestStatus? ToStatus,
    UserDto? PreviousAssignee,
    UserDto? NewAssignee,
    UserDto Actor,
    DateTimeOffset OccurredAt);

/// <summary>Global totals, independent of any list filter.</summary>
public sealed record SummaryDto(int Total, int Pending, int InProgress, int OnHold, int Resolved, int Cancelled);
