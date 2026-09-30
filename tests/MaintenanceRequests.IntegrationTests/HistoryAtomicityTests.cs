using System.Net;
using MaintenanceRequests.Application.Requests;
using MaintenanceRequests.Domain.Requests;
using MaintenanceRequests.IntegrationTests.Support;
using static MaintenanceRequests.IntegrationTests.Support.ApiClient;

namespace MaintenanceRequests.IntegrationTests;

/// <summary>
/// Proves that every change and its history entry are saved in one transaction:
/// if the history INSERT fails, the UPDATE of the request must be rolled back too.
/// </summary>
public class HistoryAtomicityTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private const string BlockHistoryInserts = """
        CREATE FUNCTION test_block_history_insert() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN
            RAISE EXCEPTION 'history insert blocked by integration test';
        END;
        $$;
        CREATE TRIGGER test_block_history_insert
            BEFORE INSERT ON request_history
            FOR EACH ROW EXECUTE FUNCTION test_block_history_insert();
        """;

    private const string UnblockHistoryInserts = """
        DROP TRIGGER IF EXISTS test_block_history_insert ON request_history;
        DROP FUNCTION IF EXISTS test_block_history_insert();
        """;

    private readonly HttpClient _client = fixture.Factory.CreateClient();

    [Fact]
    public async Task ChangeStatus_WhenHistoryInsertFails_RollsBackStatusAndHidesInternals()
    {
        var created = await ReadAsync<MaintenanceRequestDetailDto>(await CreateAsync(_client));

        var (response, body) = await WithHistoryInsertsBlockedAsync(
            () => ChangeStatusAsync(_client, created.Id, RequestStatus.InProgress, created.Version));

        AssertGenericServerError(response, body);

        // The UPDATE of the status was rolled back together with the failed INSERT.
        var detail = await GetDetailAsync(_client, created.Id);
        Assert.Equal(RequestStatus.Pending, detail.Status);
        Assert.Equal(created.Version, detail.Version);
        Assert.Single(detail.History);
    }

    [Fact]
    public async Task Assign_WhenHistoryInsertFails_RollsBackAssigneeAndHidesInternals()
    {
        var created = await ReadAsync<MaintenanceRequestDetailDto>(await CreateAsync(_client));

        var (response, body) = await WithHistoryInsertsBlockedAsync(
            () => AssignAsync(_client, created.Id, TechnicianId, created.Version));

        AssertGenericServerError(response, body);

        // The UPDATE of the assignee was rolled back together with the failed INSERT.
        var detail = await GetDetailAsync(_client, created.Id);
        Assert.Null(detail.Assignee);
        Assert.Equal(created.Version, detail.Version);
        Assert.Single(detail.History);
    }

    /// <summary>
    /// Runs <paramref name="send"/> while a trigger makes every history INSERT fail.
    /// The trigger is always removed, even if the request throws.
    /// </summary>
    private async Task<(HttpResponseMessage Response, string Body)> WithHistoryInsertsBlockedAsync(
        Func<Task<HttpResponseMessage>> send)
    {
        await fixture.ExecuteSqlAsync(BlockHistoryInserts);
        try
        {
            var response = await send();
            return (response, await response.Content.ReadAsStringAsync());
        }
        finally
        {
            await fixture.ExecuteSqlAsync(UnblockHistoryInserts);
        }
    }

    // The client gets a generic 500: no provider names, no database message, no stack trace.
    private static void AssertGenericServerError(HttpResponseMessage response, string body)
    {
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("traceId", body);
        Assert.DoesNotContain("Npgsql", body);
        Assert.DoesNotContain("blocked by integration test", body);
        Assert.DoesNotContain("   at ", body);
    }
}
