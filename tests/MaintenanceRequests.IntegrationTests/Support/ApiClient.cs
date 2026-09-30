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

    /// <summary>Read-only so tests can't alter it for each other; copy it to build a variant.</summary>
    public static readonly IReadOnlyDictionary<string, object> ValidCreateBody = new Dictionary<string, object>
    {
        ["title"] = "Aire acondicionado sin enfriar",
        ["description"] = "El equipo de la sala de juntas enciende pero no enfría.",
        ["category"] = "Equipment",
        ["priority"] = "High"
    };

    public static Task<HttpResponseMessage> CreateAsync(HttpClient client, int userId = RequesterId) =>
        PostAsync(client, ValidCreateBody, userId);

    /// <summary>POST with an arbitrary body; a null <paramref name="userId"/> omits the X-User-Id header.</summary>
    public static Task<HttpResponseMessage> PostAsync(HttpClient client, object body, int? userId = RequesterId) =>
        SendAsync(client, HttpMethod.Post, BasePath, userId, body);

    public static Task<HttpResponseMessage> ChangeStatusAsync(
        HttpClient client, int id, RequestStatus targetStatus, uint version, int? userId = RequesterId) =>
        SendAsync(client, HttpMethod.Patch, $"{BasePath}/{id}/status", userId, new { targetStatus, version });

    public static async Task<MaintenanceRequestDetailDto> GetDetailAsync(HttpClient client, int id)
    {
        var detail = await client.GetFromJsonAsync<MaintenanceRequestDetailDto>($"{BasePath}/{id}", JsonOptions);
        return detail!;
    }

    public static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(JsonOptions))!;

    private static Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethod method, string path, int? userId, object body)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };
        if (userId is { } id)
        {
            request.Headers.Add("X-User-Id", id.ToString());
        }

        return client.SendAsync(request);
    }
}
