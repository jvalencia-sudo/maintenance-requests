using MaintenanceRequests.Domain.Requests;

namespace MaintenanceRequests.Application.Requests;

public enum SortDirection
{
    Desc,
    Asc
}

/// <summary>
/// Filters for the request list. Each filter takes a single value and they combine with AND.
/// Shape (page ranges, enum parsing) is validated at the API boundary.
/// </summary>
public sealed record RequestListQuery(
    RequestStatus? Status = null,
    RequestPriority? Priority = null,
    RequestCategory? Category = null,
    string? Search = null,
    SortDirection Sort = SortDirection.Desc,
    int Page = 1,
    int PageSize = 10);
