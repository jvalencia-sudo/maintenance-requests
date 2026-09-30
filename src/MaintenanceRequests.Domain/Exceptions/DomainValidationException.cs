namespace MaintenanceRequests.Domain.Exceptions;

public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string field, string message)
        : base("validation_error", message)
    {
        Field = field;
    }

    /// <summary>Name of the invalid field, in camelCase to match the API contract.</summary>
    public string Field { get; }
}
