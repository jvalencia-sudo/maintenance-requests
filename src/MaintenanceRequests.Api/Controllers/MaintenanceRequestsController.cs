using MaintenanceRequests.Api.Contracts;
using MaintenanceRequests.Application.Abstractions;
using MaintenanceRequests.Application.Exceptions;
using MaintenanceRequests.Application.Requests;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceRequests.Api.Controllers;

[ApiController]
[Route("api/maintenance-requests")]
[Produces("application/json")]
public sealed class MaintenanceRequestsController(
    MaintenanceRequestService service,
    IMaintenanceRequestQueries queries) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MaintenanceRequestListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<MaintenanceRequestListItemDto>>> List(
        [FromQuery] MaintenanceRequestListParameters parameters, CancellationToken cancellationToken) =>
        Ok(await queries.ListAsync(parameters.ToQuery(), cancellationToken));

    [HttpGet("summary")]
    [ProducesResponseType(typeof(SummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SummaryDto>> Summary(CancellationToken cancellationToken) =>
        Ok(await queries.GetSummaryAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MaintenanceRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaintenanceRequestDetailDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await queries.GetDetailAsync(id, cancellationToken) ?? throw new NotFoundException("una solicitud", id));

    [HttpPost]
    [ProducesResponseType(typeof(MaintenanceRequestDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MaintenanceRequestDetailDto>> Create(
        CreateMaintenanceRequestDto dto, CancellationToken cancellationToken)
    {
        var created = await service.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(MaintenanceRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MaintenanceRequestDetailDto>> ChangeStatus(
        int id, ChangeStatusDto dto, CancellationToken cancellationToken) =>
        Ok(await service.ChangeStatusAsync(id, dto, cancellationToken));

    [HttpPatch("{id:int}/assignee")]
    [ProducesResponseType(typeof(MaintenanceRequestDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MaintenanceRequestDetailDto>> Assign(
        int id, AssignDto dto, CancellationToken cancellationToken) =>
        Ok(await service.AssignAsync(id, dto, cancellationToken));
}
