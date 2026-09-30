namespace MaintenanceRequests.Application.Exceptions;

/// <summary>The caller did not identify itself, or the id does not belong to a known user.</summary>
public sealed class ActorNotFoundException : Exception
{
    public ActorNotFoundException()
        : base("Envía el header X-User-Id con el id de un usuario existente.")
    {
    }
}
