using MaintenanceRequests.Domain.Requests;

namespace MaintenanceRequests.Domain.Exceptions;

public sealed class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(RequestStatus from, RequestStatus to)
        : base("invalid_status_transition", $"No se puede cambiar el estado de {from} a {to}.")
    {
        From = from;
        To = to;
    }

    public RequestStatus From { get; }

    public RequestStatus To { get; }
}
