namespace MaintenanceRequests.Domain.Exceptions;

public sealed class SameAssigneeException : DomainException
{
    public SameAssigneeException(int assigneeId)
        : base("same_assignee", "La solicitud ya está asignada a ese responsable.")
    {
        AssigneeId = assigneeId;
    }

    public int AssigneeId { get; }
}
