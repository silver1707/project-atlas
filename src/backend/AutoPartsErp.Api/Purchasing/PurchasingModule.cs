using Atlas.Api.Catalog;
using Atlas.Api.Common;
using Atlas.Api.Infrastructure;
using Atlas.Api.Inventory;
using Atlas.Api.Sales;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api.Purchasing;

public sealed class PurchasingModule : IEndpointModule
{
    public string Name => "Purchasing";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/purchasing")
            .WithTags(Name)
            .RequireAuthorization();

        group.MapPost("/suppliers", async Task<Created<SupplierSummary>> (CreateSupplierRequest request, AtlasDbContext db, ITenantContext tenant, CancellationToken ct) =>
        {
            var supplier = new Supplier
            {
                CompanyId = tenant.CompanyId,
                BranchId = tenant.BranchId,
                LegalName = request.LegalName.Trim(),
                TradeName = request.TradeName.Trim(),
                Document = request.Document.Trim(),
                StateRegistration = request.StateRegistration?.Trim(),
                Email = request.Email?.Trim(),
                Phone = request.Phone?.Trim()
            };

            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/purchasing/suppliers/{supplier.Id}", SupplierSummary.From(supplier));
        });

        group.MapPost("/orders", async Task<Created<PurchaseOrderSummary>> (CreatePurchaseOrderRequest request, AtlasDbContext db, ITenantContext tenant, CancellationToken ct) =>
        {
            var supplier = await db.Suppliers.FirstAsync(x => x.CompanyId == tenant.CompanyId && x.Id == request.SupplierId, ct);
            var order = PurchaseOrder.Create(request, supplier, tenant.CompanyId, tenant.BranchId);
            db.PurchaseOrders.Add(order);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/purchasing/orders/{order.Id}", PurchaseOrderSummary.From(order));
        });

        group.MapGet("/orders/{id:guid}", async Task<Results<Ok<PurchaseOrderSummary>, NotFound>> (Guid id, AtlasDbContext db, ITenantContext tenant, CancellationToken ct) =>
        {
            var order = await db.PurchaseOrders
                .AsNoTracking()
                .Include(x => x.Lines)
                .Where(x => x.CompanyId == tenant.CompanyId && x.Id == id)
                .Select(x => PurchaseOrderSummary.From(x))
                .FirstOrDefaultAsync(ct);
            return order is null ? TypedResults.NotFound() : TypedResults.Ok(order);
        });

        group.MapPost("/orders/{id:guid}/approve", async Task<Results<Ok<PurchaseOrderSummary>, NotFound>> (Guid id, AtlasDbContext db, ITenantContext tenant, CancellationToken ct) =>
        {
            var order = await db.PurchaseOrders.Include(x => x.Lines).FirstOrDefaultAsync(x => x.CompanyId == tenant.CompanyId && x.Id == id, ct);
            if (order is null)
            {
                return TypedResults.NotFound();
            }

            order.Approve();
            await db.SaveChangesAsync(ct);
            return TypedResults.Ok(PurchaseOrderSummary.From(order));
        });

        group.MapGet("/suggestions", async Task<Ok<IReadOnlyCollection<PurchaseSuggestion>>> (
            PurchaseSuggestionService service,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var suggestions = await service.CalculateAsync(tenant.CompanyId, tenant.BranchId, ct);
            return TypedResults.Ok(suggestions);
        });
    }
}

public sealed class PurchaseSuggestionService(AtlasDbContext db)
{
    public async Task<IReadOnlyCollection<PurchaseSuggestion>> CalculateAsync(Guid companyId, Guid branchId, CancellationToken ct)
    {
        var since = DateTimeOffset.UtcNow.AddDays(-90);
        var salesByProduct = await db.SalesOrderLines
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.BranchId == branchId && x.CreatedAt >= since)
            .GroupBy(x => x.ProductId)
            .Select(x => new { ProductId = x.Key, Quantity = x.Sum(line => line.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity, ct);

        var balances = await db.StockBalances
            .AsNoTracking()
            .Include(x => x.Product)
            .Where(x => x.CompanyId == companyId && x.BranchId == branchId)
            .ToListAsync(ct);

        return balances
            .Select(balance =>
            {
                salesByProduct.TryGetValue(balance.ProductId, out var sold);
                var dailyAverage = sold / 90m;
                var target = Math.Max(balance.MinimumQuantity, dailyAverage * 30m);
                var pending = Math.Max(0, target - balance.AvailableQuantity);
                return new PurchaseSuggestion(
                    balance.ProductId,
                    balance.Product?.Sku ?? "",
                    balance.Product?.Description ?? "",
                    balance.AvailableQuantity,
                    balance.MinimumQuantity,
                    Math.Round(dailyAverage, 2),
                    Math.Ceiling(pending));
            })
            .Where(x => x.SuggestedQuantity > 0)
            .OrderByDescending(x => x.SuggestedQuantity)
            .Take(200)
            .ToArray();
    }
}

public sealed class Supplier : TenantEntity, IAggregateRoot
{
    public string LegalName { get; set; } = "";
    public string TradeName { get; set; } = "";
    public string Document { get; set; } = "";
    public string? StateRegistration { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public sealed class PurchaseOrder : TenantEntity, IAggregateRoot
{
    public Guid SupplierId { get; set; }
    public string SupplierNameSnapshot { get; set; } = "";
    public string Status { get; set; } = PurchaseOrderStatus.Draft;
    public DateOnly ExpectedOn { get; set; }
    public string ExternalReference { get; set; } = "";
    public decimal Total { get; set; }
    public List<PurchaseOrderLine> Lines { get; set; } = [];

    public static PurchaseOrder Create(CreatePurchaseOrderRequest request, Supplier supplier, Guid companyId, Guid branchId)
    {
        var order = new PurchaseOrder
        {
            CompanyId = companyId,
            BranchId = branchId,
            SupplierId = supplier.Id,
            SupplierNameSnapshot = supplier.TradeName,
            ExpectedOn = request.ExpectedOn,
            ExternalReference = request.ExternalReference?.Trim() ?? ""
        };

        foreach (var line in request.Lines)
        {
            order.Lines.Add(new PurchaseOrderLine
            {
                CompanyId = companyId,
                BranchId = branchId,
                ProductId = line.ProductId,
                SupplierCode = line.SupplierCode?.Trim() ?? "",
                Quantity = line.Quantity,
                UnitCost = line.UnitCost,
                NcmSnapshot = line.NcmSnapshot.Trim(),
                CfopSnapshot = line.CfopSnapshot.Trim()
            });
        }

        order.Total = order.Lines.Sum(x => x.Quantity * x.UnitCost);
        order.AddDomainEvent(new PurchaseOrderCreated(order.Id, companyId, branchId, order.Total, DateTimeOffset.UtcNow));
        return order;
    }

    public void Approve()
    {
        if (Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidOperationException("Only draft purchase orders can be approved.");
        }

        Status = PurchaseOrderStatus.Approved;
        AddDomainEvent(new PurchaseOrderApproved(Id, CompanyId, BranchId, DateTimeOffset.UtcNow));
    }
}

public sealed class PurchaseOrderLine : TenantEntity
{
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Guid ProductId { get; set; }
    public string SupplierCode { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string NcmSnapshot { get; set; } = "";
    public string CfopSnapshot { get; set; } = "";
}

public static class PurchaseOrderStatus
{
    public const string Draft = "draft";
    public const string Approved = "approved";
    public const string PartiallyReceived = "partially_received";
    public const string Received = "received";
    public const string Cancelled = "cancelled";
}

public sealed record PurchaseOrderCreated(Guid PurchaseOrderId, Guid CompanyId, Guid BranchId, decimal Total, DateTimeOffset OccurredAt) : IDomainEvent;
public sealed record PurchaseOrderApproved(Guid PurchaseOrderId, Guid CompanyId, Guid BranchId, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record CreateSupplierRequest(string LegalName, string TradeName, string Document, string? StateRegistration, string? Email, string? Phone);
public sealed record SupplierSummary(Guid Id, string LegalName, string TradeName, string Document)
{
    public static SupplierSummary From(Supplier supplier) => new(supplier.Id, supplier.LegalName, supplier.TradeName, supplier.Document);
}

public sealed record CreatePurchaseOrderRequest(Guid SupplierId, DateOnly ExpectedOn, string? ExternalReference, IReadOnlyCollection<CreatePurchaseOrderLineRequest> Lines);
public sealed record CreatePurchaseOrderLineRequest(Guid ProductId, string? SupplierCode, decimal Quantity, decimal UnitCost, string NcmSnapshot, string CfopSnapshot);
public sealed record PurchaseOrderSummary(Guid Id, Guid SupplierId, string Supplier, string Status, DateOnly ExpectedOn, decimal Total, IReadOnlyCollection<PurchaseOrderLineSummary> Lines)
{
    public static PurchaseOrderSummary From(PurchaseOrder order)
        => new(order.Id, order.SupplierId, order.SupplierNameSnapshot, order.Status, order.ExpectedOn, order.Total, order.Lines.Select(PurchaseOrderLineSummary.From).ToArray());
}

public sealed record PurchaseOrderLineSummary(Guid ProductId, decimal Quantity, decimal UnitCost, string Ncm, string Cfop)
{
    public static PurchaseOrderLineSummary From(PurchaseOrderLine line) => new(line.ProductId, line.Quantity, line.UnitCost, line.NcmSnapshot, line.CfopSnapshot);
}

public sealed record PurchaseSuggestion(Guid ProductId, string Sku, string Description, decimal AvailableQuantity, decimal MinimumQuantity, decimal DailyAverageSales, decimal SuggestedQuantity);

public static class PurchasingMapping
{
    public static void ConfigurePurchasing(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Supplier>(builder =>
        {
            builder.ToTable("suppliers", "purchasing");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.LegalName).HasMaxLength(220).IsRequired();
            builder.Property(x => x.TradeName).HasMaxLength(180).IsRequired();
            builder.Property(x => x.Document).HasMaxLength(32).IsRequired();
            builder.Property(x => x.StateRegistration).HasMaxLength(32);
            builder.Property(x => x.Email).HasMaxLength(180);
            builder.Property(x => x.Phone).HasMaxLength(40);
            builder.HasIndex(x => new { x.CompanyId, x.Document }).IsUnique();
        });

        modelBuilder.Entity<PurchaseOrder>(builder =>
        {
            builder.ToTable("purchase_orders", "purchasing");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.SupplierNameSnapshot).HasMaxLength(180).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
            builder.Property(x => x.ExternalReference).HasMaxLength(120).IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.Status, x.ExpectedOn });
        });

        modelBuilder.Entity<PurchaseOrderLine>(builder =>
        {
            builder.ToTable("purchase_order_lines", "purchasing");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.SupplierCode).HasMaxLength(120).IsRequired();
            builder.Property(x => x.NcmSnapshot).HasMaxLength(16).IsRequired();
            builder.Property(x => x.CfopSnapshot).HasMaxLength(8).IsRequired();
            builder.HasOne(x => x.PurchaseOrder).WithMany(x => x.Lines).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.CompanyId, x.ProductId });
        });
    }
}
