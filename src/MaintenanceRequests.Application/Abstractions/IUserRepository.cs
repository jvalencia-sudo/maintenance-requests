using MaintenanceRequests.Application.Users;

namespace MaintenanceRequests.Application.Abstractions;

public interface IUserRepository
{
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken);
}
