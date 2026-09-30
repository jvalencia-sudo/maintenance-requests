using MaintenanceRequests.Domain.Requests;

namespace MaintenanceRequests.Application.Requests;

public enum SortDirection
{
    Desc,
    Asc
}

/// <summary>
/// Filters for the request list. Each filter takes a single value and they combine with AND.
/// The shape (ranges, enum values) is validated at the API boundary before reaching this type.
/// </summary>
public sealed record MaintenanceRequestListQuery(
    RequestStatus? Status = null,
    RequestPriority? Priority = null,
    RequestCategory? Category = null,
    string? Search = null,
    SortDirection SortDirection = SortDirection.Desc,
    int Page = 1,
    int PageSize = 10);
