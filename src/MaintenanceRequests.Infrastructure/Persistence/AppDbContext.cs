using MaintenanceRequests.Domain.Requests;
using MaintenanceRequests.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace MaintenanceRequests.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();

    public DbSet<RequestHistoryEntry> RequestHistory => Set<RequestHistoryEntry>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // By default EF adds an index on every foreign key. Here each index is declared
        // explicitly in the entity configurations and justified by the query that uses it.
        configurationBuilder.Conventions.Remove(typeof(ForeignKeyIndexConvention));
    }
}
