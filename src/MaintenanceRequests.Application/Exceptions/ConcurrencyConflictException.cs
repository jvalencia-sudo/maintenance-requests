namespace MaintenanceRequests.Application.Exceptions;

public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(Exception innerException)
        : base("La solicitud fue modificada por otro usuario. Recarga e intenta de nuevo.", innerException)
    {
    }
}
