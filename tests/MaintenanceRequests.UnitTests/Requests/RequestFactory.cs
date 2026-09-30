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
}
