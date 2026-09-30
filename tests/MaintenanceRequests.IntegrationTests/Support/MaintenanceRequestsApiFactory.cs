using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MaintenanceRequests.IntegrationTests.Support;

/// <summary>
/// Runs the real API in memory against the test container. Migrations are applied on startup,
/// so every run also proves the committed migration works on an empty database.
/// </summary>
public sealed class MaintenanceRequestsApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not "Development": that would load the developer's user secrets and their connection string.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("APPLY_MIGRATIONS", "true");
        builder.UseSetting("SEED_DEMO_DATA", "false");
        builder.UseSetting("ENABLE_SWAGGER", "false");
    }
}
