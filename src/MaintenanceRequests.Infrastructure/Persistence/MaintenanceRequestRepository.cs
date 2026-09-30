using MaintenanceRequests.Application.Abstractions;
using MaintenanceRequests.Application.Exceptions;
using MaintenanceRequests.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequests.Infrastructure.Persistence;

internal sealed class MaintenanceRequestRepository(AppDbContext dbContext) : IMaintenanceRequestRepository
{
    /// <remarks>
    /// History is not included: no rule of the aggregate reads past entries, and entries
    /// appended to the tracked collection are still detected and inserted on save.
    /// </remarks>
    public Task<MaintenanceRequest?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.MaintenanceRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public void Add(MaintenanceRequest request) => dbContext.MaintenanceRequests.Add(request);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(exception);
        }
    }
}
