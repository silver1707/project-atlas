using Atlas.Api.Common;
using Atlas.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Atlas.Api.Auditing;

public sealed class AuditRecord : Entity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string EntityName { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string Operation { get; set; } = "";
    public string Before { get; set; } = "{}";
    public string After { get; set; } = "{}";
    public string UserId { get; set; } = "";
    public string? IpAddress { get; set; }
    public DateTimeOffset OccurredAt { get; set; }

    public static AuditRecord FromEntry(EntityEntry entry, ITenantContext tenantContext, DateTimeOffset now)
    {
        Dictionary<string, object?>? before = entry.State is EntityState.Modified or EntityState.Deleted
            ? AuditValueReader.ReadValues(entry.OriginalValues)
            : null;

        Dictionary<string, object?>? after = entry.State is EntityState.Added or EntityState.Modified
            ? AuditValueReader.ReadValues(entry.CurrentValues)
            : null;

        var entityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "";

        return new AuditRecord
        {
            CompanyId = entry.Entity is TenantEntity tenant ? tenant.CompanyId : tenantContext.CompanyId,
            BranchId = entry.Entity is TenantEntity branch ? branch.BranchId : tenantContext.BranchId,
            EntityName = entry.Metadata.ClrType.Name,
            EntityId = entityId,
            Operation = entry.State.ToString(),
            Before = AuditValueReader.Serialize(before),
            After = AuditValueReader.Serialize(after),
            UserId = tenantContext.UserId,
            IpAddress = tenantContext.IpAddress,
            OccurredAt = now
        };
    }
}

public static class AuditingMapping
{
    public static void ConfigureAuditing(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditRecord>(builder =>
        {
            builder.ToTable("audit_records", "audit");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.EntityName).HasMaxLength(180).IsRequired();
            builder.Property(x => x.EntityId).HasMaxLength(120).IsRequired();
            builder.Property(x => x.Operation).HasMaxLength(40).IsRequired();
            builder.Property(x => x.Before).HasColumnType("jsonb").IsRequired();
            builder.Property(x => x.After).HasColumnType("jsonb").IsRequired();
            builder.Property(x => x.UserId).HasMaxLength(160).IsRequired();
            builder.Property(x => x.IpAddress).HasMaxLength(80);
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.OccurredAt });
            builder.HasIndex(x => new { x.EntityName, x.EntityId });
        });
    }
}
