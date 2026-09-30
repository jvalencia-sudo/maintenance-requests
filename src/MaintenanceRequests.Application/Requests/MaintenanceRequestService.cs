using MaintenanceRequests.Application.Abstractions;
using MaintenanceRequests.Application.Exceptions;
using MaintenanceRequests.Domain.Exceptions;
using MaintenanceRequests.Domain.Requests;

namespace MaintenanceRequests.Application.Requests;

/// <summary>
/// Write use cases. It resolves the actor, loads the aggregate, delegates the change to it
/// and persists; the business rules themselves stay in <see cref="MaintenanceRequest"/>.
/// </summary>
public sealed class MaintenanceRequestService(
    IMaintenanceRequestRepository requests,
    IMaintenanceRequestQueries queries,
    IUserRepository users,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<MaintenanceRequestDetailDto> CreateAsync(
        CreateMaintenanceRequestDto dto, CancellationToken cancellationToken)
    {
        var actorId = await ResolveActorAsync(cancellationToken);

        var request = MaintenanceRequest.Create(
            dto.Title!,
            dto.Description!,
            Required(dto.Category, "category"),
            Required(dto.Priority, "priority"),
            actorId,
            timeProvider.GetUtcNow());

        requests.Add(request);
        await requests.SaveChangesAsync(cancellationToken);

        return await GetDetailAsync(request.Id, cancellationToken);
    }

    public async Task<MaintenanceRequestDetailDto> ChangeStatusAsync(
        int id, ChangeStatusDto dto, CancellationToken cancellationToken)
    {
        var actorId = await ResolveActorAsync(cancellationToken);
        var request = await LoadForUpdateAsync(id, Required(dto.Version, "version"), cancellationToken);

        request.ChangeStatus(Required(dto.TargetStatus, "targetStatus"), actorId, timeProvider.GetUtcNow());
        await requests.SaveChangesAsync(cancellationToken);

        return await GetDetailAsync(id, cancellationToken);
    }

    public async Task<MaintenanceRequestDetailDto> AssignAsync(
        int id, AssignDto dto, CancellationToken cancellationToken)
    {
        var actorId = await ResolveActorAsync(cancellationToken);
        var request = await LoadForUpdateAsync(id, Required(dto.Version, "version"), cancellationToken);

        var assigneeId = Required(dto.AssigneeId, "assigneeId");
        if (!await users.ExistsAsync(assigneeId, cancellationToken))
        {
            throw new DomainValidationException("assigneeId", $"No existe un usuario con id {assigneeId}.");
        }

        request.Assign(assigneeId, actorId, timeProvider.GetUtcNow());
        await requests.SaveChangesAsync(cancellationToken);

        return await GetDetailAsync(id, cancellationToken);
    }

    private async Task<int> ResolveActorAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } actorId || !await users.ExistsAsync(actorId, cancellationToken))
        {
            throw new ActorNotFoundException();
        }

        return actorId;
    }

    /// <summary>
    /// Loads the aggregate and rejects the change if the client read an older version.
    /// A write that commits in between is still caught by the xmin check on save.
    /// </summary>
    private async Task<MaintenanceRequest> LoadForUpdateAsync(
        int id, uint expectedVersion, CancellationToken cancellationToken)
    {
        var request = await requests.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("una solicitud", id);

        if (request.Version != expectedVersion)
        {
            throw new ConcurrencyConflictException();
        }

        return request;
    }

    private async Task<MaintenanceRequestDetailDto> GetDetailAsync(int id, CancellationToken cancellationToken) =>
        await queries.GetDetailAsync(id, cancellationToken)
        ?? throw new NotFoundException("una solicitud", id);

    // The API already rejects missing fields; this keeps the service safe for any other caller.
    private static T Required<T>(T? value, string field)
        where T : struct =>
        value ?? throw new DomainValidationException(field, $"El campo {field} es obligatorio.");
}
