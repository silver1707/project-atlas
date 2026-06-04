using Atlas.Api.Common;
using Atlas.Api.Crm;
using Atlas.Api.Infrastructure;
using Atlas.Api.Inventory;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api.Sales;

public sealed class SalesModule : IEndpointModule
{
    public string Name => "Sales";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/sales")
            .WithTags(Name)
            .RequireAuthorization();

        group.MapPost("/quotes", async Task<Created<SalesQuoteSummary>> (
            CreateQuoteRequest request,
            SalesService sales,
            CancellationToken ct) =>
        {
            var quote = await sales.CreateQuoteAsync(request, ct);
            return TypedResults.Created($"/api/sales/quotes/{quote.Id}", SalesQuoteSummary.From(quote));
        });

        group.MapPost("/orders", async Task<Created<SalesOrderSummary>> (
            CreateSalesOrderRequest request,
            SalesService sales,
            CancellationToken ct) =>
        {
            var order = await sales.CreateOrderAsync(request, ct);
            return TypedResults.Created($"/api/sales/orders/{order.Id}", SalesOrderSummary.From(order));
        });

        group.MapPost("/counter-sales", async Task<Created<SalesOrderSummary>> (
            CounterSaleRequest request,
            SalesService sales,
            CancellationToken ct) =>
        {
            var order = await sales.CreateCounterSaleAsync(request, ct);
            return TypedResults.Created($"/api/sales/orders/{order.Id}", SalesOrderSummary.From(order));
        });

        group.MapPost("/orders/{id:guid}/approve", async Task<Results<Ok<SalesOrderSummary>, NotFound>> (
            Guid id,
            SalesService sales,
            CancellationToken ct) =>
        {
            var order = await sales.ApproveAsync(id, ct);
            return order is null ? TypedResults.NotFound() : TypedResults.Ok(SalesOrderSummary.From(order));
        });

        group.MapPost("/orders/{id:guid}/return", async Task<Results<Ok<SalesOrderSummary>, NotFound>> (
            Guid id,
            RegisterReturnRequest request,
            SalesService sales,
            CancellationToken ct) =>
        {
            var order = await sales.RegisterReturnAsync(id, request, ct);
            return order is null ? TypedResults.NotFound() : TypedResults.Ok(SalesOrderSummary.From(order));
        });

        group.MapPost("/cash/open", async Task<Created<CashSessionSummary>> (
            OpenCashSessionRequest request,
            SalesService sales,
            CancellationToken ct) =>
        {
            var session = await sales.OpenCashSessionAsync(request, ct);
            return TypedResults.Created($"/api/sales/cash/{session.Id}", CashSessionSummary.From(session));
        });

        group.MapPost("/cash/{id:guid}/close", async Task<Results<Ok<CashSessionSummary>, NotFound>> (
            Guid id,
            CloseCashSessionRequest request,
            SalesService sales,
            CancellationToken ct) =>
        {
            var session = await sales.CloseCashSessionAsync(id, request, ct);
            return session is null ? TypedResults.NotFound() : TypedResults.Ok(CashSessionSummary.From(session));
        });
    }
}

public sealed class SalesService(AtlasDbContext db, ITenantContext tenant, InventoryService inventory)
{
    public async Task<SalesQuote> CreateQuoteAsync(CreateQuoteRequest request, CancellationToken ct)
    {
        var quote = new SalesQuote
        {
            CompanyId = tenant.CompanyId,
            BranchId = tenant.BranchId,
            CustomerId = request.CustomerId,
            CustomerNameSnapshot = await CustomerName(request.CustomerId, ct),
            Status = SalesQuoteStatus.Open,
            ValidUntil = request.ValidUntil,
            LinesJson = JsonPayload.Serialize(request.Lines)
        };

        quote.Total = request.Lines.Sum(x => x.Quantity * x.UnitPrice);
        db.SalesQuotes.Add(quote);
        await db.SaveChangesAsync(ct);
        return quote;
    }

    public async Task<SalesOrder> CreateOrderAsync(CreateSalesOrderRequest request, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var order = new SalesOrder
        {
            CompanyId = tenant.CompanyId,
            BranchId = tenant.BranchId,
            QuoteId = request.QuoteId,
            CustomerId = request.CustomerId,
            CustomerNameSnapshot = await CustomerName(request.CustomerId, ct),
            Status = SalesOrderStatus.Pending,
            OperationNature = request.OperationNature,
            PriceTable = request.PriceTable,
            PaymentTerms = request.PaymentTerms,
            SalespersonId = request.SalespersonId,
            CommissionPercent = request.CommissionPercent
        };

        await AddLines(order, request.Lines, ct);
        order.Recalculate();
        order.AddCreatedEvent();
        db.SalesOrders.Add(order);
        await db.SaveChangesAsync(ct);

        foreach (var line in order.Lines)
        {
            var reservation = await inventory.ReserveAsync(new ReserveStockRequest(line.ProductId, line.LocationId, line.Quantity, order.Id, "sales_order", DateTimeOffset.UtcNow.AddHours(8)), ct);
            if (reservation is null)
            {
                throw new InvalidOperationException($"Insufficient stock for product {line.SkuSnapshot}.");
            }
        }

        await transaction.CommitAsync(ct);
        return order;
    }

    public async Task<SalesOrder> CreateCounterSaleAsync(CounterSaleRequest request, CancellationToken ct)
    {
        var order = await CreateOrderAsync(new CreateSalesOrderRequest(
            null,
            request.CustomerId,
            "Venda de mercadoria adquirida ou recebida de terceiros",
            request.PriceTable,
            request.PaymentTerms,
            request.SalespersonId,
            request.CommissionPercent,
            request.Lines), ct);

        order.Status = SalesOrderStatus.ReadyForFiscal;
        order.RomaneioCode = $"BALCAO-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        await db.SaveChangesAsync(ct);
        return order;
    }

    public async Task<SalesOrder?> ApproveAsync(Guid id, CancellationToken ct)
    {
        var order = await db.SalesOrders.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && x.Id == id, ct);
        if (order is null)
        {
            return null;
        }

        order.ApproveForFiscal();
        await db.SaveChangesAsync(ct);
        return order;
    }

    public async Task<SalesOrder?> RegisterReturnAsync(Guid id, RegisterReturnRequest request, CancellationToken ct)
    {
        var order = await db.SalesOrders.Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && x.Id == id, ct);
        if (order is null)
        {
            return null;
        }

        order.RegisterReturn(request.IsWarranty, request.Reason);
        await db.SaveChangesAsync(ct);
        return order;
    }

    public async Task<CashSession> OpenCashSessionAsync(OpenCashSessionRequest request, CancellationToken ct)
    {
        var session = new CashSession
        {
            CompanyId = tenant.CompanyId,
            BranchId = tenant.BranchId,
            OperatorId = request.OperatorId.Trim(),
            OpenedAt = DateTimeOffset.UtcNow,
            OpeningAmount = request.OpeningAmount,
            Status = CashSessionStatus.Open
        };
        db.CashSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return session;
    }

    public async Task<CashSession?> CloseCashSessionAsync(Guid id, CloseCashSessionRequest request, CancellationToken ct)
    {
        var session = await db.CashSessions.FirstOrDefaultAsync(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && x.Id == id, ct);
        if (session is null)
        {
            return null;
        }

        session.Status = CashSessionStatus.Closed;
        session.ClosedAt = DateTimeOffset.UtcNow;
        session.DeclaredAmount = request.DeclaredAmount;
        session.DifferenceAmount = request.DeclaredAmount - request.ExpectedAmount;
        await db.SaveChangesAsync(ct);
        return session;
    }

    private async Task AddLines(SalesOrder order, IReadOnlyCollection<CreateSalesOrderLineRequest> lines, CancellationToken ct)
    {
        var productIds = lines.Select(x => x.ProductId).Distinct().ToArray();
        var products = await db.Products
            .Where(x => x.CompanyId == tenant.CompanyId && productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        foreach (var line in lines)
        {
            var product = products[line.ProductId];
            order.Lines.Add(new SalesOrderLine
            {
                CompanyId = tenant.CompanyId,
                BranchId = tenant.BranchId,
                ProductId = line.ProductId,
                LocationId = line.LocationId,
                SkuSnapshot = product.Sku,
                DescriptionSnapshot = product.Description,
                NcmSnapshot = product.Ncm,
                CestSnapshot = product.Cest,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                Discount = line.Discount
            });
        }
    }

    private async Task<string> CustomerName(Guid? customerId, CancellationToken ct)
    {
        if (customerId is null)
        {
            return "Consumidor final";
        }

        return await db.Customers
            .Where(x => x.CompanyId == tenant.CompanyId && x.Id == customerId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(ct) ?? "Consumidor final";
    }
}

public sealed class SalesQuote : TenantEntity, IAggregateRoot
{
    public Guid? CustomerId { get; set; }
    public string CustomerNameSnapshot { get; set; } = "";
    public string Status { get; set; } = SalesQuoteStatus.Open;
    public DateOnly ValidUntil { get; set; }
    public decimal Total { get; set; }
    public string LinesJson { get; set; } = "[]";
}

public sealed class SalesOrder : TenantEntity, IAggregateRoot
{
    public Guid? QuoteId { get; set; }
    public Guid? CustomerId { get; set; }
    public string CustomerNameSnapshot { get; set; } = "";
    public string Status { get; set; } = SalesOrderStatus.Pending;
    public string OperationNature { get; set; } = "";
    public string PriceTable { get; set; } = "";
    public string PaymentTerms { get; set; } = "";
    public string? SalespersonId { get; set; }
    public decimal CommissionPercent { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string RomaneioCode { get; set; } = "";
    public string? ReturnReason { get; set; }
    public List<SalesOrderLine> Lines { get; set; } = [];

    public void Recalculate()
    {
        Subtotal = Lines.Sum(x => x.Quantity * x.UnitPrice);
        Discount = Lines.Sum(x => x.Discount);
        Total = Subtotal - Discount;
    }

    public void AddCreatedEvent() => AddDomainEvent(new SalesOrderCreated(Id, CompanyId, BranchId, Total, DateTimeOffset.UtcNow));

    public void ApproveForFiscal()
    {
        Status = SalesOrderStatus.ReadyForFiscal;
        AddDomainEvent(new SalesOrderApproved(Id, CompanyId, BranchId, Total, DateTimeOffset.UtcNow));
    }

    public void RegisterReturn(bool isWarranty, string reason)
    {
        Status = isWarranty ? SalesOrderStatus.Warranty : SalesOrderStatus.Returned;
        ReturnReason = reason.Trim();
        AddDomainEvent(new SalesReturnRegistered(Id, CompanyId, BranchId, isWarranty, DateTimeOffset.UtcNow));
    }
}

public sealed class SalesOrderLine : TenantEntity
{
    public Guid SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public string SkuSnapshot { get; set; } = "";
    public string DescriptionSnapshot { get; set; } = "";
    public string NcmSnapshot { get; set; } = "";
    public string? CestSnapshot { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal Total => Quantity * UnitPrice - Discount;
}

public sealed class CashSession : TenantEntity
{
    public string OperatorId { get; set; } = "";
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public decimal OpeningAmount { get; set; }
    public decimal DeclaredAmount { get; set; }
    public decimal DifferenceAmount { get; set; }
    public string Status { get; set; } = CashSessionStatus.Open;
}

public static class SalesQuoteStatus
{
    public const string Open = "open";
    public const string Converted = "converted";
    public const string Expired = "expired";
}

public static class SalesOrderStatus
{
    public const string Pending = "pending";
    public const string ReadyForFiscal = "ready_for_fiscal";
    public const string Invoiced = "invoiced";
    public const string Returned = "returned";
    public const string Warranty = "warranty";
    public const string Cancelled = "cancelled";
}

public static class CashSessionStatus
{
    public const string Open = "open";
    public const string Closed = "closed";
}

public sealed record SalesOrderCreated(Guid SalesOrderId, Guid CompanyId, Guid BranchId, decimal Total, DateTimeOffset OccurredAt) : IDomainEvent;
public sealed record SalesOrderApproved(Guid SalesOrderId, Guid CompanyId, Guid BranchId, decimal Total, DateTimeOffset OccurredAt) : IDomainEvent;
public sealed record SalesReturnRegistered(Guid SalesOrderId, Guid CompanyId, Guid BranchId, bool IsWarranty, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record CreateQuoteRequest(Guid? CustomerId, DateOnly ValidUntil, IReadOnlyCollection<CreateSalesOrderLineRequest> Lines);
public sealed record CreateSalesOrderRequest(Guid? QuoteId, Guid? CustomerId, string OperationNature, string PriceTable, string PaymentTerms, string? SalespersonId, decimal CommissionPercent, IReadOnlyCollection<CreateSalesOrderLineRequest> Lines);
public sealed record CounterSaleRequest(Guid? CustomerId, string PriceTable, string PaymentTerms, string? SalespersonId, decimal CommissionPercent, IReadOnlyCollection<CreateSalesOrderLineRequest> Lines);
public sealed record CreateSalesOrderLineRequest(Guid ProductId, Guid LocationId, decimal Quantity, decimal UnitPrice, decimal Discount);
public sealed record RegisterReturnRequest(bool IsWarranty, string Reason);
public sealed record OpenCashSessionRequest(string OperatorId, decimal OpeningAmount);
public sealed record CloseCashSessionRequest(decimal ExpectedAmount, decimal DeclaredAmount);

public sealed record SalesQuoteSummary(Guid Id, Guid? CustomerId, string Customer, string Status, DateOnly ValidUntil, decimal Total)
{
    public static SalesQuoteSummary From(SalesQuote quote) => new(quote.Id, quote.CustomerId, quote.CustomerNameSnapshot, quote.Status, quote.ValidUntil, quote.Total);
}

public sealed record SalesOrderSummary(Guid Id, Guid? CustomerId, string Customer, string Status, decimal Total, string RomaneioCode, IReadOnlyCollection<SalesOrderLineSummary> Lines)
{
    public static SalesOrderSummary From(SalesOrder order)
        => new(order.Id, order.CustomerId, order.CustomerNameSnapshot, order.Status, order.Total, order.RomaneioCode, order.Lines.Select(SalesOrderLineSummary.From).ToArray());
}

public sealed record SalesOrderLineSummary(Guid ProductId, string Sku, string Description, decimal Quantity, decimal UnitPrice, decimal Discount, decimal Total)
{
    public static SalesOrderLineSummary From(SalesOrderLine line) => new(line.ProductId, line.SkuSnapshot, line.DescriptionSnapshot, line.Quantity, line.UnitPrice, line.Discount, line.Total);
}

public sealed record CashSessionSummary(Guid Id, string OperatorId, string Status, DateTimeOffset OpenedAt, DateTimeOffset? ClosedAt, decimal DifferenceAmount)
{
    public static CashSessionSummary From(CashSession session) => new(session.Id, session.OperatorId, session.Status, session.OpenedAt, session.ClosedAt, session.DifferenceAmount);
}

public static class SalesMapping
{
    public static void ConfigureSales(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SalesQuote>(builder =>
        {
            builder.ToTable("sales_quotes", "sales");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.CustomerNameSnapshot).HasMaxLength(220).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
            builder.Property(x => x.LinesJson).HasColumnType("jsonb").IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.Status, x.ValidUntil });
        });

        modelBuilder.Entity<SalesOrder>(builder =>
        {
            builder.ToTable("sales_orders", "sales");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.CustomerNameSnapshot).HasMaxLength(220).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
            builder.Property(x => x.OperationNature).HasMaxLength(220).IsRequired();
            builder.Property(x => x.PriceTable).HasMaxLength(80).IsRequired();
            builder.Property(x => x.PaymentTerms).HasMaxLength(120).IsRequired();
            builder.Property(x => x.SalespersonId).HasMaxLength(160);
            builder.Property(x => x.RomaneioCode).HasMaxLength(80).IsRequired();
            builder.Property(x => x.ReturnReason).HasMaxLength(400);
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.Status, x.CreatedAt });
        });

        modelBuilder.Entity<SalesOrderLine>(builder =>
        {
            builder.ToTable("sales_order_lines", "sales");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.SkuSnapshot).HasMaxLength(80).IsRequired();
            builder.Property(x => x.DescriptionSnapshot).HasMaxLength(420).IsRequired();
            builder.Property(x => x.NcmSnapshot).HasMaxLength(16).IsRequired();
            builder.Property(x => x.CestSnapshot).HasMaxLength(16);
            builder.Ignore(x => x.Total);
            builder.HasOne(x => x.SalesOrder).WithMany(x => x.Lines).HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.ProductId, x.CreatedAt });
        });

        modelBuilder.Entity<CashSession>(builder =>
        {
            builder.ToTable("cash_sessions", "sales");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.OperatorId).HasMaxLength(160).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.OperatorId, x.Status });
        });
    }
}
