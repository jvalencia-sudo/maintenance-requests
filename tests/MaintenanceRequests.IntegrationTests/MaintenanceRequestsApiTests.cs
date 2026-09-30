using System.Net;
using System.Text.Json;
using MaintenanceRequests.Application.Requests;
using MaintenanceRequests.Domain.Requests;
using MaintenanceRequests.IntegrationTests.Support;
using static MaintenanceRequests.IntegrationTests.Support.ApiClient;

namespace MaintenanceRequests.IntegrationTests;

// Each test creates its own request and only asserts on it, never on global counts.
public class MaintenanceRequestsApiTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    [Fact]
    public async Task MainFlow_CreateChangeStatusAndRejectInvalidTransition()
    {
        // POST: 201, Location header, starts Pending.
        var createResponse = await CreateAsync(_client);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await ReadAsync<MaintenanceRequestDetailDto>(createResponse);
        Assert.Equal($"{BasePath}/{created.Id}", createResponse.Headers.Location?.AbsolutePath);
        Assert.Equal(RequestStatus.Pending, created.Status);

        // PATCH to InProgress with the version just read: 200.
        var changeResponse = await ChangeStatusAsync(
            _client, created.Id, RequestStatus.InProgress, created.Version, TechnicianId);
        Assert.Equal(HttpStatusCode.OK, changeResponse.StatusCode);

        // GET: two history entries in order, with actor names resolved.
        var detail = await GetDetailAsync(_client, created.Id);
        Assert.Equal(RequestStatus.InProgress, detail.Status);
        Assert.Collection(
            detail.History,
            entry =>
            {
                Assert.Equal(HistoryEventType.Created, entry.Type);
                Assert.Equal(RequestStatus.Pending, entry.ToStatus);
                Assert.Equal(RequesterName, entry.Actor.Name);
            },
            entry =>
            {
                Assert.Equal(HistoryEventType.StatusChanged, entry.Type);
                Assert.Equal(RequestStatus.Pending, entry.FromStatus);
                Assert.Equal(RequestStatus.InProgress, entry.ToStatus);
                Assert.Equal(TechnicianName, entry.Actor.Name);
            });

        // PATCH back to Pending is not an allowed transition: 409, history untouched.
        var invalidResponse = await ChangeStatusAsync(_client, created.Id, RequestStatus.Pending, detail.Version);
        Assert.Equal(HttpStatusCode.Conflict, invalidResponse.StatusCode);
        var problem = await ReadAsync<JsonElement>(invalidResponse);
        Assert.Equal("invalid_status_transition", problem.GetProperty("code").GetString());

        var afterRejection = await GetDetailAsync(_client, created.Id);
        Assert.Equal(RequestStatus.InProgress, afterRejection.Status);
        Assert.Equal(2, afterRejection.History.Count);
    }

    [Fact]
    public async Task Create_IgnoresOverPostedFields()
    {
        // Fields the client must not control: the server assigns id, status and dates.
        var body = new Dictionary<string, object>(ValidCreateBody)
        {
            ["id"] = 999,
            ["status"] = "Resolved",
            ["createdAt"] = "2000-01-01T00:00:00Z"
        };

        var before = DateTimeOffset.UtcNow;
        var response = await PostAsync(_client, body);
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await ReadAsync<MaintenanceRequestDetailDto>(response);
        Assert.NotEqual(999, created.Id);
        Assert.Equal(RequestStatus.Pending, created.Status);
        Assert.InRange(created.CreatedAt, before.AddSeconds(-1), after.AddSeconds(1));
        Assert.Equal(RequesterId, created.Requester.Id);
    }

    [Fact]
    public async Task Mutation_WithoutUserHeader_ReturnsUnauthorized()
    {
        var created = await ReadAsync<MaintenanceRequestDetailDto>(await CreateAsync(_client));

        var createResponse = await PostAsync(_client, ValidCreateBody, userId: null);
        var changeResponse = await ChangeStatusAsync(
            _client, created.Id, RequestStatus.InProgress, created.Version, userId: null);

        Assert.Equal(HttpStatusCode.Unauthorized, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, changeResponse.StatusCode);
        var detail = await GetDetailAsync(_client, created.Id);
        Assert.Equal(RequestStatus.Pending, detail.Status);
    }

    [Fact]
    public async Task Create_WithFourCharacterTitle_ReturnsFieldErrorInCamelCase()
    {
        var body = new Dictionary<string, object>(ValidCreateBody) { ["title"] = "abcd" };

        var response = await PostAsync(_client, body);

        // The domain's DomainValidationException.Field reaches the JSON as errors.title.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await ReadAsync<JsonElement>(response);
        var titleErrors = problem.GetProperty("errors").GetProperty("title");
        Assert.Equal(JsonValueKind.Array, titleErrors.ValueKind);
        Assert.NotEqual(0, titleErrors.GetArrayLength());
    }

    [Fact]
    public async Task ChangeStatus_WithStaleVersion_ReturnsConcurrencyConflict()
    {
        var created = await ReadAsync<MaintenanceRequestDetailDto>(await CreateAsync(_client));
        var staleVersion = created.Version;

        var firstChange = await ChangeStatusAsync(_client, created.Id, RequestStatus.InProgress, staleVersion);
        Assert.Equal(HttpStatusCode.OK, firstChange.StatusCode);

        // A second client still holding the version it read before the first change.
        var staleChange = await ChangeStatusAsync(_client, created.Id, RequestStatus.Cancelled, staleVersion);

        Assert.Equal(HttpStatusCode.Conflict, staleChange.StatusCode);
        var problem = await ReadAsync<JsonElement>(staleChange);
        Assert.Equal("concurrency_conflict", problem.GetProperty("code").GetString());

        var detail = await GetDetailAsync(_client, created.Id);
        Assert.Equal(RequestStatus.InProgress, detail.Status);
        Assert.Equal(2, detail.History.Count);
    }
}
