using Atlas.Api.Common;
using Atlas.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api.Catalog;

public sealed class CatalogModule : IEndpointModule
{
    public string Name => "Catalog and Compatibility";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/catalog")
            .WithTags(Name)
            .RequireAuthorization();

        group.MapGet("/products/search", async Task<Ok<IReadOnlyCollection<ProductSearchResult>>> (
            string? term,
            string? make,
            string? model,
            int? year,
            string? engine,
            string? chassis,
            string? vin,
            int? limit,
            ProductSearchService searchService,
            CancellationToken ct) =>
        {
            var request = new ProductSearchRequest(term, make, model, year, engine, chassis, vin, limit);
            var result = await searchService.SearchAsync(request, ct);
            return TypedResults.Ok(result);
        });

        group.MapGet("/products/{id:guid}", async Task<Results<Ok<ProductDetail>, NotFound>> (
            Guid id,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var product = await db.Products
                .AsNoTracking()
                .Include(x => x.Applications)
                .Include(x => x.Equivalents)
                .Include(x => x.SupplierOffers)
                .Include(x => x.PriceHistory)
                .Where(x => x.CompanyId == tenant.CompanyId && x.Id == id)
                .Select(x => ProductDetail.From(x))
                .FirstOrDefaultAsync(ct);

            return product is null ? TypedResults.NotFound() : TypedResults.Ok(product);
        });

        group.MapPost("/products", async Task<Created<ProductDetail>> (
            CreateProductRequest request,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var product = AutoPartProduct.Create(request, tenant.CompanyId, tenant.BranchId);
            db.Products.Add(product);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/catalog/products/{product.Id}", ProductDetail.From(product));
        });

        group.MapPost("/products/{id:guid}/applications", async Task<Results<Ok<ProductDetail>, NotFound>> (
            Guid id,
            AddApplicationRequest request,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var product = await db.Products
                .Include(x => x.Applications)
                .Include(x => x.Equivalents)
                .Include(x => x.SupplierOffers)
                .Include(x => x.PriceHistory)
                .FirstOrDefaultAsync(x => x.CompanyId == tenant.CompanyId && x.Id == id, ct);

            if (product is null)
            {
                return TypedResults.NotFound();
            }

            product.AddApplication(request);
            await db.SaveChangesAsync(ct);
            return TypedResults.Ok(ProductDetail.From(product));
        });

        group.MapPost("/products/{id:guid}/equivalents", async Task<Results<Ok<ProductDetail>, NotFound>> (
            Guid id,
            AddEquivalentRequest request,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var product = await db.Products
                .Include(x => x.Applications)
                .Include(x => x.Equivalents)
                .Include(x => x.SupplierOffers)
                .Include(x => x.PriceHistory)
                .FirstOrDefaultAsync(x => x.CompanyId == tenant.CompanyId && x.Id == id, ct);

            if (product is null)
            {
                return TypedResults.NotFound();
            }

            product.AddEquivalent(request.Code, request.Type, request.Brand, request.Notes);
            await db.SaveChangesAsync(ct);
            return TypedResults.Ok(ProductDetail.From(product));
        });

        group.MapPost("/products/{id:guid}/price", async Task<Results<Ok<ProductDetail>, NotFound>> (
            Guid id,
            UpdateProductPriceRequest request,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var product = await db.Products
                .Include(x => x.Applications)
                .Include(x => x.Equivalents)
                .Include(x => x.SupplierOffers)
                .Include(x => x.PriceHistory)
                .FirstOrDefaultAsync(x => x.CompanyId == tenant.CompanyId && x.Id == id, ct);

            if (product is null)
            {
                return TypedResults.NotFound();
            }

            product.UpdateCommercialPolicy(request.Cost, request.MarginPercent, request.PriceTable, request.ValidFrom);
            await db.SaveChangesAsync(ct);
            return TypedResults.Ok(ProductDetail.From(product));
        });
    }
}

public sealed class ProductSearchService(AtlasDbContext db, ITenantContext tenant)
{
    public async Task<IReadOnlyCollection<ProductSearchResult>> SearchAsync(ProductSearchRequest request, CancellationToken ct)
    {
        var term = FiscalStrings.NormalizeCode(request.Term);
        var query = db.Products
            .AsNoTracking()
            .Include(x => x.Applications)
            .Include(x => x.Equivalents)
            .Where(x => x.CompanyId == tenant.CompanyId);

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(x =>
                x.Sku.ToUpper().Contains(term)
                || x.Description.ToUpper().Contains(term)
                || x.Gtin.ToUpper().Contains(term)
                || x.ManufacturerCode.ToUpper().Contains(term)
                || x.OeCode.ToUpper().Contains(term)
                || x.Ncm.ToUpper().Contains(term)
                || x.Equivalents.Any(eq => eq.Code.ToUpper().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.Make))
        {
            var make = FiscalStrings.NormalizeCode(request.Make);
            query = query.Where(x => x.Applications.Any(a => a.Make.ToUpper() == make));
        }

        if (!string.IsNullOrWhiteSpace(request.Model))
        {
            var model = FiscalStrings.NormalizeCode(request.Model);
            query = query.Where(x => x.Applications.Any(a => a.Model.ToUpper().Contains(model)));
        }

        if (request.Year is not null)
        {
            var year = request.Year.Value;
            query = query.Where(x => x.Applications.Any(a => a.FromYear <= year && (a.ToYear == null || a.ToYear >= year)));
        }

        if (!string.IsNullOrWhiteSpace(request.Engine))
        {
            var engine = FiscalStrings.NormalizeCode(request.Engine);
            query = query.Where(x => x.Applications.Any(a => a.Engine.ToUpper().Contains(engine)));
        }

        if (!string.IsNullOrWhiteSpace(request.Chassis))
        {
            var chassis = FiscalStrings.NormalizeCode(request.Chassis);
            query = query.Where(x => x.Applications.Any(a =>
                (a.ChassisFrom == null || string.Compare(a.ChassisFrom, chassis, StringComparison.Ordinal) <= 0)
                && (a.ChassisTo == null || string.Compare(a.ChassisTo, chassis, StringComparison.Ordinal) >= 0)));
        }

        return await query
            .OrderBy(x => x.BrandName)
            .ThenBy(x => x.Description)
            .Take(Math.Clamp(request.Limit ?? 50, 1, 200))
            .Select(x => ProductSearchResult.From(x))
            .ToListAsync(ct);
    }
}

public sealed class AutoPartProduct : TenantEntity, IAggregateRoot
{
    public string Sku { get; set; } = "";
    public string Description { get; set; } = "";
    public string Gtin { get; set; } = "";
    public string Ncm { get; set; } = "";
    public string? Cest { get; set; }
    public string CommercialUnit { get; set; } = "UN";
    public string TaxUnit { get; set; } = "UN";
    public string BrandName { get; set; } = "";
    public string ProductLine { get; set; } = "";
    public string ManufacturerName { get; set; } = "";
    public string ManufacturerCode { get; set; } = "";
    public string OeCode { get; set; } = "";
    public string Origin { get; set; } = "0";
    public decimal WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? LengthCm { get; set; }
    public string TechnicalAttributes { get; set; } = "{}";
    public string PhotoUrls { get; set; } = "[]";
    public bool IsKit { get; set; }
    public string KitComposition { get; set; } = "[]";
    public decimal LastCost { get; set; }
    public decimal MarginPercent { get; set; }
    public decimal SuggestedPrice { get; set; }
    public DateTimeOffset? DiscontinuedAt { get; set; }
    public List<ProductApplication> Applications { get; set; } = [];
    public List<ProductEquivalent> Equivalents { get; set; } = [];
    public List<ProductSupplierOffer> SupplierOffers { get; set; } = [];
    public List<ProductPriceHistory> PriceHistory { get; set; } = [];

    public static AutoPartProduct Create(CreateProductRequest request, Guid companyId, Guid branchId)
    {
        var product = new AutoPartProduct
        {
            CompanyId = companyId,
            BranchId = branchId,
            Sku = FiscalStrings.NormalizeCode(request.Sku),
            Description = request.Description.Trim(),
            Gtin = request.Gtin?.Trim() ?? "",
            Ncm = FiscalStrings.OnlyDigits(request.Ncm),
            Cest = string.IsNullOrWhiteSpace(request.Cest) ? null : FiscalStrings.OnlyDigits(request.Cest),
            CommercialUnit = request.CommercialUnit.Trim().ToUpperInvariant(),
            TaxUnit = request.TaxUnit.Trim().ToUpperInvariant(),
            BrandName = request.BrandName.Trim(),
            ProductLine = request.ProductLine.Trim(),
            ManufacturerName = request.ManufacturerName.Trim(),
            ManufacturerCode = FiscalStrings.NormalizeCode(request.ManufacturerCode),
            OeCode = FiscalStrings.NormalizeCode(request.OeCode),
            Origin = request.Origin.Trim(),
            WeightKg = request.WeightKg,
            HeightCm = request.HeightCm,
            WidthCm = request.WidthCm,
            LengthCm = request.LengthCm,
            TechnicalAttributes = request.TechnicalAttributesJson ?? "{}",
            PhotoUrls = request.PhotoUrlsJson ?? "[]",
            LastCost = request.Cost,
            MarginPercent = request.MarginPercent
        };

        product.SuggestedPrice = product.CalculateSuggestedPrice();
        product.AddDomainEvent(new ProductCreated(product.Id, companyId, branchId, product.Sku, DateTimeOffset.UtcNow));
        return product;
    }

    public void AddApplication(AddApplicationRequest request)
    {
        Applications.Add(new ProductApplication
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            ProductId = Id,
            Make = request.Make.Trim(),
            Model = request.Model.Trim(),
            FromYear = request.FromYear,
            ToYear = request.ToYear,
            Engine = request.Engine.Trim(),
            Fuel = request.Fuel.Trim(),
            ChassisFrom = string.IsNullOrWhiteSpace(request.ChassisFrom) ? null : request.ChassisFrom.Trim().ToUpperInvariant(),
            ChassisTo = string.IsNullOrWhiteSpace(request.ChassisTo) ? null : request.ChassisTo.Trim().ToUpperInvariant(),
            VinFilterExpression = request.VinFilterExpression,
            Notes = request.Notes
        });
        AddDomainEvent(new ProductCompatibilityChanged(Id, CompanyId, BranchId, DateTimeOffset.UtcNow));
    }

    public void AddEquivalent(string code, string type, string? brand, string? notes)
    {
        Equivalents.Add(new ProductEquivalent
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            ProductId = Id,
            Code = FiscalStrings.NormalizeCode(code),
            Type = FiscalStrings.NormalizeCode(type),
            Brand = brand?.Trim(),
            Notes = notes
        });
        AddDomainEvent(new ProductCompatibilityChanged(Id, CompanyId, BranchId, DateTimeOffset.UtcNow));
    }

    public void UpdateCommercialPolicy(decimal cost, decimal marginPercent, string priceTable, DateOnly validFrom)
    {
        LastCost = cost;
        MarginPercent = marginPercent;
        SuggestedPrice = CalculateSuggestedPrice();
        PriceHistory.Add(new ProductPriceHistory
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            ProductId = Id,
            PriceTable = priceTable.Trim(),
            Cost = cost,
            MarginPercent = marginPercent,
            Price = SuggestedPrice,
            ValidFrom = validFrom
        });
        AddDomainEvent(new ProductPriceChanged(Id, CompanyId, BranchId, SuggestedPrice, DateTimeOffset.UtcNow));
    }

    private decimal CalculateSuggestedPrice() => Math.Round(LastCost * (1 + MarginPercent / 100), 2);
}

public sealed class ProductApplication : TenantEntity
{
    public Guid ProductId { get; set; }
    public AutoPartProduct? Product { get; set; }
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public int FromYear { get; set; }
    public int? ToYear { get; set; }
    public string Engine { get; set; } = "";
    public string Fuel { get; set; } = "";
    public string? ChassisFrom { get; set; }
    public string? ChassisTo { get; set; }
    public string? VinFilterExpression { get; set; }
    public string? Notes { get; set; }
}

public sealed class ProductEquivalent : TenantEntity
{
    public Guid ProductId { get; set; }
    public AutoPartProduct? Product { get; set; }
    public string Code { get; set; } = "";
    public string Type { get; set; } = "EQUIVALENT";
    public string? Brand { get; set; }
    public string? Notes { get; set; }
}

public sealed class ProductSupplierOffer : TenantEntity
{
    public Guid ProductId { get; set; }
    public AutoPartProduct? Product { get; set; }
    public Guid SupplierId { get; set; }
    public string SupplierCode { get; set; } = "";
    public decimal LastCost { get; set; }
    public int LeadTimeDays { get; set; }
    public bool Preferred { get; set; }
}

public sealed class ProductPriceHistory : TenantEntity
{
    public Guid ProductId { get; set; }
    public AutoPartProduct? Product { get; set; }
    public string PriceTable { get; set; } = "varejo";
    public decimal Cost { get; set; }
    public decimal MarginPercent { get; set; }
    public decimal Price { get; set; }
    public DateOnly ValidFrom { get; set; }
}

public sealed class VehicleModel : TenantEntity
{
    public string Make { get; set; } = "";
    public string Model { get; set; } = "";
    public int FromYear { get; set; }
    public int? ToYear { get; set; }
    public string Engine { get; set; } = "";
    public string Fuel { get; set; } = "";
    public string ExternalCatalogRef { get; set; } = "";
}

public sealed record ProductCreated(Guid ProductId, Guid CompanyId, Guid BranchId, string Sku, DateTimeOffset OccurredAt) : IDomainEvent;
public sealed record ProductCompatibilityChanged(Guid ProductId, Guid CompanyId, Guid BranchId, DateTimeOffset OccurredAt) : IDomainEvent;
public sealed record ProductPriceChanged(Guid ProductId, Guid CompanyId, Guid BranchId, decimal Price, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ProductSearchRequest(
    string? Term,
    string? Make,
    string? Model,
    int? Year,
    string? Engine,
    string? Chassis,
    string? Vin,
    int? Limit);

public sealed record CreateProductRequest(
    string Sku,
    string Description,
    string? Gtin,
    string Ncm,
    string? Cest,
    string CommercialUnit,
    string TaxUnit,
    string BrandName,
    string ProductLine,
    string ManufacturerName,
    string ManufacturerCode,
    string OeCode,
    string Origin,
    decimal WeightKg,
    decimal? HeightCm,
    decimal? WidthCm,
    decimal? LengthCm,
    string? TechnicalAttributesJson,
    string? PhotoUrlsJson,
    decimal Cost,
    decimal MarginPercent);

public sealed record AddApplicationRequest(
    string Make,
    string Model,
    int FromYear,
    int? ToYear,
    string Engine,
    string Fuel,
    string? ChassisFrom,
    string? ChassisTo,
    string? VinFilterExpression,
    string? Notes);

public sealed record AddEquivalentRequest(string Code, string Type, string? Brand, string? Notes);
public sealed record UpdateProductPriceRequest(decimal Cost, decimal MarginPercent, string PriceTable, DateOnly ValidFrom);

public sealed record ProductSearchResult(
    Guid Id,
    string Sku,
    string Description,
    string Brand,
    string ManufacturerCode,
    string OeCode,
    string Gtin,
    decimal SuggestedPrice,
    IReadOnlyCollection<string> Equivalents,
    IReadOnlyCollection<string> Applications)
{
    public static ProductSearchResult From(AutoPartProduct product)
        => new(
            product.Id,
            product.Sku,
            product.Description,
            product.BrandName,
            product.ManufacturerCode,
            product.OeCode,
            product.Gtin,
            product.SuggestedPrice,
            product.Equivalents.Select(x => x.Code).Take(8).ToArray(),
            product.Applications.Select(x => $"{x.Make} {x.Model} {x.FromYear}-{x.ToYear?.ToString() ?? "atual"} {x.Engine}").Take(8).ToArray());
}

public sealed record ProductDetail(
    Guid Id,
    string Sku,
    string Description,
    string Gtin,
    string Ncm,
    string? Cest,
    string Brand,
    string ManufacturerCode,
    string OeCode,
    decimal LastCost,
    decimal MarginPercent,
    decimal SuggestedPrice,
    IReadOnlyCollection<ProductApplicationDetail> Applications,
    IReadOnlyCollection<ProductEquivalentDetail> Equivalents,
    IReadOnlyCollection<ProductSupplierOfferDetail> Suppliers,
    IReadOnlyCollection<ProductPriceHistoryDetail> Prices)
{
    public static ProductDetail From(AutoPartProduct product)
        => new(
            product.Id,
            product.Sku,
            product.Description,
            product.Gtin,
            product.Ncm,
            product.Cest,
            product.BrandName,
            product.ManufacturerCode,
            product.OeCode,
            product.LastCost,
            product.MarginPercent,
            product.SuggestedPrice,
            product.Applications.Select(ProductApplicationDetail.From).ToArray(),
            product.Equivalents.Select(ProductEquivalentDetail.From).ToArray(),
            product.SupplierOffers.Select(ProductSupplierOfferDetail.From).ToArray(),
            product.PriceHistory.Select(ProductPriceHistoryDetail.From).ToArray());
}

public sealed record ProductApplicationDetail(string Make, string Model, int FromYear, int? ToYear, string Engine, string Fuel, string? ChassisFrom, string? ChassisTo)
{
    public static ProductApplicationDetail From(ProductApplication application)
        => new(application.Make, application.Model, application.FromYear, application.ToYear, application.Engine, application.Fuel, application.ChassisFrom, application.ChassisTo);
}

public sealed record ProductEquivalentDetail(string Code, string Type, string? Brand)
{
    public static ProductEquivalentDetail From(ProductEquivalent equivalent) => new(equivalent.Code, equivalent.Type, equivalent.Brand);
}

public sealed record ProductSupplierOfferDetail(Guid SupplierId, string SupplierCode, decimal LastCost, int LeadTimeDays, bool Preferred)
{
    public static ProductSupplierOfferDetail From(ProductSupplierOffer offer) => new(offer.SupplierId, offer.SupplierCode, offer.LastCost, offer.LeadTimeDays, offer.Preferred);
}

public sealed record ProductPriceHistoryDetail(string PriceTable, decimal Cost, decimal MarginPercent, decimal Price, DateOnly ValidFrom)
{
    public static ProductPriceHistoryDetail From(ProductPriceHistory price) => new(price.PriceTable, price.Cost, price.MarginPercent, price.Price, price.ValidFrom);
}

public static class CatalogMapping
{
    public static void ConfigureCatalog(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AutoPartProduct>(builder =>
        {
            builder.ToTable("products", "catalog");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.Sku).HasMaxLength(80).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(420).IsRequired();
            builder.Property(x => x.Gtin).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Ncm).HasMaxLength(16).IsRequired();
            builder.Property(x => x.Cest).HasMaxLength(16);
            builder.Property(x => x.CommercialUnit).HasMaxLength(8).IsRequired();
            builder.Property(x => x.TaxUnit).HasMaxLength(8).IsRequired();
            builder.Property(x => x.BrandName).HasMaxLength(120).IsRequired();
            builder.Property(x => x.ProductLine).HasMaxLength(120).IsRequired();
            builder.Property(x => x.ManufacturerName).HasMaxLength(160).IsRequired();
            builder.Property(x => x.ManufacturerCode).HasMaxLength(120).IsRequired();
            builder.Property(x => x.OeCode).HasMaxLength(120).IsRequired();
            builder.Property(x => x.Origin).HasMaxLength(4).IsRequired();
            builder.Property(x => x.TechnicalAttributes).HasColumnType("jsonb").IsRequired();
            builder.Property(x => x.PhotoUrls).HasColumnType("jsonb").IsRequired();
            builder.Property(x => x.KitComposition).HasColumnType("jsonb").IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.Sku }).IsUnique();
            builder.HasIndex(x => new { x.CompanyId, x.Gtin });
            builder.HasIndex(x => new { x.CompanyId, x.ManufacturerCode });
            builder.HasIndex(x => new { x.CompanyId, x.OeCode });
        });

        modelBuilder.Entity<ProductApplication>(builder =>
        {
            builder.ToTable("product_applications", "catalog");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.Make).HasMaxLength(120).IsRequired();
            builder.Property(x => x.Model).HasMaxLength(160).IsRequired();
            builder.Property(x => x.Engine).HasMaxLength(120).IsRequired();
            builder.Property(x => x.Fuel).HasMaxLength(40).IsRequired();
            builder.Property(x => x.ChassisFrom).HasMaxLength(40);
            builder.Property(x => x.ChassisTo).HasMaxLength(40);
            builder.Property(x => x.VinFilterExpression).HasMaxLength(600);
            builder.HasOne(x => x.Product).WithMany(x => x.Applications).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.CompanyId, x.Make, x.Model, x.FromYear, x.ToYear });
        });

        modelBuilder.Entity<ProductEquivalent>(builder =>
        {
            builder.ToTable("product_equivalents", "catalog");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.Code).HasMaxLength(120).IsRequired();
            builder.Property(x => x.Type).HasMaxLength(40).IsRequired();
            builder.Property(x => x.Brand).HasMaxLength(120);
            builder.HasOne(x => x.Product).WithMany(x => x.Equivalents).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.CompanyId, x.Code });
        });

        modelBuilder.Entity<ProductSupplierOffer>(builder =>
        {
            builder.ToTable("product_supplier_offers", "catalog");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.SupplierCode).HasMaxLength(120).IsRequired();
            builder.HasOne(x => x.Product).WithMany(x => x.SupplierOffers).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.CompanyId, x.SupplierId, x.SupplierCode });
        });

        modelBuilder.Entity<ProductPriceHistory>(builder =>
        {
            builder.ToTable("product_price_history", "catalog");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.PriceTable).HasMaxLength(80).IsRequired();
            builder.HasOne(x => x.Product).WithMany(x => x.PriceHistory).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.CompanyId, x.ProductId, x.PriceTable, x.ValidFrom });
        });

        modelBuilder.Entity<VehicleModel>(builder =>
        {
            builder.ToTable("vehicle_models", "catalog");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.Make).HasMaxLength(120).IsRequired();
            builder.Property(x => x.Model).HasMaxLength(160).IsRequired();
            builder.Property(x => x.Engine).HasMaxLength(120).IsRequired();
            builder.Property(x => x.Fuel).HasMaxLength(40).IsRequired();
            builder.Property(x => x.ExternalCatalogRef).HasMaxLength(160);
            builder.HasIndex(x => new { x.CompanyId, x.Make, x.Model, x.FromYear, x.ToYear });
        });
    }
}
