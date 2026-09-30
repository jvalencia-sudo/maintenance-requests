using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MaintenanceRequests.Application.Requests;
using MaintenanceRequests.Domain.Requests;

namespace MaintenanceRequests.IntegrationTests.Support;

/// <summary>Thin helpers over HttpClient so each test reads as the HTTP flow it checks.</summary>
internal static class ApiClient
{
    public const string BasePath = "/api/maintenance-requests";

    public const int RequesterId = 1;
    public const string RequesterName = "Ana Gómez";
    public const int TechnicianId = 3;
    public const string TechnicianName = "Laura Martínez";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static Task<HttpResponseMessage> CreateAsync(HttpClient client, int userId = RequesterId) =>
        SendAsync(client, HttpMethod.Post, BasePath, userId, new
        {
            title = "Aire acondicionado sin enfriar",
            description = "El equipo de la sala de juntas enciende pero no enfría.",
            category = "Equipment",
            priority = "High"
        });

    public static Task<HttpResponseMessage> ChangeStatusAsync(
        HttpClient client, int id, RequestStatus targetStatus, uint version, int userId = RequesterId) =>
        SendAsync(client, HttpMethod.Patch, $"{BasePath}/{id}/status", userId, new { targetStatus, version });

    public static async Task<MaintenanceRequestDetailDto> GetDetailAsync(HttpClient client, int id)
    {
        var detail = await client.GetFromJsonAsync<MaintenanceRequestDetailDto>($"{BasePath}/{id}", JsonOptions);
        return detail!;
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;

    private static Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethod method, string path, int userId, object body)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        request.Headers.Add("X-User-Id", userId.ToString());
        return client.SendAsync(request);
    }
}
