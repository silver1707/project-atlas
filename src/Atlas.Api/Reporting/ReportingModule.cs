using Atlas.Api.Common;
using Atlas.Api.Fiscal;
using Atlas.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api.Reporting;

public sealed class ReportingModule : IEndpointModule
{
    public string Name => "Reports";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/reports")
            .WithTags(Name)
            .RequireAuthorization();

        group.MapGet("/operations-dashboard", async Task<Ok<OperationsDashboard>> (
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var today = DateTimeOffset.UtcNow.Date;
            var todayStart = new DateTimeOffset(today, TimeSpan.Zero);
            var sales = await db.SalesOrders.AsNoTracking()
                .Where(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && x.CreatedAt >= todayStart)
                .ToListAsync(ct);
            var fiscal = await db.FiscalDocuments.AsNoTracking()
                .Where(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && x.IssuedAt >= todayStart)
                .ToListAsync(ct);
            var lowStock = await db.StockBalances.AsNoTracking()
                .CountAsync(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && x.AvailableQuantity < x.MinimumQuantity, ct);
            var picking = await db.PickLists.AsNoTracking()
                .CountAsync(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && x.Status == "pending", ct);

            return TypedResults.Ok(new OperationsDashboard(
                sales.Count,
                sales.Sum(x => x.Total),
                fiscal.Count(x => x.Status == FiscalDocumentStatus.Authorized),
                fiscal.Count(x => x.Status == FiscalDocumentStatus.Rejected),
                lowStock,
                picking));
        });

        group.MapGet("/sales-by-day", async Task<Ok<IReadOnlyCollection<SalesByDay>>> (
            DateOnly from,
            DateOnly to,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var end = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            var rows = await db.SalesOrders.AsNoTracking()
                .Where(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && x.CreatedAt >= start && x.CreatedAt <= end)
                .GroupBy(x => x.CreatedAt.Date)
                .Select(x => new SalesByDay(DateOnly.FromDateTime(x.Key), x.Count(), x.Sum(order => order.Total)))
                .OrderBy(x => x.Date)
                .ToListAsync(ct);
            return TypedResults.Ok(rows);
        });
    }
}

public sealed record OperationsDashboard(int SalesCountToday, decimal SalesAmountToday, int AuthorizedFiscalDocuments, int RejectedFiscalDocuments, int LowStockItems, int PendingPickLists);
public sealed record SalesByDay(DateOnly Date, int Orders, decimal Amount);
