using System.ComponentModel.DataAnnotations;
using MaintenanceRequests.Application.Requests;
using MaintenanceRequests.Domain.Requests;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceRequests.Api.Contracts;

/// <summary>
/// Query string of GET /api/maintenance-requests. Explicit names keep the validation
/// error keys in camelCase, matching the parameters the client sent.
/// </summary>
public sealed class MaintenanceRequestListParameters
{
    public const int MaxPageSize = 50;

    [FromQuery(Name = "page")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe ser mayor o igual a 1.")]
    public int Page { get; init; } = 1;

    [FromQuery(Name = "pageSize")]
    [Range(1, MaxPageSize, ErrorMessage = "Debe estar entre 1 y 50.")]
    public int PageSize { get; init; } = 10;

    [FromQuery(Name = "status")]
    [EnumDataType(typeof(RequestStatus), ErrorMessage = ValidationMessages.InvalidValue)]
    public RequestStatus? Status { get; init; }

    [FromQuery(Name = "priority")]
    [EnumDataType(typeof(RequestPriority), ErrorMessage = ValidationMessages.InvalidValue)]
    public RequestPriority? Priority { get; init; }

    [FromQuery(Name = "category")]
    [EnumDataType(typeof(RequestCategory), ErrorMessage = ValidationMessages.InvalidValue)]
    public RequestCategory? Category { get; init; }

    [FromQuery(Name = "search")]
    public string? Search { get; init; }

    [FromQuery(Name = "sortDirection")]
    [EnumDataType(typeof(SortDirection), ErrorMessage = ValidationMessages.InvalidValue)]
    public SortDirection SortDirection { get; init; } = SortDirection.Desc;

    public MaintenanceRequestListQuery ToQuery() =>
        new(Status, Priority, Category, Search, SortDirection, Page, PageSize);
}
