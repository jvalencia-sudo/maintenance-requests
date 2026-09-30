namespace MaintenanceRequests.Application.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string resource, int id)
        : base($"No existe {resource} con id {id}.")
    {
    }
}
