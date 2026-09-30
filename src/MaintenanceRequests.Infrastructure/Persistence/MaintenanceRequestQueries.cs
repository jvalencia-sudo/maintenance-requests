using MaintenanceRequests.Application.Abstractions;
using MaintenanceRequests.Application.Requests;
using MaintenanceRequests.Application.Users;
using MaintenanceRequests.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequests.Infrastructure.Persistence;

internal sealed class MaintenanceRequestQueries(AppDbContext dbContext) : IMaintenanceRequestQueries
{
    private const string LikeEscapeCharacter = @"\";

    public async Task<PagedResult<MaintenanceRequestListItemDto>> ListAsync(
        MaintenanceRequestListQuery query, CancellationToken cancellationToken)
    {
        var requests = dbContext.MaintenanceRequests.AsNoTracking();

        if (query.Status is { } status)
        {
            requests = requests.Where(r => r.Status == status);
        }

        if (query.Priority is { } priority)
        {
            requests = requests.Where(r => r.Priority == priority);
        }

        if (query.Category is { } category)
        {
            requests = requests.Where(r => r.Category == category);
        }

        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var pattern = $"%{EscapeLikePattern(search)}%";
            requests = requests.Where(r => EF.Functions.ILike(r.Title, pattern, LikeEscapeCharacter));
        }

        var totalCount = await requests.CountAsync(cancellationToken);

        // Id breaks ties between equal timestamps so pagination is deterministic.
        var ordered = query.SortDirection == SortDirection.Asc
            ? requests.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id)
            : requests.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id);

        var rows = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.Category,
                r.Priority,
                r.Status,
                r.AssigneeId,
                AssigneeName = dbContext.Users.Where(u => u.Id == r.AssigneeId).Select(u => u.Name).FirstOrDefault(),
                r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new MaintenanceRequestListItemDto(
                r.Id,
                r.Title,
                r.Category,
                r.Priority,
                r.Status,
                ToUser(r.AssigneeId, r.AssigneeName),
                r.CreatedAt))
            .ToList();

        return new PagedResult<MaintenanceRequestListItemDto>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<MaintenanceRequestDetailDto?> GetDetailAsync(int id, CancellationToken cancellationToken)
    {
        var row = await dbContext.MaintenanceRequests
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.Description,
                r.Category,
                r.Priority,
                r.Status,
                r.RequesterId,
                RequesterName = dbContext.Users.Where(u => u.Id == r.RequesterId).Select(u => u.Name).FirstOrDefault(),
                r.AssigneeId,
                AssigneeName = dbContext.Users.Where(u => u.Id == r.AssigneeId).Select(u => u.Name).FirstOrDefault(),
                r.CreatedAt,
                r.Version,
                History = r.History
                    .OrderBy(h => h.OccurredAt)
                    .ThenBy(h => h.Id)
                    .Select(h => new
                    {
                        h.Id,
                        h.EventType,
                        h.FromStatus,
                        h.ToStatus,
                        h.PreviousAssigneeId,
                        PreviousAssigneeName = dbContext.Users.Where(u => u.Id == h.PreviousAssigneeId).Select(u => u.Name).FirstOrDefault(),
                        h.NewAssigneeId,
                        NewAssigneeName = dbContext.Users.Where(u => u.Id == h.NewAssigneeId).Select(u => u.Name).FirstOrDefault(),
                        h.ActorId,
                        ActorName = dbContext.Users.Where(u => u.Id == h.ActorId).Select(u => u.Name).FirstOrDefault(),
                        h.OccurredAt
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var history = row.History
            .Select(h => new HistoryEntryDto(
                h.Id,
                h.EventType,
                h.FromStatus,
                h.ToStatus,
                ToUser(h.PreviousAssigneeId, h.PreviousAssigneeName),
                ToUser(h.NewAssigneeId, h.NewAssigneeName),
                new UserDto(h.ActorId, h.ActorName!),
                h.OccurredAt))
            .ToList();

        // Derived from the transition map in memory: the database knows nothing about the lifecycle.
        return new MaintenanceRequestDetailDto(
            row.Id,
            row.Title,
            row.Description,
            row.Category,
            row.Priority,
            row.Status,
            new UserDto(row.RequesterId, row.RequesterName!),
            ToUser(row.AssigneeId, row.AssigneeName),
            row.CreatedAt,
            row.Version,
            StatusTransitions.From(row.Status),
            !StatusTransitions.IsTerminal(row.Status),
            history);
    }

    public async Task<SummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var counts = await dbContext.MaintenanceRequests
            .AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        return new SummaryDto(
            counts.Values.Sum(),
            counts.GetValueOrDefault(RequestStatus.Pending),
            counts.GetValueOrDefault(RequestStatus.InProgress),
            counts.GetValueOrDefault(RequestStatus.OnHold),
            counts.GetValueOrDefault(RequestStatus.Resolved),
            counts.GetValueOrDefault(RequestStatus.Cancelled));
    }

    // Foreign keys guarantee the user exists whenever the id does, so the name is never null here.
    private static UserDto? ToUser(int? id, string? name) => id is { } userId ? new UserDto(userId, name!) : null;

    /// <summary>Makes user input literal inside a LIKE pattern by escaping \, % and _.</summary>
    private static string EscapeLikePattern(string value) => value
        .Replace(@"\", @"\\")
        .Replace("%", @"\%")
        .Replace("_", @"\_");
}
