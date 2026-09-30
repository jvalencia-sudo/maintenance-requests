using MaintenanceRequests.Domain.Exceptions;

namespace MaintenanceRequests.Domain.Requests;

/// <summary>
/// Aggregate root. Every state change goes through a method that validates it
/// and appends the matching history entry in the same operation.
/// </summary>
public sealed class MaintenanceRequest
{
    public const int TitleMinLength = 5;
    public const int TitleMaxLength = 120;
    public const int DescriptionMinLength = 10;
    public const int DescriptionMaxLength = 2000;

    private readonly List<RequestHistoryEntry> _history = [];

    private MaintenanceRequest()
    {
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public RequestCategory Category { get; private set; }

    public RequestPriority Priority { get; private set; }

    public RequestStatus Status { get; private set; }

    public Guid RequesterId { get; private set; }

    public Guid? AssigneeId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<RequestHistoryEntry> History => _history.AsReadOnly();

    public static MaintenanceRequest Create(
        string title,
        string description,
        RequestCategory category,
        RequestPriority priority,
        Guid requesterId,
        DateTimeOffset now)
    {
        var normalizedTitle = NormalizeText(title, "title", "El título", TitleMinLength, TitleMaxLength);
        var normalizedDescription = NormalizeText(
            description, "description", "La descripción", DescriptionMinLength, DescriptionMaxLength);

        if (!Enum.IsDefined(category))
        {
            throw new DomainValidationException("category", "La categoría no es válida.");
        }

        if (!Enum.IsDefined(priority))
        {
            throw new DomainValidationException("priority", "La prioridad no es válida.");
        }

        var request = new MaintenanceRequest
        {
            Id = Guid.NewGuid(),
            Title = normalizedTitle,
            Description = normalizedDescription,
            Category = category,
            Priority = priority,
            Status = RequestStatus.Pending,
            RequesterId = requesterId,
            CreatedAt = now
        };

        request._history.Add(RequestHistoryEntry.Created(requesterId, now));

        return request;
    }

    public void ChangeStatus(RequestStatus target, Guid actorId, DateTimeOffset now)
    {
        if (!StatusTransitions.IsAllowed(Status, target))
        {
            throw new InvalidStatusTransitionException(Status, target);
        }

        var previous = Status;
        Status = target;
        _history.Add(RequestHistoryEntry.StatusChanged(previous, target, actorId, now));
    }

    public void Assign(Guid assigneeId, Guid actorId, DateTimeOffset now)
    {
        if (StatusTransitions.IsTerminal(Status))
        {
            throw new RequestClosedException(Status);
        }

        if (AssigneeId == assigneeId)
        {
            throw new SameAssigneeException(assigneeId);
        }

        var previous = AssigneeId;
        AssigneeId = assigneeId;
        _history.Add(RequestHistoryEntry.AssigneeChanged(previous, assigneeId, actorId, now));
    }

    /// <summary>
    /// Trims the value and checks its length in runes (Unicode scalar values),
    /// so an emoji counts as one character instead of two UTF-16 code units.
    /// </summary>
    private static string NormalizeText(string? value, string field, string label, int minLength, int maxLength)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        var length = trimmed.EnumerateRunes().Count();

        if (length < minLength || length > maxLength)
        {
            throw new DomainValidationException(
                field, $"{label} debe tener entre {minLength} y {maxLength} caracteres.");
        }

        return trimmed;
    }
}
