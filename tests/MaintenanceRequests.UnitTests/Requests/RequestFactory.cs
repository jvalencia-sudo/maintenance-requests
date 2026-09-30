using MaintenanceRequests.Domain.Requests;

namespace MaintenanceRequests.UnitTests.Requests;

internal static class RequestFactory
{
    public static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    public static readonly Guid RequesterId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public const string ValidTitle = "Aire acondicionado sin enfriar";

    public const string ValidDescription = "El equipo del piso 3 enciende pero no enfría la sala.";

    public static MaintenanceRequest CreateValid() => MaintenanceRequest.Create(
        ValidTitle,
        ValidDescription,
        RequestCategory.Equipment,
        RequestPriority.High,
        RequesterId,
        Now);

    /// <summary>
    /// Reaches <paramref name="status"/> by walking a valid path through the public API,
    /// so tests never need a public setter. Paths are written by hand, not read from StatusTransitions.
    /// </summary>
    public static MaintenanceRequest CreateIn(RequestStatus status)
    {
        RequestStatus[] path = status switch
        {
            RequestStatus.Pending => [],
            RequestStatus.InProgress => [RequestStatus.InProgress],
            RequestStatus.OnHold => [RequestStatus.InProgress, RequestStatus.OnHold],
            RequestStatus.Resolved => [RequestStatus.InProgress, RequestStatus.Resolved],
            RequestStatus.Cancelled => [RequestStatus.Cancelled],
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        var request = CreateValid();
        foreach (var step in path)
        {
            request.ChangeStatus(step, RequesterId, Now);
        }

        return request;
    }
}
