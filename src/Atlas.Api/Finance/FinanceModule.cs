using Atlas.Api.Common;
using Atlas.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api.Finance;

public sealed class FinanceModule : IEndpointModule
{
    public string Name => "Finance";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/finance")
            .WithTags(Name)
            .RequireAuthorization(AuthorizationPolicies.SensitiveFinance);

        group.MapPost("/payables", async Task<Created<AccountPayableSummary>> (
            CreateAccountPayableRequest request,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var payable = AccountPayable.Create(request, tenant.CompanyId, tenant.BranchId);
            db.AccountsPayable.Add(payable);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/finance/payables/{payable.Id}", AccountPayableSummary.From(payable));
        });

        group.MapPost("/receivables", async Task<Created<AccountReceivableSummary>> (
            CreateAccountReceivableRequest request,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var receivable = AccountReceivable.Create(request, tenant.CompanyId, tenant.BranchId);
            db.AccountsReceivable.Add(receivable);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/finance/receivables/{receivable.Id}", AccountReceivableSummary.From(receivable));
        });

        group.MapPost("/cash-ledger", async Task<Created<CashLedgerEntrySummary>> (
            CreateCashLedgerEntryRequest request,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var entry = CashLedgerEntry.Create(request, tenant.CompanyId, tenant.BranchId);
            db.CashLedgerEntries.Add(entry);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/finance/cash-ledger/{entry.Id}", CashLedgerEntrySummary.From(entry));
        });

        group.MapPost("/reconciliation", async Task<Ok<ReconciliationResult>> (
            ReconcileBankStatementRequest request,
            FinanceReconciliationService reconciliation,
            CancellationToken ct) =>
        {
            var result = await reconciliation.ReconcileAsync(request, ct);
            return TypedResults.Ok(result);
        });

        group.MapGet("/dashboard", async Task<Ok<FinanceDashboard>> (
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var payables = await db.AccountsPayable.AsNoTracking()
                .Where(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && x.Status == FinancialTitleStatus.Open)
                .ToListAsync(ct);
            var receivables = await db.AccountsReceivable.AsNoTracking()
                .Where(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && x.Status == FinancialTitleStatus.Open)
                .ToListAsync(ct);

            return TypedResults.Ok(new FinanceDashboard(
                payables.Where(x => x.DueOn < today).Sum(x => x.Amount),
                receivables.Where(x => x.DueOn < today).Sum(x => x.Amount),
                payables.Sum(x => x.Amount),
                receivables.Sum(x => x.Amount)));
        });
    }
}

public sealed class FinanceReconciliationService(AtlasDbContext db, ITenantContext tenant)
{
    public async Task<ReconciliationResult> ReconcileAsync(ReconcileBankStatementRequest request, CancellationToken ct)
    {
        var openReceivables = await db.AccountsReceivable
            .Where(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && x.Status == FinancialTitleStatus.Open)
            .ToListAsync(ct);

        var matches = new List<ReconciliationMatch>();
        foreach (var line in request.Lines)
        {
            var match = openReceivables.FirstOrDefault(x =>
                x.Amount == line.Amount
                && Math.Abs(x.DueOn.DayNumber - line.Date.DayNumber) <= 3
                && (string.IsNullOrWhiteSpace(line.Document) || x.DocumentNumber == line.Document));

            if (match is null)
            {
                continue;
            }

            match.Status = FinancialTitleStatus.Settled;
            match.SettledAt = DateTimeOffset.UtcNow;
            matches.Add(new ReconciliationMatch(line.Date, line.Amount, line.Document, match.Id));
        }

        await db.SaveChangesAsync(ct);
        return new ReconciliationResult(matches);
    }
}

public sealed class AccountPayable : TenantEntity, IAggregateRoot
{
    public Guid? SupplierId { get; set; }
    public string SupplierSnapshot { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public DateOnly DueOn { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = FinancialTitleStatus.Open;
    public DateTimeOffset? SettledAt { get; set; }

    public static AccountPayable Create(CreateAccountPayableRequest request, Guid companyId, Guid branchId)
        => new()
        {
            CompanyId = companyId,
            BranchId = branchId,
            SupplierId = request.SupplierId,
            SupplierSnapshot = request.SupplierSnapshot.Trim(),
            DocumentNumber = request.DocumentNumber.Trim(),
            DueOn = request.DueOn,
            Amount = request.Amount
        };
}

public sealed class AccountReceivable : TenantEntity, IAggregateRoot
{
    public Guid? CustomerId { get; set; }
    public string CustomerSnapshot { get; set; } = "";
    public string DocumentNumber { get; set; } = "";
    public DateOnly DueOn { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = FinancialTitleStatus.Open;
    public DateTimeOffset? SettledAt { get; set; }

    public static AccountReceivable Create(CreateAccountReceivableRequest request, Guid companyId, Guid branchId)
        => new()
        {
            CompanyId = companyId,
            BranchId = branchId,
            CustomerId = request.CustomerId,
            CustomerSnapshot = request.CustomerSnapshot.Trim(),
            DocumentNumber = request.DocumentNumber.Trim(),
            DueOn = request.DueOn,
            Amount = request.Amount
        };
}

public sealed class CashLedgerEntry : TenantEntity, IAggregateRoot
{
    public string AccountCode { get; set; } = "";
    public string Type { get; set; } = "";
    public DateOnly PostedOn { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = "";
    public Guid? SourceDocumentId { get; set; }

    public static CashLedgerEntry Create(CreateCashLedgerEntryRequest request, Guid companyId, Guid branchId)
        => new()
        {
            CompanyId = companyId,
            BranchId = branchId,
            AccountCode = request.AccountCode.Trim(),
            Type = request.Type.Trim().ToLowerInvariant(),
            PostedOn = request.PostedOn,
            Amount = request.Amount,
            Description = request.Description.Trim(),
            SourceDocumentId = request.SourceDocumentId
        };
}

public static class FinancialTitleStatus
{
    public const string Open = "open";
    public const string Settled = "settled";
    public const string Cancelled = "cancelled";
}

public sealed record CreateAccountPayableRequest(Guid? SupplierId, string SupplierSnapshot, string DocumentNumber, DateOnly DueOn, decimal Amount);
public sealed record CreateAccountReceivableRequest(Guid? CustomerId, string CustomerSnapshot, string DocumentNumber, DateOnly DueOn, decimal Amount);
public sealed record CreateCashLedgerEntryRequest(string AccountCode, string Type, DateOnly PostedOn, decimal Amount, string Description, Guid? SourceDocumentId);
public sealed record ReconcileBankStatementRequest(IReadOnlyCollection<BankStatementLine> Lines);
public sealed record BankStatementLine(DateOnly Date, decimal Amount, string? Document, string Description);
public sealed record ReconciliationResult(IReadOnlyCollection<ReconciliationMatch> Matches);
public sealed record ReconciliationMatch(DateOnly Date, decimal Amount, string? Document, Guid ReceivableId);
public sealed record FinanceDashboard(decimal OverduePayables, decimal OverdueReceivables, decimal OpenPayables, decimal OpenReceivables);

public sealed record AccountPayableSummary(Guid Id, string Supplier, string DocumentNumber, DateOnly DueOn, decimal Amount, string Status)
{
    public static AccountPayableSummary From(AccountPayable payable) => new(payable.Id, payable.SupplierSnapshot, payable.DocumentNumber, payable.DueOn, payable.Amount, payable.Status);
}

public sealed record AccountReceivableSummary(Guid Id, string Customer, string DocumentNumber, DateOnly DueOn, decimal Amount, string Status)
{
    public static AccountReceivableSummary From(AccountReceivable receivable) => new(receivable.Id, receivable.CustomerSnapshot, receivable.DocumentNumber, receivable.DueOn, receivable.Amount, receivable.Status);
}

public sealed record CashLedgerEntrySummary(Guid Id, string AccountCode, string Type, DateOnly PostedOn, decimal Amount, string Description)
{
    public static CashLedgerEntrySummary From(CashLedgerEntry entry) => new(entry.Id, entry.AccountCode, entry.Type, entry.PostedOn, entry.Amount, entry.Description);
}

public static class FinanceMapping
{
    public static void ConfigureFinance(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountPayable>(builder =>
        {
            builder.ToTable("accounts_payable", "finance");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.SupplierSnapshot).HasMaxLength(220).IsRequired();
            builder.Property(x => x.DocumentNumber).HasMaxLength(80).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.Status, x.DueOn });
        });

        modelBuilder.Entity<AccountReceivable>(builder =>
        {
            builder.ToTable("accounts_receivable", "finance");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.CustomerSnapshot).HasMaxLength(220).IsRequired();
            builder.Property(x => x.DocumentNumber).HasMaxLength(80).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.Status, x.DueOn });
        });

        modelBuilder.Entity<CashLedgerEntry>(builder =>
        {
            builder.ToTable("cash_ledger_entries", "finance");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.AccountCode).HasMaxLength(40).IsRequired();
            builder.Property(x => x.Type).HasMaxLength(40).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(300).IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.PostedOn, x.AccountCode });
        });
    }
}
