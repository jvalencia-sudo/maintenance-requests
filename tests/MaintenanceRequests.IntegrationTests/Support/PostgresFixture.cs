using Testcontainers.PostgreSql;

namespace MaintenanceRequests.IntegrationTests.Support;

/// <summary>
/// One real PostgreSQL container per test class, plus the API wired to it.
/// A real database is required: InMemory ignores transactions, CHECKs, FKs and ILIKE.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public MaintenanceRequestsApiFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Factory = new MaintenanceRequestsApiFactory(_container.GetConnectionString());
    }

    /// <summary>Runs SQL directly in the container, bypassing the API (used to inject failures).</summary>
    public async Task ExecuteSqlAsync(string sql)
    {
        var result = await _container.ExecScriptAsync(sql);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"SQL script failed: {result.Stderr}");
        }
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }
}
