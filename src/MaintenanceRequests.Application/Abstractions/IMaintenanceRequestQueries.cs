using MaintenanceRequests.Application.Requests;

namespace MaintenanceRequests.Application.Abstractions;

/// <summary>
/// Read side: projects straight to DTOs without loading the aggregate,
/// since reads have no business rules to protect.
/// </summary>
public interface IMaintenanceRequestQueries
{
    Task<PagedResult<RequestListItemDto>> ListAsync(RequestListQuery query, CancellationToken cancellationToken);

    Task<RequestDetailDto?> GetDetailAsync(int id, CancellationToken cancellationToken);

    Task<RequestSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}
