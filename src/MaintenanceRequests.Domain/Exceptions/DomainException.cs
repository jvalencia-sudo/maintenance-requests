namespace MaintenanceRequests.Domain.Exceptions;

/// <summary>
/// Base type for business rule violations. <see cref="Code"/> is a stable,
/// machine-readable identifier that the API exposes to clients.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
