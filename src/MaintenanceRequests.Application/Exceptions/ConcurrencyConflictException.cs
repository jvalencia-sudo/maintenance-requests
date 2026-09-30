namespace MaintenanceRequests.Application.Exceptions;

/// <summary>
/// The request changed after the client read it: either the version it sent is stale,
/// or another write committed first (xmin check on save).
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    private const string DefaultMessage =
        "La solicitud fue modificada por otro usuario. Recarga e intenta de nuevo.";

    public ConcurrencyConflictException()
        : base(DefaultMessage)
    {
    }

    public ConcurrencyConflictException(Exception innerException)
        : base(DefaultMessage, innerException)
    {
    }
}
