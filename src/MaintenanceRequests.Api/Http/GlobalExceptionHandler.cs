using System.Diagnostics;
using MaintenanceRequests.Application.Exceptions;
using MaintenanceRequests.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceRequests.Api.Http;

/// <summary>
/// Single place that turns exceptions into HTTP responses, so controllers stay free of try/catch.
/// Unexpected errors return a generic message; the details only go to the log.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            DomainValidationException e => new ValidationProblemDetails(
                new Dictionary<string, string[]> { [e.Field] = [e.Message] })
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "La solicitud tiene datos inválidos."
            },
            NotFoundException e => Problem(StatusCodes.Status404NotFound, "Recurso no encontrado.", e.Message),
            InvalidStatusTransitionException or RequestClosedException or SameAssigneeException => WithCode(
                Problem(StatusCodes.Status409Conflict, "La operación no es válida en el estado actual.", exception.Message),
                ((DomainException)exception).Code),
            ConcurrencyConflictException e => WithCode(
                Problem(StatusCodes.Status409Conflict, "Conflicto de concurrencia.", e.Message),
                "concurrency_conflict"),
            ActorNotFoundException e => Problem(StatusCodes.Status401Unauthorized, "Usuario no identificado.", e.Message),
            _ => null
        };

        // traceId links the response the client sees to the server log entry.
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        if (problem is null)
        {
            logger.LogError(exception, "Unhandled exception on {Method} {Path} (traceId {TraceId})",
                httpContext.Request.Method, httpContext.Request.Path, traceId);
            problem = Problem(StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.", null);
        }

        problem.Extensions["traceId"] = traceId;
        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Problem(int status, string title, string? detail) =>
        new() { Status = status, Title = title, Detail = detail };

    private static ProblemDetails WithCode(ProblemDetails problem, string code)
    {
        problem.Extensions["code"] = code;
        return problem;
    }
}
