using System.ComponentModel.DataAnnotations;
using MaintenanceRequests.Domain.Requests;

namespace MaintenanceRequests.Application.Requests;

// Input contracts. DataAnnotations only check the shape of the request (field present,
// enum value defined); business rules such as lengths and transitions live in the domain.
// Enums are nullable so a missing field is reported instead of silently becoming the default value.
// Init properties rather than positional records, so validation errors use the camelCase JSON names.

public sealed record CreateMaintenanceRequestDto
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    public string? Title { get; init; }

    [Required(ErrorMessage = ValidationMessages.Required)]
    public string? Description { get; init; }

    [Required(ErrorMessage = ValidationMessages.Required)]
    [EnumDataType(typeof(RequestCategory), ErrorMessage = ValidationMessages.InvalidValue)]
    public RequestCategory? Category { get; init; }

    [Required(ErrorMessage = ValidationMessages.Required)]
    [EnumDataType(typeof(RequestPriority), ErrorMessage = ValidationMessages.InvalidValue)]
    public RequestPriority? Priority { get; init; }
}

public sealed record ChangeStatusDto
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EnumDataType(typeof(RequestStatus), ErrorMessage = ValidationMessages.InvalidValue)]
    public RequestStatus? TargetStatus { get; init; }

    [Required(ErrorMessage = ValidationMessages.Required)]
    public uint? Version { get; init; }
}

public sealed record AssignDto
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    public int? AssigneeId { get; init; }

    [Required(ErrorMessage = ValidationMessages.Required)]
    public uint? Version { get; init; }
}

public static class ValidationMessages
{
    public const string Required = "El campo es obligatorio.";
    public const string InvalidValue = "El valor no es válido.";
}
