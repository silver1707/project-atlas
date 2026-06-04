using System.Text.Json;
using Atlas.Api.Administration;
using Atlas.Api.Auditing;
using Atlas.Api.Catalog;
using Atlas.Api.Common;
using Atlas.Api.Crm;
using Atlas.Api.Finance;
using Atlas.Api.Fiscal;
using Atlas.Api.IdentityAccess;
using Atlas.Api.Integrations;
using Atlas.Api.Inventory;
using Atlas.Api.Purchasing;
using Atlas.Api.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Atlas.Api.Infrastructure;

public sealed class AtlasDbContext(DbContextOptions<AtlasDbContext> options, ITenantContext tenantContext)
    : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<ModulePermission> ModulePermissions => Set<ModulePermission>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    public DbSet<AutoPartProduct> Products => Set<AutoPartProduct>();
    public DbSet<ProductApplication> ProductApplications => Set<ProductApplication>();
    public DbSet<ProductEquivalent> ProductEquivalents => Set<ProductEquivalent>();
    public DbSet<ProductSupplierOffer> ProductSupplierOffers => Set<ProductSupplierOffer>();
    public DbSet<ProductPriceHistory> ProductPriceHistory => Set<ProductPriceHistory>();
    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

    public DbSet<StockLocation> StockLocations => Set<StockLocation>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    public DbSet<PickList> PickLists => Set<PickList>();

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<SalesQuote> SalesQuotes => Set<SalesQuote>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<CashSession> CashSessions => Set<CashSession>();

    public DbSet<FiscalRule> FiscalRules => Set<FiscalRule>();
    public DbSet<FiscalDocument> FiscalDocuments => Set<FiscalDocument>();
    public DbSet<FiscalDocumentItem> FiscalDocumentItems => Set<FiscalDocumentItem>();
    public DbSet<FiscalDocumentEvent> FiscalDocumentEvents => Set<FiscalDocumentEvent>();
    public DbSet<DfeDistributionCursor> DfeDistributionCursors => Set<DfeDistributionCursor>();

    public DbSet<AccountPayable> AccountsPayable => Set<AccountPayable>();
    public DbSet<AccountReceivable> AccountsReceivable => Set<AccountReceivable>();
    public DbSet<CashLedgerEntry> CashLedgerEntries => Set<CashLedgerEntry>();

    public DbSet<IntegrationAdapter> IntegrationAdapters => Set<IntegrationAdapter>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.HasPostgresExtension("unaccent");

        modelBuilder.ConfigureAdministration();
        modelBuilder.ConfigureIdentityAccess();
        modelBuilder.ConfigureAuditing();
        modelBuilder.ConfigureCatalog();
        modelBuilder.ConfigureCrm();
        modelBuilder.ConfigurePurchasing();
        modelBuilder.ConfigureInventory();
        modelBuilder.ConfigureSales();
        modelBuilder.ConfigureFiscal();
        modelBuilder.ConfigureFinance();
        modelBuilder.ConfigureIntegrations();

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var auditRecords = new List<AuditRecord>();
        var outboxMessages = new List<OutboxMessage>();

        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (entry.Entity is AuditRecord or OutboxMessage)
            {
                continue;
            }

            if (entry.Entity is TenantEntity tenantEntity)
            {
                if (tenantEntity.CompanyId == Guid.Empty)
                {
                    tenantEntity.CompanyId = tenantContext.CompanyId;
                }

                if (tenantEntity.BranchId == Guid.Empty)
                {
                    tenantEntity.BranchId = tenantContext.BranchId;
                }

                if (entry.State == EntityState.Added)
                {
                    tenantEntity.CreatedAt = now;
                    tenantEntity.CreatedBy = tenantContext.UserId;
                }
                else
                {
                    tenantEntity.UpdatedAt = now;
                    tenantEntity.UpdatedBy = tenantContext.UserId;
                }
            }

            auditRecords.Add(AuditRecord.FromEntry(entry, tenantContext, now));
        }

        foreach (var entity in ChangeTracker.Entries<Entity>().Select(e => e.Entity))
        {
            foreach (var domainEvent in entity.DequeueDomainEvents())
            {
                var companyId = entity is TenantEntity tenant ? tenant.CompanyId : tenantContext.CompanyId;
                var branchId = entity is TenantEntity tenantScoped ? tenantScoped.BranchId : tenantContext.BranchId;
                outboxMessages.Add(OutboxMessage.FromDomainEvent(domainEvent, companyId, branchId));
            }
        }

        if (auditRecords.Count > 0)
        {
            AuditRecords.AddRange(auditRecords);
        }

        if (outboxMessages.Count > 0)
        {
            OutboxMessages.AddRange(outboxMessages);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

public static class EntityMapping
{
    public static void ConfigureTenantEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : TenantEntity
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.BranchId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(160).IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(160);
        builder.Property(x => x.RowVersion).IsConcurrencyToken();
        builder.HasIndex(x => new { x.CompanyId, x.BranchId });
    }
}

public static class AuditValueReader
{
    public static Dictionary<string, object?> ReadValues(PropertyValues values)
        => values.Properties.ToDictionary(property => property.Name, property => values[property]);

    public static string Serialize(Dictionary<string, object?>? value)
        => value is null ? "{}" : JsonSerializer.Serialize(value, JsonPayload.Options);
}
