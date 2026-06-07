using Atlas.Api.Common;
using Atlas.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api.Integrations;

public sealed class IntegrationsModule : IEndpointModule
{
    public string Name => "Integrations";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/integrations")
            .WithTags(Name)
            .RequireAuthorization();

        group.MapGet("/adapters", async Task<Ok<IReadOnlyCollection<IntegrationAdapterSummary>>> (
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            
            var adapters = await db.IntegrationAdapters
                .AsNoTracking()
                .Where(x => x.CompanyId == tenant.CompanyId)
                .OrderBy(x => x.Type)
                .ThenBy(x => x.Name)
                .Select(x => IntegrationAdapterSummary.From(x))
                .ToListAsync(ct);
            return TypedResults.Ok(adapters);
        });

        group.MapPost("/adapters", async Task<Created<IntegrationAdapterSummary>> (
            RegisterIntegrationAdapterRequest request,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var adapter = new IntegrationAdapter
            {
                CompanyId = tenant.CompanyId,
                BranchId = tenant.BranchId,
                Type = request.Type.Trim().ToLowerInvariant(),
                Name = request.Name.Trim().ToLowerInvariant(),
                Enabled = request.Enabled,
                ConfigurationJson = request.ConfigurationJson
            };

            db.IntegrationAdapters.Add(adapter);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/integrations/adapters/{adapter.Id}", IntegrationAdapterSummary.From(adapter));
        });

        group.MapPost("/aftermarket/catalog/search", async Task<Ok<ExternalCatalogSearchResult>> (
            ExternalCatalogSearchRequest request,
            AftermarketCatalogGateway gateway,
            CancellationToken ct) =>
        {
            var result = await gateway.SearchAsync(request, ct);
            return TypedResults.Ok(result);
        });
    }
}

public sealed class AftermarketCatalogGateway(IEnumerable<IAftermarketCatalogAdapter> adapters)
{
    public async Task<ExternalCatalogSearchResult> SearchAsync(ExternalCatalogSearchRequest request, CancellationToken ct)
    {
        var adapter = adapters.FirstOrDefault(x => string.Equals(x.Name, request.Adapter, StringComparison.OrdinalIgnoreCase))
                      ?? adapters.First(x => x.Name == "null");

        return await adapter.SearchAsync(request, ct);
    }
}

public interface IAftermarketCatalogAdapter
{
    string Name { get; }
    Task<ExternalCatalogSearchResult> SearchAsync(ExternalCatalogSearchRequest request, CancellationToken ct);
}

public sealed class NullAftermarketCatalogAdapter : IAftermarketCatalogAdapter
{
    public string Name => "null";

    public Task<ExternalCatalogSearchResult> SearchAsync(ExternalCatalogSearchRequest request, CancellationToken ct)
        => Task.FromResult(new ExternalCatalogSearchResult(request.Adapter, []));
}

public sealed class IntegrationAdapter : TenantEntity
{
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Enabled { get; set; }
    public string ConfigurationJson { get; set; } = "{}";
}

public sealed record RegisterIntegrationAdapterRequest(string Type, string Name, bool Enabled, string ConfigurationJson);
public sealed record IntegrationAdapterSummary(Guid Id, string Type, string Name, bool Enabled)
{
    public static IntegrationAdapterSummary From(IntegrationAdapter adapter) => new(adapter.Id, adapter.Type, adapter.Name, adapter.Enabled);
}

public sealed record ExternalCatalogSearchRequest(string Adapter, string? Code, string? Vin, string? Make, string? Model, int? Year, string? Engine);
public sealed record ExternalCatalogSearchResult(string Adapter, IReadOnlyCollection<ExternalCatalogPart> Parts);
public sealed record ExternalCatalogPart(string ExternalId, string Code, string Brand, string Description, string FitmentJson);

public static class IntegrationsMapping
{
    public static void ConfigureIntegrations(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IntegrationAdapter>(builder =>
        {
            builder.ToTable("integration_adapters", "integrations");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.Type).HasMaxLength(80).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
            builder.Property(x => x.ConfigurationJson).HasColumnType("jsonb").IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.Type, x.Name }).IsUnique();
        });

        modelBuilder.ConfigureOutbox();
    }
}
