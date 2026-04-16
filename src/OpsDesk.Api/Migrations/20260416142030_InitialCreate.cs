using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OpsDesk.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    IncidentNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SystemId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ReportedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CurrentStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StatusMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.IncidentNumber);
                });

            migrationBuilder.CreateTable(
                name: "SystemOwners",
                columns: table => new
                {
                    SystemId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TeamName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SlackChannel = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemOwners", x => x.SystemId);
                });

            migrationBuilder.CreateTable(
                name: "SystemRunbooks",
                columns: table => new
                {
                    SystemId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    LastReviewed = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemRunbooks", x => x.SystemId);
                });

            migrationBuilder.CreateTable(
                name: "SystemStatuses",
                columns: table => new
                {
                    SystemId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemStatuses", x => x.SystemId);
                });

            migrationBuilder.InsertData(
                table: "SystemOwners",
                columns: new[] { "SystemId", "ContactEmail", "SlackChannel", "TeamName" },
                values: new object[,]
                {
                    { "auth", "iam-team@contoso.com", "#iam-oncall", "Identity & Access" },
                    { "inventory", "supply-chain@contoso.com", "#inventory-oncall", "Supply Chain Systems" },
                    { "notifications", "comms-eng@contoso.com", "#comms-oncall", "Comms Engineering" },
                    { "payments", "payments-team@contoso.com", "#payments-oncall", "Payments Platform" },
                    { "reporting", "data-platform@contoso.com", "#analytics-oncall", "Data & Analytics" }
                });

            migrationBuilder.InsertData(
                table: "SystemRunbooks",
                columns: new[] { "SystemId", "LastReviewed", "Title", "Url" },
                values: new object[,]
                {
                    { "auth", new DateOnly(2025, 1, 22), "Identity Service Runbook", "https://wiki.contoso.com/runbooks/auth" },
                    { "inventory", new DateOnly(2025, 4, 1), "Inventory DB Failover Runbook", "https://wiki.contoso.com/runbooks/inventory" },
                    { "notifications", new DateOnly(2025, 2, 14), "Notifications Pipeline Runbook", "https://wiki.contoso.com/runbooks/notifications" },
                    { "payments", new DateOnly(2025, 3, 10), "Payments Service Incident Runbook", "https://wiki.contoso.com/runbooks/payments" },
                    { "reporting", new DateOnly(2024, 12, 5), "Reporting & Analytics Runbook", "https://wiki.contoso.com/runbooks/reporting" }
                });

            migrationBuilder.InsertData(
                table: "SystemStatuses",
                columns: new[] { "SystemId", "Message", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { "auth", "Elevated latency on token refresh endpoint", "yellow", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "inventory", "Database replication lag exceeding threshold", "red", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "notifications", "Email and push delivery operating normally", "green", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "payments", "All payment processors responding normally", "green", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "reporting", "Batch jobs running on schedule", "green", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "SystemOwners");

            migrationBuilder.DropTable(
                name: "SystemRunbooks");

            migrationBuilder.DropTable(
                name: "SystemStatuses");
        }
    }
}
