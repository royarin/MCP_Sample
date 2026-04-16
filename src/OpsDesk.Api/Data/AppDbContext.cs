using Microsoft.EntityFrameworkCore;
using OpsDesk.Api.Models;

namespace OpsDesk.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<SystemStatus> SystemStatuses => Set<SystemStatus>();
    public DbSet<SystemOwner> SystemOwners => Set<SystemOwner>();
    public DbSet<SystemRunbook> SystemRunbooks => Set<SystemRunbook>();
    public DbSet<Incident> Incidents => Set<Incident>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SystemStatus>(e =>
        {
            e.HasKey(x => x.SystemId);
            e.Property(x => x.SystemId).HasMaxLength(100);
            e.Property(x => x.Status)
                .HasMaxLength(20)
                .IsRequired()
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => Enum.Parse<SystemHealthStatus>(v, ignoreCase: true));
            e.Property(x => x.Message).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<SystemOwner>(e =>
        {
            e.HasKey(x => x.SystemId);
            e.Property(x => x.SystemId).HasMaxLength(100);
            e.Property(x => x.TeamName).HasMaxLength(200).IsRequired();
            e.Property(x => x.ContactEmail).HasMaxLength(200).IsRequired();
            e.Property(x => x.SlackChannel).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<SystemRunbook>(e =>
        {
            e.HasKey(x => x.SystemId);
            e.Property(x => x.SystemId).HasMaxLength(100);
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Url).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<Incident>(e =>
        {
            e.HasKey(x => x.IncidentNumber);
            e.Property(x => x.IncidentNumber).HasMaxLength(20);
            e.Property(x => x.SystemId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Severity)
                .HasMaxLength(20)
                .IsRequired()
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => Enum.Parse<IncidentSeverity>(v, ignoreCase: true));
            e.Property(x => x.ReportedBy).HasMaxLength(200).IsRequired();
            e.Property(x => x.CurrentStatus)
                .HasMaxLength(20)
                .IsRequired()
                .HasConversion(
                    v => v.ToString().ToLower(),
                    v => Enum.Parse<SystemHealthStatus>(v, ignoreCase: true));
            e.Property(x => x.StatusMessage).HasMaxLength(500).IsRequired();
        });

        // Seed data
        var now = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        modelBuilder.Entity<SystemStatus>().HasData(
            new SystemStatus { SystemId = "payments",      Status = SystemHealthStatus.Green,  Message = "All payment processors responding normally",          UpdatedAt = now },
            new SystemStatus { SystemId = "auth",          Status = SystemHealthStatus.Yellow, Message = "Elevated latency on token refresh endpoint",          UpdatedAt = now },
            new SystemStatus { SystemId = "notifications", Status = SystemHealthStatus.Green,  Message = "Email and push delivery operating normally",          UpdatedAt = now },
            new SystemStatus { SystemId = "inventory",     Status = SystemHealthStatus.Red,    Message = "Database replication lag exceeding threshold",        UpdatedAt = now },
            new SystemStatus { SystemId = "reporting",     Status = SystemHealthStatus.Green,  Message = "Batch jobs running on schedule",                     UpdatedAt = now }
        );

        modelBuilder.Entity<SystemOwner>().HasData(
            new SystemOwner { SystemId = "payments",      TeamName = "Payments Platform",    ContactEmail = "payments-team@contoso.com",  SlackChannel = "#payments-oncall" },
            new SystemOwner { SystemId = "auth",          TeamName = "Identity & Access",    ContactEmail = "iam-team@contoso.com",        SlackChannel = "#iam-oncall" },
            new SystemOwner { SystemId = "notifications", TeamName = "Comms Engineering",    ContactEmail = "comms-eng@contoso.com",       SlackChannel = "#comms-oncall" },
            new SystemOwner { SystemId = "inventory",     TeamName = "Supply Chain Systems", ContactEmail = "supply-chain@contoso.com",    SlackChannel = "#inventory-oncall" },
            new SystemOwner { SystemId = "reporting",     TeamName = "Data & Analytics",     ContactEmail = "data-platform@contoso.com",   SlackChannel = "#analytics-oncall" }
        );

        modelBuilder.Entity<SystemRunbook>().HasData(
            new SystemRunbook { SystemId = "payments",      Title = "Payments Service Incident Runbook",  Url = "https://wiki.contoso.com/runbooks/payments",     LastReviewed = new DateOnly(2025, 3, 10) },
            new SystemRunbook { SystemId = "auth",          Title = "Identity Service Runbook",           Url = "https://wiki.contoso.com/runbooks/auth",          LastReviewed = new DateOnly(2025, 1, 22) },
            new SystemRunbook { SystemId = "notifications", Title = "Notifications Pipeline Runbook",     Url = "https://wiki.contoso.com/runbooks/notifications", LastReviewed = new DateOnly(2025, 2, 14) },
            new SystemRunbook { SystemId = "inventory",     Title = "Inventory DB Failover Runbook",      Url = "https://wiki.contoso.com/runbooks/inventory",     LastReviewed = new DateOnly(2025, 4, 1) },
            new SystemRunbook { SystemId = "reporting",     Title = "Reporting & Analytics Runbook",      Url = "https://wiki.contoso.com/runbooks/reporting",     LastReviewed = new DateOnly(2024, 12, 5) }
        );
    }
}
