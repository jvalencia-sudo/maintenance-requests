using MaintenanceRequests.Domain.Requests;
using MaintenanceRequests.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaintenanceRequests.Infrastructure.Persistence.Configurations;

internal sealed class MaintenanceRequestConfiguration : IEntityTypeConfiguration<MaintenanceRequest>
{
    public const int EnumMaxLength = 20;

    public void Configure(EntityTypeBuilder<MaintenanceRequest> builder)
    {
        builder.ToTable("maintenance_requests", table =>
        {
            table.HasCheckConstraint(
                "ck_maintenance_requests_title_length",
                $"char_length(title) BETWEEN {MaintenanceRequest.TitleMinLength} AND {MaintenanceRequest.TitleMaxLength}");
            table.HasCheckConstraint(
                "ck_maintenance_requests_description_length",
                $"char_length(description) BETWEEN {MaintenanceRequest.DescriptionMinLength} AND {MaintenanceRequest.DescriptionMaxLength}");
            table.HasCheckConstraint("ck_maintenance_requests_category", CheckConstraintSql.In<RequestCategory>("category"));
            table.HasCheckConstraint("ck_maintenance_requests_priority", CheckConstraintSql.In<RequestPriority>("priority"));
            table.HasCheckConstraint("ck_maintenance_requests_status", CheckConstraintSql.In<RequestStatus>("status"));
        });

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).UseIdentityByDefaultColumn();

        builder.Property(r => r.Title).HasMaxLength(MaintenanceRequest.TitleMaxLength).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(MaintenanceRequest.DescriptionMaxLength).IsRequired();
        builder.Property(r => r.Category).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(r => r.Priority).HasConversion<string>().HasMaxLength(EnumMaxLength);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(EnumMaxLength);

        // uint + IsRowVersion maps to PostgreSQL's xmin system column: no extra column,
        // and every UPDATE checks it in its WHERE clause (optimistic concurrency).
        builder.Property(r => r.Version).IsRowVersion();

        builder.HasOne<User>().WithMany().HasForeignKey(r => r.RequesterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.AssigneeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.History)
            .WithOne()
            .HasForeignKey(RequestHistoryEntryConfiguration.RequestIdProperty)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(r => r.History).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(r => r.AllowedTransitions);
        builder.Ignore(r => r.CanBeAssigned);

        // Default list: ORDER BY created_at, id. A B-tree can be scanned backwards,
        // so one index serves both sort directions.
        builder.HasIndex(r => new { r.CreatedAt, r.Id });

        // List filtered by status, keeping the same order.
        builder.HasIndex(r => new { r.Status, r.CreatedAt, r.Id });

        // Search is title ILIKE '%text%'; a leading wildcard cannot use a B-tree, a trigram GIN can.
        builder.HasIndex(r => r.Title).HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}
