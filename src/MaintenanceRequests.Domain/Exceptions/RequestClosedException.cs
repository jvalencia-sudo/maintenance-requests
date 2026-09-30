using MaintenanceRequests.Domain.Requests;

namespace MaintenanceRequests.Domain.Exceptions;

public sealed class RequestClosedException : DomainException
{
    public RequestClosedException(RequestStatus status)
        : base("request_closed", $"La solicitud está en estado {status} y ya no admite cambios.")
    {
        Status = status;
    }

    public RequestStatus Status { get; }
}
