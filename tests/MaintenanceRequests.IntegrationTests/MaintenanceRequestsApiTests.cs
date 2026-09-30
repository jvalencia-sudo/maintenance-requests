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
