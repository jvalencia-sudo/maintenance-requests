using MaintenanceRequests.Application.Requests;

namespace MaintenanceRequests.Application.Abstractions;

/// <summary>
/// Read side: projects straight to DTOs without loading the aggregate,
/// since reads have no business rules to protect.
/// </summary>
public interface IMaintenanceRequestQueries
{
    Task<PagedResult<MaintenanceRequestListItemDto>> ListAsync(
        MaintenanceRequestListQuery query, CancellationToken cancellationToken);

    Task<MaintenanceRequestDetailDto?> GetDetailAsync(int id, CancellationToken cancellationToken);

    Task<SummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
}
