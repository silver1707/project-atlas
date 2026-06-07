using Atlas.Api.Catalog;
using Atlas.Api.Common;
using Atlas.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api.Inventory;

public sealed class InventoryModule : IEndpointModule
{
    public string Name => "Inventory";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/inventory")
            .WithTags(Name)
            .RequireAuthorization();

        group.MapGet("/balances", async Task<Ok<IReadOnlyCollection<StockBalanceSummary>>> (
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var balances = await db.StockBalances
                .AsNoTracking()
                .Include(x => x.Product)
                .Include(x => x.Location)
                .Where(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId)
                .OrderBy(x => x.Product!.Sku)
                .Select(x => StockBalanceSummary.From(x))
                .ToListAsync(ct);
            return TypedResults.Ok(balances);
        });

        group.MapPost("/locations", async Task<Created<StockLocationSummary>> (
            CreateStockLocationRequest request,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var location = new StockLocation
            {
                CompanyId = tenant.CompanyId,
                BranchId = tenant.BranchId,
                Code = request.Code.Trim().ToUpperInvariant(),
                Name = request.Name.Trim(),
                Type = request.Type.Trim().ToLowerInvariant(),
                AllowsPicking = request.AllowsPicking
            };
            db.StockLocations.Add(location);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/inventory/locations/{location.Id}", StockLocationSummary.From(location));
        });

        group.MapPost("/receipts", async Task<Ok<StockBalanceSummary>> (
            ReceiveStockRequest request,
            InventoryService inventory,
            CancellationToken ct) =>
        {
            var balance = await inventory.ReceiveAsync(request, ct);
            return TypedResults.Ok(StockBalanceSummary.From(balance));
        });

        group.MapPost("/reservations", async Task<Results<Ok<StockReservationSummary>, BadRequest<string>>> (
            ReserveStockRequest request,
            InventoryService inventory,
            CancellationToken ct) =>
        {
            var result = await inventory.ReserveAsync(request, ct);
            return result is null
                ? TypedResults.BadRequest("Insufficient available stock for reservation.")
                : TypedResults.Ok(StockReservationSummary.From(result));
        });

        group.MapPost("/transfers", async Task<Ok<IReadOnlyCollection<StockMovementSummary>>> (
            TransferStockRequest request,
            InventoryService inventory,
            CancellationToken ct) =>
        {
            var movements = await inventory.TransferAsync(request, ct);
            return TypedResults.Ok(movements.Select(StockMovementSummary.From).ToArray());
        });

        group.MapPost("/picking", async Task<Created<PickListSummary>> (
            CreatePickListRequest request,
            InventoryService inventory,
            CancellationToken ct) =>
        {
            var pickList = await inventory.CreatePickListAsync(request, ct);
            return TypedResults.Created($"/api/inventory/picking/{pickList.Id}", PickListSummary.From(pickList));
        });

        group.MapPost("/picking/{id:guid}/confirm", async Task<Results<Ok<PickListSummary>, NotFound>> (
            Guid id,
            InventoryService inventory,
            CancellationToken ct) =>
        {
            var pickList = await inventory.ConfirmPickListAsync(id, ct);
            return pickList is null ? TypedResults.NotFound() : TypedResults.Ok(PickListSummary.From(pickList));
        });
    }
}

public sealed class InventoryService(AtlasDbContext db, ITenantContext tenant)
{
    public async Task<StockBalance> ReceiveAsync(ReceiveStockRequest request, CancellationToken ct)
    {
        var balance = await GetOrCreateBalance(request.ProductId, request.LocationId, ct);
        balance.OnHandQuantity += request.Quantity;
        balance.AvailableQuantity += request.Quantity;
        balance.LastCost = request.UnitCost;

        db.StockMovements.Add(StockMovement.Create(
            tenant.CompanyId,
            tenant.BranchId,
            request.ProductId,
            request.LocationId,
            StockMovementType.Receipt,
            request.Quantity,
            request.UnitCost,
            request.Reference,
            "Entrada de compra/XML"));

        await db.SaveChangesAsync(ct);
        return balance;
    }

    public async Task<StockReservation?> ReserveAsync(ReserveStockRequest request, CancellationToken ct)
    {
        var balance = await db.StockBalances
            .FirstOrDefaultAsync(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && x.ProductId == request.ProductId && x.LocationId == request.LocationId, ct);

        if (balance is null || balance.AvailableQuantity < request.Quantity)
        {
            return null;
        }

        balance.AvailableQuantity -= request.Quantity;
        balance.ReservedQuantity += request.Quantity;

        var reservation = new StockReservation
        {
            CompanyId = tenant.CompanyId,
            BranchId = tenant.BranchId,
            ProductId = request.ProductId,
            LocationId = request.LocationId,
            Quantity = request.Quantity,
            SourceDocumentId = request.SourceDocumentId,
            SourceType = request.SourceType,
            ExpiresAt = request.ExpiresAt,
            Status = StockReservationStatus.Active
        };

        db.StockReservations.Add(reservation);
        await db.SaveChangesAsync(ct);
        return reservation;
    }

    public async Task<IReadOnlyCollection<StockMovement>> TransferAsync(TransferStockRequest request, CancellationToken ct)
    {
        var source = await db.StockBalances.FirstAsync(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && x.ProductId == request.ProductId && x.LocationId == request.FromLocationId, ct);
        if (source.AvailableQuantity < request.Quantity)
        {
            throw new InvalidOperationException("Insufficient stock for transfer.");
        }

        var target = await GetOrCreateBalance(request.ProductId, request.ToLocationId, ct);
        source.OnHandQuantity -= request.Quantity;
        source.AvailableQuantity -= request.Quantity;
        target.OnHandQuantity += request.Quantity;
        target.AvailableQuantity += request.Quantity;

        var outbound = StockMovement.Create(tenant.CompanyId, tenant.BranchId, request.ProductId, request.FromLocationId, StockMovementType.TransferOut, -request.Quantity, source.LastCost, request.Reference, request.Reason);
        var inbound = StockMovement.Create(tenant.CompanyId, tenant.BranchId, request.ProductId, request.ToLocationId, StockMovementType.TransferIn, request.Quantity, source.LastCost, request.Reference, request.Reason);
        db.StockMovements.AddRange(outbound, inbound);
        await db.SaveChangesAsync(ct);
        return [outbound, inbound];
    }

    public async Task<PickList> CreatePickListAsync(CreatePickListRequest request, CancellationToken ct)
    {
        var pickList = new PickList
        {
            CompanyId = tenant.CompanyId,
            BranchId = tenant.BranchId,
            SourceDocumentId = request.SourceDocumentId,
            SourceType = request.SourceType,
            Status = PickListStatus.Pending,
            LinesJson = JsonPayload.Serialize(request.Lines)
        };

        db.PickLists.Add(pickList);
        await db.SaveChangesAsync(ct);
        return pickList;
    }

    public async Task<PickList?> ConfirmPickListAsync(Guid id, CancellationToken ct)
    {
        var pickList = await db.PickLists.FirstOrDefaultAsync(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && x.Id == id, ct);
        if (pickList is null)
        {
            return null;
        }

        pickList.Status = PickListStatus.Confirmed;
        pickList.ConfirmedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return pickList;
    }

    private async Task<StockBalance> GetOrCreateBalance(Guid productId, Guid locationId, CancellationToken ct)
    {
        var balance = await db.StockBalances.FirstOrDefaultAsync(x =>
            x.CompanyId == tenant.CompanyId
            && x.BranchId == tenant.BranchId
            && x.ProductId == productId
            && x.LocationId == locationId, ct);

        if (balance is not null)
        {
            return balance;
        }

        balance = new StockBalance
        {
            CompanyId = tenant.CompanyId,
            BranchId = tenant.BranchId,
            ProductId = productId,
            LocationId = locationId
        };
        db.StockBalances.Add(balance);
        return balance;
    }
}

public sealed class StockLocation : TenantEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "warehouse";
    public bool AllowsPicking { get; set; } = true;
}

public sealed class StockBalance : TenantEntity
{
    public Guid ProductId { get; set; }
    public AutoPartProduct? Product { get; set; }
    public Guid LocationId { get; set; }
    public StockLocation? Location { get; set; }
    public decimal OnHandQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal MinimumQuantity { get; set; }
    public decimal MaximumQuantity { get; set; }
    public decimal LastCost { get; set; }
}

public sealed class StockMovement : TenantEntity, IAggregateRoot
{
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public string Type { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string Reference { get; set; } = "";
    public string Reason { get; set; } = "";

    public static StockMovement Create(Guid companyId, Guid branchId, Guid productId, Guid locationId, string type, decimal quantity, decimal unitCost, string? reference, string? reason)
    {
        var movement = new StockMovement
        {
            CompanyId = companyId,
            BranchId = branchId,
            ProductId = productId,
            LocationId = locationId,
            Type = type,
            Quantity = quantity,
            UnitCost = unitCost,
            Reference = reference?.Trim() ?? "",
            Reason = reason?.Trim() ?? ""
        };
        movement.AddDomainEvent(new StockMoved(movement.Id, companyId, branchId, productId, locationId, type, quantity, DateTimeOffset.UtcNow));
        return movement;
    }
}

public sealed class StockReservation : TenantEntity
{
    public Guid ProductId { get; set; }
    public Guid LocationId { get; set; }
    public decimal Quantity { get; set; }
    public Guid SourceDocumentId { get; set; }
    public string SourceType { get; set; } = "";
    public DateTimeOffset? ExpiresAt { get; set; }
    public string Status { get; set; } = StockReservationStatus.Active;
}

public sealed class PickList : TenantEntity
{
    public Guid SourceDocumentId { get; set; }
    public string SourceType { get; set; } = "";
    public string Status { get; set; } = PickListStatus.Pending;
    public string LinesJson { get; set; } = "[]";
    public DateTimeOffset? ConfirmedAt { get; set; }
}

public static class StockMovementType
{
    public const string Receipt = "receipt";
    public const string Sale = "sale";
    public const string TransferIn = "transfer_in";
    public const string TransferOut = "transfer_out";
    public const string Return = "return";
    public const string Warranty = "warranty";
    public const string Adjustment = "adjustment";
}

public static class StockReservationStatus
{
    public const string Active = "active";
    public const string Released = "released";
    public const string Consumed = "consumed";
    public const string Expired = "expired";
}

public static class PickListStatus
{
    public const string Pending = "pending";
    public const string Confirmed = "confirmed";
    public const string Cancelled = "cancelled";
}

public sealed record StockMoved(Guid MovementId, Guid CompanyId, Guid BranchId, Guid ProductId, Guid LocationId, string Type, decimal Quantity, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record CreateStockLocationRequest(string Code, string Name, string Type, bool AllowsPicking);
public sealed record ReceiveStockRequest(Guid ProductId, Guid LocationId, decimal Quantity, decimal UnitCost, string? Reference);
public sealed record ReserveStockRequest(Guid ProductId, Guid LocationId, decimal Quantity, Guid SourceDocumentId, string SourceType, DateTimeOffset? ExpiresAt);
public sealed record TransferStockRequest(Guid ProductId, Guid FromLocationId, Guid ToLocationId, decimal Quantity, string? Reference, string? Reason);
public sealed record CreatePickListRequest(Guid SourceDocumentId, string SourceType, IReadOnlyCollection<PickLineRequest> Lines);
public sealed record PickLineRequest(Guid ProductId, Guid LocationId, decimal Quantity);

public sealed record StockLocationSummary(Guid Id, string Code, string Name, string Type, bool AllowsPicking)
{
    public static StockLocationSummary From(StockLocation location) => new(location.Id, location.Code, location.Name, location.Type, location.AllowsPicking);
}

public sealed record StockBalanceSummary(Guid ProductId, string Sku, string Description, Guid LocationId, string Location, decimal OnHand, decimal Reserved, decimal Available, decimal Minimum)
{
    public static StockBalanceSummary From(StockBalance balance)
        => new(balance.ProductId, balance.Product?.Sku ?? "", balance.Product?.Description ?? "", balance.LocationId, balance.Location?.Code ?? "", balance.OnHandQuantity, balance.ReservedQuantity, balance.AvailableQuantity, balance.MinimumQuantity);
}

public sealed record StockReservationSummary(Guid Id, Guid ProductId, Guid LocationId, decimal Quantity, string Status)
{
    public static StockReservationSummary From(StockReservation reservation) => new(reservation.Id, reservation.ProductId, reservation.LocationId, reservation.Quantity, reservation.Status);
}

public sealed record StockMovementSummary(Guid Id, Guid ProductId, Guid LocationId, string Type, decimal Quantity, string Reference)
{
    public static StockMovementSummary From(StockMovement movement) => new(movement.Id, movement.ProductId, movement.LocationId, movement.Type, movement.Quantity, movement.Reference);
}

public sealed record PickListSummary(Guid Id, Guid SourceDocumentId, string SourceType, string Status, DateTimeOffset? ConfirmedAt)
{
    public static PickListSummary From(PickList pickList) => new(pickList.Id, pickList.SourceDocumentId, pickList.SourceType, pickList.Status, pickList.ConfirmedAt);
}

public static class InventoryMapping
{
    public static void ConfigureInventory(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockLocation>(builder =>
        {
            builder.ToTable("stock_locations", "inventory");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.Code).HasMaxLength(60).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
            builder.Property(x => x.Type).HasMaxLength(40).IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.Code }).IsUnique();
        });

        modelBuilder.Entity<StockBalance>(builder =>
        {
            builder.ToTable("stock_balances", "inventory");
            builder.ConfigureTenantEntity();
            builder.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId);
            builder.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId);
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.ProductId, x.LocationId }).IsUnique();
        });

        modelBuilder.Entity<StockMovement>(builder =>
        {
            builder.ToTable("stock_movements", "inventory");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.Type).HasMaxLength(40).IsRequired();
            builder.Property(x => x.Reference).HasMaxLength(160).IsRequired();
            builder.Property(x => x.Reason).HasMaxLength(300).IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.ProductId, x.CreatedAt });
        });

        modelBuilder.Entity<StockReservation>(builder =>
        {
            builder.ToTable("stock_reservations", "inventory");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.SourceType).HasMaxLength(60).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.SourceDocumentId, x.Status });
        });

        modelBuilder.Entity<PickList>(builder =>
        {
            builder.ToTable("pick_lists", "inventory");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.SourceType).HasMaxLength(60).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
            builder.Property(x => x.LinesJson).HasColumnType("jsonb").IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.SourceDocumentId });
        });
    }
}
