using System.Text.Json;
using Atlas.Api.Common;
using Atlas.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api.Crm;

public sealed class CrmModule : IEndpointModule
{
    public string Name => "CRM";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/crm")
            .WithTags(Name)
            .RequireAuthorization();

        group.MapPost("/customers", async Task<Created<CustomerSummary>> (
            CreateCustomerRequest request,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var customer = Customer.Create(request, tenant.CompanyId, tenant.BranchId);
            db.Customers.Add(customer);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/crm/customers/{customer.Id}", CustomerSummary.From(customer));
        });

        group.MapGet("/customers", async Task<Ok<IReadOnlyCollection<CustomerSummary>>> (
            string? term,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var normalized = FiscalStrings.NormalizeCode(term);
            var query = db.Customers.AsNoTracking().Where(x => x.CompanyId == tenant.CompanyId);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                query = query.Where(x => x.Name.ToUpper().Contains(normalized)
                                         || x.Document.ToUpper().Contains(normalized)
                                         || x.Phone.ToUpper().Contains(normalized)
                                         || x.Email.ToUpper().Contains(normalized)
                                         || x.PrimaryPlate.ToUpper().Contains(normalized));
            }

            var customers = await query
                .OrderBy(x => x.Name)
                .Take(80)
                .Select(x => CustomerSummary.From(x))
                .ToListAsync(ct);
            return TypedResults.Ok(customers);
        });

        group.MapPost("/customers/{id:guid}/interactions", async Task<Results<Ok<CustomerSummary>, NotFound>> (
            Guid id,
            AddCustomerInteractionRequest request,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var customer = await db.Customers.FirstOrDefaultAsync(x => x.CompanyId == tenant.CompanyId && x.Id == id, ct);
            if (customer is null)
            {
                return TypedResults.NotFound();
            }

            customer.AddInteraction(request);
            await db.SaveChangesAsync(ct);
            return TypedResults.Ok(CustomerSummary.From(customer));
        });
    }
}

public sealed class Customer : TenantEntity, IAggregateRoot
{
    public string Type { get; set; } = "individual";
    public string Name { get; set; } = "";
    public string Document { get; set; } = "";
    public string? StateRegistration { get; set; }
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string PrimaryPlate { get; set; } = "";
    public string PrimaryChassis { get; set; } = "";
    public string Segment { get; set; } = "";
    public string InteractionsJson { get; set; } = "[]";

    public static Customer Create(CreateCustomerRequest request, Guid companyId, Guid branchId)
        => new()
        {
            CompanyId = companyId,
            BranchId = branchId,
            Type = request.Type.Trim().ToLowerInvariant(),
            Name = request.Name.Trim(),
            Document = request.Document.Trim(),
            StateRegistration = request.StateRegistration?.Trim(),
            Email = request.Email?.Trim() ?? "",
            Phone = request.Phone?.Trim() ?? "",
            PrimaryPlate = request.PrimaryPlate?.Trim().ToUpperInvariant() ?? "",
            PrimaryChassis = request.PrimaryChassis?.Trim().ToUpperInvariant() ?? "",
            Segment = request.Segment?.Trim() ?? ""
        };

    public void AddInteraction(AddCustomerInteractionRequest request)
    {
        var entry = new CustomerInteraction(DateTimeOffset.UtcNow, request.Channel, request.Subject, request.Notes, request.NextFollowUpAt);
        var interactions = JsonSerializer.Deserialize<List<CustomerInteraction>>(InteractionsJson, JsonPayload.Options) ?? [];
        interactions.Add(entry);
        InteractionsJson = JsonPayload.Serialize(interactions);
    }
}

public sealed record CustomerInteraction(DateTimeOffset OccurredAt, string Channel, string Subject, string Notes, DateTimeOffset? NextFollowUpAt);
public sealed record CreateCustomerRequest(string Type, string Name, string Document, string? StateRegistration, string? Email, string? Phone, string? PrimaryPlate, string? PrimaryChassis, string? Segment);
public sealed record AddCustomerInteractionRequest(string Channel, string Subject, string Notes, DateTimeOffset? NextFollowUpAt);
public sealed record CustomerSummary(Guid Id, string Type, string Name, string Document, string Email, string Phone, string PrimaryPlate)
{
    public static CustomerSummary From(Customer customer) => new(customer.Id, customer.Type, customer.Name, customer.Document, customer.Email, customer.Phone, customer.PrimaryPlate);
}

public static class CrmMapping
{
    public static void ConfigureCrm(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(builder =>
        {
            builder.ToTable("customers", "crm");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.Type).HasMaxLength(40).IsRequired();
            builder.Property(x => x.Name).HasMaxLength(220).IsRequired();
            builder.Property(x => x.Document).HasMaxLength(32).IsRequired();
            builder.Property(x => x.StateRegistration).HasMaxLength(32);
            builder.Property(x => x.Email).HasMaxLength(180).IsRequired();
            builder.Property(x => x.Phone).HasMaxLength(40).IsRequired();
            builder.Property(x => x.PrimaryPlate).HasMaxLength(16).IsRequired();
            builder.Property(x => x.PrimaryChassis).HasMaxLength(40).IsRequired();
            builder.Property(x => x.Segment).HasMaxLength(80).IsRequired();
            builder.Property(x => x.InteractionsJson).HasColumnType("jsonb").IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.Document });
            builder.HasIndex(x => new { x.CompanyId, x.PrimaryPlate });
            builder.HasIndex(x => new { x.CompanyId, x.PrimaryChassis });
        });
    }
}
