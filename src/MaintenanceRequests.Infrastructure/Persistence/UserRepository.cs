using MaintenanceRequests.Application.Abstractions;
using MaintenanceRequests.Application.Users;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequests.Infrastructure.Persistence;

internal sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(u => u.Id == id, cancellationToken);

    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken) =>
        await dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .Select(u => new UserDto(u.Id, u.Name))
            .ToListAsync(cancellationToken);
}
