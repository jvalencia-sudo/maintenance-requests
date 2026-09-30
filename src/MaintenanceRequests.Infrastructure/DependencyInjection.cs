using MaintenanceRequests.Application.Abstractions;
using MaintenanceRequests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceRequests.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IMaintenanceRequestRepository, MaintenanceRequestRepository>();
        services.AddScoped<IMaintenanceRequestQueries, MaintenanceRequestQueries>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<DemoDataSeeder>();

        return services;
    }

    /// <summary>Applies pending migrations. Never EnsureCreated: it bypasses migrations history.</summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    /// <summary>Seeds demo requests when the table is empty; does nothing otherwise.</summary>
    public static async Task SeedDemoDataAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
        await seeder.SeedAsync(CancellationToken.None);
    }
}
