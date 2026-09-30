namespace MaintenanceRequests.Application.Abstractions;

/// <summary>The user performing the current operation, as identified by the caller.</summary>
public interface ICurrentUser
{
    /// <summary>The caller's user id, or null when it was not provided or is not a valid id.</summary>
    int? UserId { get; }
}
