namespace MaintenanceRequests.Domain.Requests;

public enum RequestStatus
{
    Pending,
    InProgress,
    OnHold,
    Resolved,
    Cancelled
}
