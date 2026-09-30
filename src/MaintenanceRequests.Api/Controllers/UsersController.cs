using MaintenanceRequests.Application.Abstractions;
using MaintenanceRequests.Application.Users;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceRequests.Api.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController(IUserRepository users) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken cancellationToken) =>
        Ok(await users.ListAsync(cancellationToken));
}
