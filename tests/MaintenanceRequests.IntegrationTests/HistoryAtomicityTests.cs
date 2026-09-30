using System.Net;
using MaintenanceRequests.Application.Requests;
using MaintenanceRequests.Domain.Requests;
using MaintenanceRequests.IntegrationTests.Support;
using static MaintenanceRequests.IntegrationTests.Support.ApiClient;

namespace MaintenanceRequests.IntegrationTests;

/// <summary>
/// Proves that a status change and its history entry are saved in one transaction:
/// if the history INSERT fails, the status UPDATE must be rolled back too.
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

        await fixture.ExecuteSqlAsync(BlockHistoryInserts);
        HttpResponseMessage response;
        string body;
        try
        {
            response = await ChangeStatusAsync(_client, created.Id, RequestStatus.InProgress, created.Version);
            body = await response.Content.ReadAsStringAsync();
        }
        finally
        {
            await fixture.ExecuteSqlAsync(UnblockHistoryInserts);
        }

        // The client gets a generic 500: no provider names, no database message, no stack trace.
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("traceId", body);
        Assert.DoesNotContain("Npgsql", body);
        Assert.DoesNotContain("blocked by integration test", body);
        Assert.DoesNotContain("   at ", body);

        // The UPDATE of the status was rolled back together with the failed INSERT.
        var detail = await GetDetailAsync(_client, created.Id);
        Assert.Equal(RequestStatus.Pending, detail.Status);
        Assert.Equal(created.Version, detail.Version);
        Assert.Single(detail.History);
    }
}
