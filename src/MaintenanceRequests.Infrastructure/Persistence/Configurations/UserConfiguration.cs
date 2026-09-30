using MaintenanceRequests.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaintenanceRequests.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public const int NameMaxLength = 100;

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();
        builder.Property(u => u.Name).HasMaxLength(NameMaxLength).IsRequired();

        // Fixed catalog with explicit ids, so X-User-Id values are stable across environments.
        builder.HasData(
            new User(1, "Ana Gómez"),
            new User(2, "Carlos Ruiz"),
            new User(3, "Laura Martínez"),
            new User(4, "Andrés López"),
            new User(5, "Sofía Herrera"));
    }
}
