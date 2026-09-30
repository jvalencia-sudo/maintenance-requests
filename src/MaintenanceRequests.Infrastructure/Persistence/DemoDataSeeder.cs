using MaintenanceRequests.Domain.Requests;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequests.Infrastructure.Persistence;

/// <summary>
/// Fills an empty database with demo requests. It only uses the aggregate's public
/// methods, so every seeded request follows the same rules and has a full history.
/// </summary>
internal sealed class DemoDataSeeder(AppDbContext dbContext, TimeProvider timeProvider)
{
    private const int RequestCount = 30;

    private static readonly (string Title, string Description, RequestCategory Category)[] Samples =
    [
        ("Aire acondicionado sin enfriar", "El equipo de la sala de juntas enciende pero no enfría.", RequestCategory.Equipment),
        ("Fuga de agua en baño", "Gotea el lavamanos del baño del segundo piso.", RequestCategory.Infrastructure),
        ("Impresora atascada", "La impresora del área contable atasca cada hoja.", RequestCategory.Equipment),
        ("Instalar actualización del ERP", "Aplicar el parche de seguridad publicado por el proveedor.", RequestCategory.Software),
        ("Luminaria fundida en pasillo", "Dos tubos del pasillo norte no encienden.", RequestCategory.Infrastructure),
        ("Cámara de acceso sin imagen", "La cámara de la puerta principal no transmite video.", RequestCategory.Equipment),
        ("Renovar licencias de ofimática", "Las licencias del equipo comercial vencen este mes.", RequestCategory.Software),
        ("Puerta de emergencia trabada", "La barra antipánico del sótano no abre con facilidad.", RequestCategory.Infrastructure),
        ("Portátil no carga batería", "El cargador funciona con otros equipos pero no con este.", RequestCategory.Equipment),
        ("Correo no sincroniza en móvil", "El buzón corporativo no sincroniza desde ayer.", RequestCategory.Software),
        ("Humedad en pared de bodega", "Aparecieron manchas de humedad junto a las estanterías.", RequestCategory.Infrastructure),
        ("Control de acceso rechaza tarjetas", "El lector del piso 4 rechaza tarjetas válidas.", RequestCategory.Equipment),
        ("Reubicar puesto de trabajo", "Trasladar escritorio y red de un colaborador nuevo.", RequestCategory.Other),
        ("VPN se desconecta", "La conexión VPN se cae cada pocos minutos.", RequestCategory.Software),
        ("Revisión de extintores", "Verificar carga y fecha de vencimiento de extintores.", RequestCategory.Other)
    ];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.MaintenanceRequests.AnyAsync(cancellationToken))
        {
            return;
        }

        var userIds = await dbContext.Users.OrderBy(u => u.Id).Select(u => u.Id).ToListAsync(cancellationToken);
        var random = new Random(42);
        var start = timeProvider.GetUtcNow().AddDays(-35);

        for (var i = 0; i < RequestCount; i++)
        {
            var sample = Samples[i % Samples.Length];
            var createdAt = start.AddHours(i * 26);
            var requesterId = userIds[i % userIds.Count];

            var request = MaintenanceRequest.Create(
                sample.Title,
                sample.Description,
                sample.Category,
                (RequestPriority)random.Next(Enum.GetValues<RequestPriority>().Length),
                requesterId,
                createdAt);

            ApplyScenario(request, i % 6, requesterId, userIds, random, createdAt);
            dbContext.MaintenanceRequests.Add(request);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Walks a valid path so the seed covers every status, with events hours apart.</summary>
    private static void ApplyScenario(
        MaintenanceRequest request, int scenario, int requesterId, List<int> userIds, Random random, DateTimeOffset createdAt)
    {
        var assigneeId = userIds[random.Next(userIds.Count)];
        var at = createdAt;
        DateTimeOffset Next() => at = at.AddHours(random.Next(1, 5));

        switch (scenario)
        {
            case 0:
                break;
            case 1:
                request.Assign(assigneeId, requesterId, Next());
                request.ChangeStatus(RequestStatus.InProgress, assigneeId, Next());
                break;
            case 2:
                request.Assign(assigneeId, requesterId, Next());
                request.ChangeStatus(RequestStatus.InProgress, assigneeId, Next());
                request.ChangeStatus(RequestStatus.OnHold, assigneeId, Next());
                break;
            case 3:
                request.Assign(assigneeId, requesterId, Next());
                request.ChangeStatus(RequestStatus.InProgress, assigneeId, Next());
                request.ChangeStatus(RequestStatus.Resolved, assigneeId, Next());
                break;
            case 4:
                request.ChangeStatus(RequestStatus.Cancelled, requesterId, Next());
                break;
            default:
                var secondAssigneeId = userIds.First(id => id != assigneeId);
                request.Assign(assigneeId, requesterId, Next());
                request.ChangeStatus(RequestStatus.InProgress, assigneeId, Next());
                request.ChangeStatus(RequestStatus.OnHold, assigneeId, Next());
                request.Assign(secondAssigneeId, requesterId, Next());
                request.ChangeStatus(RequestStatus.InProgress, secondAssigneeId, Next());
                request.ChangeStatus(RequestStatus.Resolved, secondAssigneeId, Next());
                break;
        }
    }
}
