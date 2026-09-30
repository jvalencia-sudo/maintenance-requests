using MaintenanceRequests.Application.Abstractions;
using MaintenanceRequests.Application.Requests;
using MaintenanceRequests.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequests.Infrastructure.Persistence;

internal sealed class MaintenanceRequestQueries(AppDbContext dbContext) : IMaintenanceRequestQueries
{
    private const string LikeEscapeCharacter = @"\";

    public async Task<PagedResult<RequestListItemDto>> ListAsync(
        RequestListQuery query, CancellationToken cancellationToken)
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

        var totalItems = await requests.CountAsync(cancellationToken);

        // Id breaks ties between equal timestamps so pagination is deterministic.
        var ordered = query.Sort == SortDirection.Asc
            ? requests.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id)
            : requests.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id);

        var items = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new RequestListItemDto(
                r.Id,
                r.Title,
                r.Category,
                r.Priority,
                r.Status,
                dbContext.Users.Where(u => u.Id == r.RequesterId).Select(u => u.Name).FirstOrDefault()!,
                dbContext.Users.Where(u => u.Id == r.AssigneeId).Select(u => u.Name).FirstOrDefault(),
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<RequestListItemDto>(items, query.Page, query.PageSize, totalItems);
    }

    public async Task<RequestDetailDto?> GetDetailAsync(int id, CancellationToken cancellationToken)
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
                RequesterName = dbContext.Users.Where(u => u.Id == r.RequesterId).Select(u => u.Name).FirstOrDefault()!,
                r.AssigneeId,
                AssigneeName = dbContext.Users.Where(u => u.Id == r.AssigneeId).Select(u => u.Name).FirstOrDefault(),
                r.CreatedAt,
                r.Version,
                History = r.History
                    .OrderBy(h => h.OccurredAt)
                    .ThenBy(h => h.Id)
                    .Select(h => new RequestHistoryItemDto(
                        h.Id,
                        h.EventType,
                        h.FromStatus,
                        h.ToStatus,
                        h.PreviousAssigneeId,
                        dbContext.Users.Where(u => u.Id == h.PreviousAssigneeId).Select(u => u.Name).FirstOrDefault(),
                        h.NewAssigneeId,
                        dbContext.Users.Where(u => u.Id == h.NewAssigneeId).Select(u => u.Name).FirstOrDefault(),
                        h.ActorId,
                        dbContext.Users.Where(u => u.Id == h.ActorId).Select(u => u.Name).FirstOrDefault()!,
                        h.OccurredAt))
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        // Derived from the transition map in memory: the database knows nothing about the lifecycle.
        return new RequestDetailDto(
            row.Id,
            row.Title,
            row.Description,
            row.Category,
            row.Priority,
            row.Status,
            row.RequesterId,
            row.RequesterName,
            row.AssigneeId,
            row.AssigneeName,
            row.CreatedAt,
            row.Version,
            StatusTransitions.From(row.Status),
            !StatusTransitions.IsTerminal(row.Status),
            row.History);
    }

    public async Task<RequestSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var counts = await dbContext.MaintenanceRequests
            .AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        // Every status is present, with 0 when there are no requests in it.
        var byStatus = Enum.GetValues<RequestStatus>()
            .ToDictionary(status => status, status => counts.GetValueOrDefault(status));

        return new RequestSummaryDto(byStatus.Values.Sum(), byStatus);
    }

    /// <summary>Makes user input literal inside a LIKE pattern by escaping \, % and _.</summary>
    private static string EscapeLikePattern(string value) => value
        .Replace(@"\", @"\\")
        .Replace("%", @"\%")
        .Replace("_", @"\_");
}
