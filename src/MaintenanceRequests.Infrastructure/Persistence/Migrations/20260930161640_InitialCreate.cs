using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MaintenanceRequests.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requester_id = table.Column<int>(type: "integer", nullable: false),
                    assignee_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_maintenance_requests", x => x.id);
                    table.CheckConstraint("ck_maintenance_requests_category", "category IN ('Infrastructure', 'Equipment', 'Software', 'Other')");
                    table.CheckConstraint("ck_maintenance_requests_description_length", "char_length(description) BETWEEN 10 AND 2000");
                    table.CheckConstraint("ck_maintenance_requests_priority", "priority IN ('Low', 'Medium', 'High', 'Critical')");
                    table.CheckConstraint("ck_maintenance_requests_status", "status IN ('Pending', 'InProgress', 'OnHold', 'Resolved', 'Cancelled')");
                    table.CheckConstraint("ck_maintenance_requests_title_length", "char_length(title) BETWEEN 5 AND 120");
                    table.ForeignKey(
                        name: "fk_maintenance_requests_users_assignee_id",
                        column: x => x.assignee_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_maintenance_requests_users_requester_id",
                        column: x => x.requester_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "request_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    from_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    to_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    previous_assignee_id = table.Column<int>(type: "integer", nullable: true),
                    new_assignee_id = table.Column<int>(type: "integer", nullable: true),
                    actor_id = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    request_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_request_history", x => x.id);
                    table.CheckConstraint("ck_request_history_consistency", "(type = 'Created' AND to_status IS NOT NULL) OR (type = 'StatusChanged' AND from_status IS NOT NULL AND to_status IS NOT NULL) OR (type = 'AssigneeChanged' AND new_assignee_id IS NOT NULL)");
                    table.CheckConstraint("ck_request_history_from_status", "from_status IN ('Pending', 'InProgress', 'OnHold', 'Resolved', 'Cancelled')");
                    table.CheckConstraint("ck_request_history_to_status", "to_status IN ('Pending', 'InProgress', 'OnHold', 'Resolved', 'Cancelled')");
                    table.CheckConstraint("ck_request_history_type", "type IN ('Created', 'StatusChanged', 'AssigneeChanged')");
                    table.ForeignKey(
                        name: "fk_request_history_maintenance_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "maintenance_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_request_history_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_request_history_users_new_assignee_id",
                        column: x => x.new_assignee_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_request_history_users_previous_assignee_id",
                        column: x => x.previous_assignee_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1, "Ana Gómez" },
                    { 2, "Carlos Ruiz" },
                    { 3, "Laura Martínez" },
                    { 4, "Andrés López" },
                    { 5, "Sofía Herrera" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_requests_created_at_id",
                table: "maintenance_requests",
                columns: new[] { "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_requests_status_created_at_id",
                table: "maintenance_requests",
                columns: new[] { "status", "created_at", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_requests_title",
                table: "maintenance_requests",
                column: "title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_request_history_request_id_occurred_at_id",
                table: "request_history",
                columns: new[] { "request_id", "occurred_at", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "request_history");

            migrationBuilder.DropTable(
                name: "maintenance_requests");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
