using MaintenanceRequests.Domain.Requests;
using MaintenanceRequests.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaintenanceRequests.Infrastructure.Persistence.Configurations;

internal sealed class RequestHistoryEntryConfiguration : IEntityTypeConfiguration<RequestHistoryEntry>
{
    /// <summary>Shadow foreign key: the domain entry does not need to know its parent id.</summary>
    public const string RequestIdProperty = "RequestId";

    public void Configure(EntityTypeBuilder<RequestHistoryEntry> builder)
    {
        builder.ToTable("request_history", table =>
        {
            table.HasCheckConstraint("ck_request_history_type", CheckConstraintSql.In<HistoryEventType>("type"));
            table.HasCheckConstraint("ck_request_history_from_status", CheckConstraintSql.In<RequestStatus>("from_status"));
            table.HasCheckConstraint("ck_request_history_to_status", CheckConstraintSql.In<RequestStatus>("to_status"));

            // Each event type must carry the data that describes it.
            table.HasCheckConstraint(
                "ck_request_history_consistency",
                "(type = 'Created' AND to_status IS NOT NULL) OR " +
                "(type = 'StatusChanged' AND from_status IS NOT NULL AND to_status IS NOT NULL) OR " +
                "(type = 'AssigneeChanged' AND new_assignee_id IS NOT NULL)");
        });

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).UseIdentityByDefaultColumn();
        builder.Property<int>(RequestIdProperty);

        builder.Property(h => h.EventType)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(MaintenanceRequestConfiguration.EnumMaxLength);
        builder.Property(h => h.FromStatus).HasConversion<string>().HasMaxLength(MaintenanceRequestConfiguration.EnumMaxLength);
        builder.Property(h => h.ToStatus).HasConversion<string>().HasMaxLength(MaintenanceRequestConfiguration.EnumMaxLength);

        builder.HasOne<User>().WithMany().HasForeignKey(h => h.ActorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(h => h.PreviousAssigneeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(h => h.NewAssigneeId).OnDelete(DeleteBehavior.Restrict);

        // Detail view: history of one request in order. Its leading column also covers the FK.
        builder.HasIndex(RequestIdProperty, nameof(RequestHistoryEntry.OccurredAt), nameof(RequestHistoryEntry.Id));
    }
}
