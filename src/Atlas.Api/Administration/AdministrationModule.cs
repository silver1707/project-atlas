using Atlas.Api.Common;
using Atlas.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api.Administration;

public sealed class AdministrationModule : IEndpointModule
{
    public string Name => "Administration";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin")
            .WithTags(Name)
            .RequireAuthorization(AuthorizationPolicies.Administration);

        group.MapGet("/companies", async Task<Ok<List<CompanySummary>>> (AtlasDbContext db, CancellationToken ct) =>
        {
            var companies = await db.Companies
                .AsNoTracking()
                .OrderBy(x => x.TradeName)
                .Select(x => new CompanySummary(x.Id, x.LegalName, x.TradeName, x.Cnpj, x.TaxRegime))
                .ToListAsync(ct);
            return TypedResults.Ok(companies);
        });

        group.MapPost("/companies", async Task<Created<CompanySummary>> (CreateCompanyRequest request, AtlasDbContext db, CancellationToken ct) =>
        {
            var company = new Company
            {
                LegalName = request.LegalName.Trim(),
                TradeName = request.TradeName.Trim(),
                Cnpj = request.Cnpj.Trim(),
                StateRegistration = request.StateRegistration?.Trim(),
                TaxRegime = request.TaxRegime,
                FiscalState = request.FiscalState.Trim().ToUpperInvariant()
            };

            db.Companies.Add(company);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/admin/companies/{company.Id}", new CompanySummary(company.Id, company.LegalName, company.TradeName, company.Cnpj, company.TaxRegime));
        });

        group.MapPost("/branches", async Task<Created<BranchSummary>> (CreateBranchRequest request, AtlasDbContext db, CancellationToken ct) =>
        {
            var branch = new Branch
            {
                CompanyId = request.CompanyId,
                BranchId = Guid.Empty,
                Name = request.Name.Trim(),
                Cnpj = request.Cnpj.Trim(),
                StateRegistration = request.StateRegistration?.Trim(),
                FiscalState = request.FiscalState.Trim().ToUpperInvariant(),
                WarehouseCode = request.WarehouseCode.Trim().ToUpperInvariant()
            };

            db.Branches.Add(branch);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/admin/branches/{branch.Id}", new BranchSummary(branch.Id, branch.CompanyId, branch.Name, branch.Cnpj, branch.FiscalState));
        });
    }
}

public sealed class Company : Entity, IAggregateRoot
{
    public string LegalName { get; set; } = "";
    public string TradeName { get; set; } = "";
    public string Cnpj { get; set; } = "";
    public string? StateRegistration { get; set; }
    public string FiscalState { get; set; } = "";
    public string TaxRegime { get; set; } = "simple_national";
    public List<Branch> Branches { get; set; } = [];
}

public sealed class Branch : TenantEntity, IAggregateRoot
{
    public string Name { get; set; } = "";
    public string Cnpj { get; set; } = "";
    public string? StateRegistration { get; set; }
    public string FiscalState { get; set; } = "";
    public string WarehouseCode { get; set; } = "";
}

public sealed class ModulePermission : TenantEntity
{
    public string RoleName { get; set; } = "";
    public string Module { get; set; } = "";
    public string Permission { get; set; } = "";
    public bool RequiresMfa { get; set; }
}

public sealed record CreateCompanyRequest(string LegalName, string TradeName, string Cnpj, string? StateRegistration, string FiscalState, string TaxRegime);
public sealed record CreateBranchRequest(Guid CompanyId, string Name, string Cnpj, string? StateRegistration, string FiscalState, string WarehouseCode);
public sealed record CompanySummary(Guid Id, string LegalName, string TradeName, string Cnpj, string TaxRegime);
public sealed record BranchSummary(Guid Id, Guid CompanyId, string Name, string Cnpj, string FiscalState);

public static class AdministrationMapping
{
    public static void ConfigureAdministration(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(builder =>
        {
            builder.ToTable("companies", "administration");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.LegalName).HasMaxLength(220).IsRequired();
            builder.Property(x => x.TradeName).HasMaxLength(180).IsRequired();
            builder.Property(x => x.Cnpj).HasMaxLength(32).IsRequired();
            builder.Property(x => x.StateRegistration).HasMaxLength(32);
            builder.Property(x => x.FiscalState).HasMaxLength(2).IsRequired();
            builder.Property(x => x.TaxRegime).HasMaxLength(40).IsRequired();
            builder.HasIndex(x => x.Cnpj).IsUnique();
        });

        modelBuilder.Entity<Branch>(builder =>
        {
            builder.ToTable("branches", "administration");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.Name).HasMaxLength(180).IsRequired();
            builder.Property(x => x.Cnpj).HasMaxLength(32).IsRequired();
            builder.Property(x => x.StateRegistration).HasMaxLength(32);
            builder.Property(x => x.FiscalState).HasMaxLength(2).IsRequired();
            builder.Property(x => x.WarehouseCode).HasMaxLength(40).IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.Cnpj }).IsUnique();
        });

        modelBuilder.Entity<ModulePermission>(builder =>
        {
            builder.ToTable("module_permissions", "identity");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.RoleName).HasMaxLength(120).IsRequired();
            builder.Property(x => x.Module).HasMaxLength(80).IsRequired();
            builder.Property(x => x.Permission).HasMaxLength(120).IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.RoleName, x.Module, x.Permission }).IsUnique();
        });
    }
}
