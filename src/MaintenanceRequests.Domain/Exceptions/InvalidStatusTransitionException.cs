using MaintenanceRequests.Domain.Requests;

namespace MaintenanceRequests.Domain.Exceptions;

public sealed class InvalidStatusTransitionException : DomainException
{
    public InvalidStatusTransitionException(RequestStatus from, RequestStatus to)
        : base("invalid_status_transition", BuildMessage(from, to))
    {
        From = from;
        To = to;
    }

    public RequestStatus From { get; }

    public RequestStatus To { get; }

    private static string BuildMessage(RequestStatus from, RequestStatus to)
    {
        var allowed = StatusTransitions.From(from);

        return allowed.Count == 0
            ? $"La solicitud está en {from}, un estado final, y ya no admite cambios de estado."
            : $"No se puede cambiar el estado de {from} a {to}. Desde {from} solo se permite: {string.Join(", ", allowed)}.";
    }
}
