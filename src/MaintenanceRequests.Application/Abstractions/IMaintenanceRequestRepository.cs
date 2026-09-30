using MaintenanceRequests.Domain.Requests;

namespace MaintenanceRequests.Application.Abstractions;

/// <summary>
/// Write-side access to the aggregate. Every change is made through the aggregate's
/// methods and persisted with <see cref="SaveChangesAsync"/>; there is no generic Update.
/// </summary>
public interface IMaintenanceRequestRepository
{
    Task<MaintenanceRequest?> GetByIdAsync(int id, CancellationToken cancellationToken);

    void Add(MaintenanceRequest request);

    /// <exception cref="Exceptions.ConcurrencyConflictException">
    /// The request was modified by someone else after it was loaded.
    /// </exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
