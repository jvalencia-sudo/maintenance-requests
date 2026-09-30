namespace MaintenanceRequests.Domain.Exceptions;

public sealed class SameAssigneeException : DomainException
{
    public SameAssigneeException(Guid assigneeId)
        : base("same_assignee", "La solicitud ya está asignada a ese responsable.")
    {
        AssigneeId = assigneeId;
    }

    public Guid AssigneeId { get; }
}
