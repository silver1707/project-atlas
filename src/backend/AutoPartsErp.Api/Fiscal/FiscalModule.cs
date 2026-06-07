using System.Xml.Linq;
using Atlas.Api.Common;
using Atlas.Api.Infrastructure;
using Atlas.Api.Sales;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api.Fiscal;

public sealed class FiscalModule : IEndpointModule
{
    public string Name => "Fiscal";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/fiscal")
            .WithTags(Name)
            .RequireAuthorization(AuthorizationPolicies.SensitiveFiscal);

        group.MapPost("/rules", async Task<Created<FiscalRuleSummary>> (
            UpsertFiscalRuleRequest request,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var rule = FiscalRule.From(request, tenant.CompanyId, tenant.BranchId);
            db.FiscalRules.Add(rule);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/fiscal/rules/{rule.Id}", FiscalRuleSummary.From(rule));
        });

        group.MapPost("/documents/issue", async Task<Created<FiscalDocumentSummary>> (
            IssueFiscalDocumentRequest request,
            FiscalDocumentService fiscal,
            CancellationToken ct) =>
        {
            var document = await fiscal.IssueAsync(request, ct);
            return TypedResults.Created($"/api/fiscal/documents/{document.Id}", FiscalDocumentSummary.From(document));
        });

        group.MapPost("/documents/{id:guid}/cancel", async Task<Results<Ok<FiscalDocumentSummary>, NotFound>> (
            Guid id,
            CancelFiscalDocumentRequest request,
            FiscalDocumentService fiscal,
            CancellationToken ct) =>
        {
            var document = await fiscal.CancelAsync(id, request, ct);
            return document is null ? TypedResults.NotFound() : TypedResults.Ok(FiscalDocumentSummary.From(document));
        });

        group.MapPost("/documents/{id:guid}/query", async Task<Results<Ok<FiscalDocumentSummary>, NotFound>> (
            Guid id,
            FiscalDocumentService fiscal,
            CancellationToken ct) =>
        {
            var document = await fiscal.QueryAsync(id, ct);
            return document is null ? TypedResults.NotFound() : TypedResults.Ok(FiscalDocumentSummary.From(document));
        });

        group.MapPost("/documents/inutilization", async Task<Ok<ProviderOperationResult>> (
            InutilizeNumberRequest request,
            FiscalProviderRouter providers,
            CancellationToken ct) =>
        {
            var result = await providers.For(request.Provider).InutilizeAsync(request, ct);
            return TypedResults.Ok(result);
        });

        group.MapPost("/dfe/distribution", async Task<Ok<DfeDistributionResult>> (
            DfeDistributionRequest request,
            FiscalDocumentService fiscal,
            CancellationToken ct) =>
        {
            var result = await fiscal.DistributeDfeAsync(request, ct);
            return TypedResults.Ok(result);
        });

        group.MapPost("/dfe/manifestation", async Task<Ok<ProviderOperationResult>> (
            RecipientManifestationRequest request,
            FiscalProviderRouter providers,
            CancellationToken ct) =>
        {
            var result = await providers.For(request.Provider).ManifestRecipientAsync(request, ct);
            return TypedResults.Ok(result);
        });

        group.MapPost("/imports/xml", async Task<Ok<InboundXmlImportResult>> (
            ImportInboundXmlRequest request,
            FiscalDocumentService fiscal,
            CancellationToken ct) =>
        {
            var result = await fiscal.ImportInboundXmlAsync(request, ct);
            return TypedResults.Ok(result);
        });

        group.MapGet("/documents", async Task<Ok<IReadOnlyCollection<FiscalDocumentSummary>>> (
            string? status,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var query = db.FiscalDocuments.AsNoTracking().Where(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId);
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            var docs = await query
                .OrderByDescending(x => x.IssuedAt)
                .Take(100)
                .Select(x => FiscalDocumentSummary.From(x))
                .ToListAsync(ct);
            return TypedResults.Ok(docs);
        });
    }
}

public sealed class FiscalDocumentService(
    AtlasDbContext db,
    ITenantContext tenant,
    TaxEngine taxEngine,
    FiscalProviderRouter providers,
    ImmutableXmlStore xmlStore)
{
    public async Task<FiscalDocument> IssueAsync(IssueFiscalDocumentRequest request, CancellationToken ct)
    {
        var order = await db.SalesOrders
            .Include(x => x.Lines)
            .FirstAsync(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && x.Id == request.SalesOrderId, ct);

        var taxContext = FiscalOperationContext.From(order, request);
        var taxes = await taxEngine.CalculateAsync(taxContext, ct);
        var xmlDraft = FiscalXmlDraftBuilder.Build(order, request, taxes);
        var stored = await xmlStore.StoreAsync(tenant.CompanyId, request.Model, xmlDraft, ct);

        var document = FiscalDocument.CreateFromSalesOrder(order, request, taxes, stored);
        db.FiscalDocuments.Add(document);
        await db.SaveChangesAsync(ct);

        var response = await providers.For(request.Provider).AuthorizeAsync(document, xmlDraft, ct);
        document.RegisterProviderResponse(response);
        await db.SaveChangesAsync(ct);
        return document;
    }

    public async Task<FiscalDocument?> CancelAsync(Guid id, CancelFiscalDocumentRequest request, CancellationToken ct)
    {
        var document = await db.FiscalDocuments.Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && x.Id == id, ct);
        if (document is null)
        {
            return null;
        }

        var response = await providers.For(request.Provider).CancelAsync(document, request, ct);
        document.RegisterEvent("cancelamento", response.Status, response.Protocol, request.Reason, response.RawPayload);
        document.Status = response.Success ? FiscalDocumentStatus.Cancelled : document.Status;
        await db.SaveChangesAsync(ct);
        return document;
    }

    public async Task<FiscalDocument?> QueryAsync(Guid id, CancellationToken ct)
    {
        var document = await db.FiscalDocuments.Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && x.Id == id, ct);
        if (document is null)
        {
            return null;
        }

        var response = await providers.For(document.Provider).QueryAsync(document, ct);
        document.RegisterEvent("consulta", response.Status, response.Protocol, response.Message, response.RawPayload);
        if (response.Success)
        {
            document.Status = FiscalDocumentStatus.Authorized;
        }

        await db.SaveChangesAsync(ct);
        return document;
    }

    public async Task<DfeDistributionResult> DistributeDfeAsync(DfeDistributionRequest request, CancellationToken ct)
    {
        var response = await providers.For(request.Provider).DistributeDfeAsync(request, ct);
        var cursor = await db.DfeDistributionCursors.FirstOrDefaultAsync(x =>
            x.CompanyId == tenant.CompanyId
            && x.BranchId == tenant.BranchId
            && x.Cnpj == request.Cnpj
            && x.Environment == request.Environment, ct);

        if (cursor is null)
        {
            cursor = new DfeDistributionCursor
            {
                CompanyId = tenant.CompanyId,
                BranchId = tenant.BranchId,
                Cnpj = request.Cnpj,
                Environment = request.Environment
            };
            db.DfeDistributionCursors.Add(cursor);
        }

        cursor.LastNsu = response.LastNsu;
        cursor.MaxNsu = response.MaxNsu;
        cursor.LastRunAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return response;
    }

    public async Task<InboundXmlImportResult> ImportInboundXmlAsync(ImportInboundXmlRequest request, CancellationToken ct)
    {
        var normalizedXml = request.Xml.Trim();
        var xdoc = XDocument.Parse(normalizedXml, LoadOptions.PreserveWhitespace);
        var accessKey = xdoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "chNFe")?.Value
                        ?? xdoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "infNFe")?.Attribute("Id")?.Value?.Replace("NFe", "", StringComparison.Ordinal)
                        ?? FiscalStrings.Sha256Hex(normalizedXml);

        var stored = await xmlStore.StoreAsync(tenant.CompanyId, "incoming", normalizedXml, ct);
        var document = new FiscalDocument
        {
            CompanyId = tenant.CompanyId,
            BranchId = tenant.BranchId,
            Model = "55",
            Kind = FiscalDocumentKind.Inbound,
            Status = FiscalDocumentStatus.Imported,
            Provider = request.Provider,
            AccessKey = accessKey,
            Number = xdoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "nNF")?.Value ?? "",
            Series = xdoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "serie")?.Value ?? "",
            IssuedAt = DateTimeOffset.UtcNow,
            XmlHash = stored.Hash,
            XmlStorageUri = stored.Uri,
            PayloadSnapshot = JsonPayload.Serialize(new { source = "xml_import", request.PurchaseOrderId })
        };
        document.RegisterEvent("importacao_xml_entrada", "imported", null, "XML de entrada importado para conciliacao de compras.", "{}");
        db.FiscalDocuments.Add(document);
        await db.SaveChangesAsync(ct);
        return new InboundXmlImportResult(document.Id, document.AccessKey, stored.Hash, stored.Uri);
    }
}

public sealed class TaxEngine(AtlasDbContext db, ITenantContext tenant)
{
    public async Task<TaxCalculationResult> CalculateAsync(FiscalOperationContext context, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rules = await db.FiscalRules.AsNoTracking()
            .Where(x => x.CompanyId == tenant.CompanyId
                        && x.Active
                        && x.ValidFrom <= today
                        && (x.ValidTo == null || x.ValidTo >= today)
                        && x.SourceUf == context.SourceUf
                        && x.DestinationUf == context.DestinationUf
                        && x.Regime == context.Regime
                        && x.OperationType == context.OperationType)
            .ToListAsync(ct);

        var itemTaxes = context.Items.Select(item =>
        {
            var rule = rules
                .Where(x => string.IsNullOrWhiteSpace(x.Ncm) || x.Ncm == item.Ncm)
                .Where(x => string.IsNullOrWhiteSpace(x.Cest) || x.Cest == item.Cest)
                .OrderByDescending(x => SpecificityScore(x))
                .FirstOrDefault();

            if (rule is null)
            {
                throw new InvalidOperationException($"No fiscal rule for NCM {item.Ncm}, CEST {item.Cest}, UF {context.SourceUf}->{context.DestinationUf}, regime {context.Regime}.");
            }

            var baseAmount = item.Total;
            return new TaxItemCalculation(
                item.LineId,
                rule.Cfop,
                rule.Cst,
                rule.Csosn,
                rule.Origin,
                baseAmount,
                Math.Round(baseAmount * rule.IcmsRate / 100, 2),
                Math.Round(baseAmount * rule.IcmsStRate / 100, 2),
                Math.Round(baseAmount * rule.PisRate / 100, 2),
                Math.Round(baseAmount * rule.CofinsRate / 100, 2),
                Math.Round(baseAmount * rule.IbsRate / 100, 2),
                Math.Round(baseAmount * rule.CbsRate / 100, 2),
                rule.BenefitCode,
                rule.LegalBasis);
        }).ToArray();

        return new TaxCalculationResult(
            itemTaxes,
            itemTaxes.Sum(x => x.Icms),
            itemTaxes.Sum(x => x.IcmsSt),
            itemTaxes.Sum(x => x.Pis),
            itemTaxes.Sum(x => x.Cofins),
            itemTaxes.Sum(x => x.Ibs),
            itemTaxes.Sum(x => x.Cbs));
    }

    private static int SpecificityScore(FiscalRule rule)
        => (string.IsNullOrWhiteSpace(rule.Ncm) ? 0 : 10)
           + (string.IsNullOrWhiteSpace(rule.Cest) ? 0 : 5)
           + (string.IsNullOrWhiteSpace(rule.ExceptionCode) ? 0 : 3);
}

public sealed class ImmutableXmlStore(AtlasOptions options)
{
    public async Task<StoredXml> StoreAsync(Guid companyId, string model, string xml, CancellationToken ct)
    {
        var hash = FiscalStrings.Sha256Hex(xml);
        var directory = Path.Combine(options.ImmutableXmlStoragePath, companyId.ToString("D"), model, DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{hash}.xml");
        if (!File.Exists(path))
        {
            await File.WriteAllTextAsync(path, xml, ct);
            File.SetAttributes(path, FileAttributes.ReadOnly);
        }

        return new StoredXml(hash, path);
    }
}

public sealed class FiscalProviderRouter(IEnumerable<IFiscalDocumentProvider> providers, AtlasOptions options)
{
    public IFiscalDocumentProvider For(string? providerName)
    {
        var selected = string.IsNullOrWhiteSpace(providerName) ? options.DefaultFiscalProvider : providerName;
        return providers.FirstOrDefault(x => string.Equals(x.Name, selected, StringComparison.OrdinalIgnoreCase))
               ?? providers.First(x => x.Name == "sandbox");
    }
}

public interface IFiscalDocumentProvider
{
    string Name { get; }
    Task<ProviderOperationResult> AuthorizeAsync(FiscalDocument document, string xml, CancellationToken ct);
    Task<ProviderOperationResult> CancelAsync(FiscalDocument document, CancelFiscalDocumentRequest request, CancellationToken ct);
    Task<ProviderOperationResult> QueryAsync(FiscalDocument document, CancellationToken ct);
    Task<ProviderOperationResult> InutilizeAsync(InutilizeNumberRequest request, CancellationToken ct);
    Task<DfeDistributionResult> DistributeDfeAsync(DfeDistributionRequest request, CancellationToken ct);
    Task<ProviderOperationResult> ManifestRecipientAsync(RecipientManifestationRequest request, CancellationToken ct);
}

public sealed class SandboxFiscalProvider : IFiscalDocumentProvider
{
    public string Name => "sandbox";

    public Task<ProviderOperationResult> AuthorizeAsync(FiscalDocument document, string xml, CancellationToken ct)
        => Task.FromResult(new ProviderOperationResult(true, "authorized", "Autorizado em sandbox; substituir por provider SEFAZ/NFS-e/provedor fiscal em homologacao.", $"SANDBOX-{Guid.CreateVersion7():N}", "{}"));

    public Task<ProviderOperationResult> CancelAsync(FiscalDocument document, CancelFiscalDocumentRequest request, CancellationToken ct)
        => Task.FromResult(new ProviderOperationResult(true, "cancelled", "Cancelado em sandbox.", $"CANCEL-{Guid.CreateVersion7():N}", "{}"));

    public Task<ProviderOperationResult> QueryAsync(FiscalDocument document, CancellationToken ct)
        => Task.FromResult(new ProviderOperationResult(true, "authorized", "Consulta sandbox retornou autorizado.", document.AuthorizationProtocol, "{}"));

    public Task<ProviderOperationResult> InutilizeAsync(InutilizeNumberRequest request, CancellationToken ct)
        => Task.FromResult(new ProviderOperationResult(true, "inutilized", "Numeracao inutilizada em sandbox.", $"INUT-{Guid.CreateVersion7():N}", "{}"));

    public Task<DfeDistributionResult> DistributeDfeAsync(DfeDistributionRequest request, CancellationToken ct)
        => Task.FromResult(new DfeDistributionResult(request.LastNsu, request.LastNsu, []));

    public Task<ProviderOperationResult> ManifestRecipientAsync(RecipientManifestationRequest request, CancellationToken ct)
        => Task.FromResult(new ProviderOperationResult(true, "manifested", "Manifestacao registrada em sandbox.", $"MANIF-{Guid.CreateVersion7():N}", "{}"));
}

public static class FiscalXmlDraftBuilder
{
    public static string Build(SalesOrder order, IssueFiscalDocumentRequest request, TaxCalculationResult taxes)
    {
        var document = new XDocument(
            new XElement("AtlasFiscalDraft",
                new XAttribute("model", request.Model),
                new XAttribute("environment", request.Environment),
                new XElement("SalesOrderId", order.Id),
                new XElement("Customer", order.CustomerNameSnapshot),
                new XElement("Totals",
                    new XElement("Products", order.Total),
                    new XElement("ICMS", taxes.TotalIcms),
                    new XElement("ICMSST", taxes.TotalIcmsSt),
                    new XElement("IBS", taxes.TotalIbs),
                    new XElement("CBS", taxes.TotalCbs)),
                new XElement("Items", order.Lines.Select(line =>
                    new XElement("Item",
                        new XAttribute("id", line.Id),
                        new XElement("Sku", line.SkuSnapshot),
                        new XElement("Description", line.DescriptionSnapshot),
                        new XElement("Ncm", line.NcmSnapshot),
                        new XElement("Cest", line.CestSnapshot ?? ""),
                        new XElement("Quantity", line.Quantity),
                        new XElement("UnitPrice", line.UnitPrice))))));

        return document.ToString(SaveOptions.DisableFormatting);
    }
}

public sealed class FiscalRule : TenantEntity, IAggregateRoot
{
    public string SourceUf { get; set; } = "";
    public string DestinationUf { get; set; } = "";
    public string Regime { get; set; } = "";
    public string OperationType { get; set; } = "";
    public string Ncm { get; set; } = "";
    public string? Cest { get; set; }
    public string Cfop { get; set; } = "";
    public string? Cst { get; set; }
    public string? Csosn { get; set; }
    public string Origin { get; set; } = "";
    public decimal IcmsRate { get; set; }
    public decimal IcmsStRate { get; set; }
    public decimal PisRate { get; set; }
    public decimal CofinsRate { get; set; }
    public decimal IbsRate { get; set; }
    public decimal CbsRate { get; set; }
    public string? BenefitCode { get; set; }
    public string? ExceptionCode { get; set; }
    public string LegalBasis { get; set; } = "";
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public bool Active { get; set; } = true;

    public static FiscalRule From(UpsertFiscalRuleRequest request, Guid companyId, Guid branchId)
        => new()
        {
            CompanyId = companyId,
            BranchId = branchId,
            SourceUf = request.SourceUf.Trim().ToUpperInvariant(),
            DestinationUf = request.DestinationUf.Trim().ToUpperInvariant(),
            Regime = request.Regime.Trim(),
            OperationType = request.OperationType.Trim(),
            Ncm = FiscalStrings.OnlyDigits(request.Ncm),
            Cest = string.IsNullOrWhiteSpace(request.Cest) ? null : FiscalStrings.OnlyDigits(request.Cest),
            Cfop = request.Cfop.Trim(),
            Cst = request.Cst?.Trim(),
            Csosn = request.Csosn?.Trim(),
            Origin = request.Origin.Trim(),
            IcmsRate = request.IcmsRate,
            IcmsStRate = request.IcmsStRate,
            PisRate = request.PisRate,
            CofinsRate = request.CofinsRate,
            IbsRate = request.IbsRate,
            CbsRate = request.CbsRate,
            BenefitCode = request.BenefitCode,
            ExceptionCode = request.ExceptionCode,
            LegalBasis = request.LegalBasis.Trim(),
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo,
            Active = request.Active
        };
}

public sealed class FiscalDocument : TenantEntity, IAggregateRoot
{
    public Guid? SalesOrderId { get; set; }
    public string Model { get; set; } = "55";
    public string Kind { get; set; } = FiscalDocumentKind.Outbound;
    public string Status { get; set; } = FiscalDocumentStatus.Draft;
    public string Provider { get; set; } = "sandbox";
    public string Environment { get; set; } = "homologation";
    public string AccessKey { get; set; } = "";
    public string Number { get; set; } = "";
    public string Series { get; set; } = "";
    public DateTimeOffset IssuedAt { get; set; }
    public string? AuthorizationProtocol { get; set; }
    public string XmlHash { get; set; } = "";
    public string XmlStorageUri { get; set; } = "";
    public string PayloadSnapshot { get; set; } = "{}";
    public string TaxSnapshot { get; set; } = "{}";
    public bool Contingency { get; set; }
    public string? ContingencyReason { get; set; }
    public List<FiscalDocumentItem> Items { get; set; } = [];
    public List<FiscalDocumentEvent> Events { get; set; } = [];

    public static FiscalDocument CreateFromSalesOrder(SalesOrder order, IssueFiscalDocumentRequest request, TaxCalculationResult taxes, StoredXml stored)
    {
        var document = new FiscalDocument
        {
            CompanyId = order.CompanyId,
            BranchId = order.BranchId,
            SalesOrderId = order.Id,
            Model = request.Model,
            Kind = FiscalDocumentKind.Outbound,
            Status = FiscalDocumentStatus.PendingAuthorization,
            Provider = request.Provider ?? "sandbox",
            Environment = request.Environment,
            AccessKey = request.AccessKey ?? "",
            Number = request.Number,
            Series = request.Series,
            IssuedAt = DateTimeOffset.UtcNow,
            XmlHash = stored.Hash,
            XmlStorageUri = stored.Uri,
            PayloadSnapshot = JsonPayload.Serialize(new { order.Id, order.CustomerNameSnapshot, order.Total }),
            TaxSnapshot = JsonPayload.Serialize(taxes),
            Contingency = request.Contingency,
            ContingencyReason = request.ContingencyReason
        };

        foreach (var line in order.Lines)
        {
            var tax = taxes.Items.First(x => x.LineId == line.Id);
            document.Items.Add(new FiscalDocumentItem
            {
                CompanyId = order.CompanyId,
                BranchId = order.BranchId,
                ProductId = line.ProductId,
                SkuSnapshot = line.SkuSnapshot,
                NcmSnapshot = line.NcmSnapshot,
                CestSnapshot = line.CestSnapshot,
                Quantity = line.Quantity,
                UnitAmount = line.UnitPrice,
                TotalAmount = line.Total,
                Cfop = tax.Cfop,
                Cst = tax.Cst,
                Csosn = tax.Csosn,
                TaxesJson = JsonPayload.Serialize(tax)
            });
        }

        document.RegisterEvent("criacao", "pending_authorization", null, "Documento fiscal criado para autorizacao.", "{}");
        document.AddDomainEvent(new FiscalDocumentCreated(document.Id, order.CompanyId, order.BranchId, request.Model, DateTimeOffset.UtcNow));
        return document;
    }

    public void RegisterProviderResponse(ProviderOperationResult response)
    {
        Status = response.Success ? FiscalDocumentStatus.Authorized : FiscalDocumentStatus.Rejected;
        AuthorizationProtocol = response.Protocol;
        RegisterEvent("autorizacao", response.Status, response.Protocol, response.Message, response.RawPayload);
        AddDomainEvent(new FiscalDocumentAuthorizationChanged(Id, CompanyId, BranchId, Status, DateTimeOffset.UtcNow));
    }

    public void RegisterEvent(string eventType, string status, string? protocol, string? message, string rawPayload)
    {
        Events.Add(new FiscalDocumentEvent
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            FiscalDocumentId = Id,
            EventType = eventType,
            Status = status,
            Protocol = protocol,
            Message = message,
            RawPayload = rawPayload,
            OccurredAt = DateTimeOffset.UtcNow
        });
    }
}

public sealed class FiscalDocumentItem : TenantEntity
{
    public Guid FiscalDocumentId { get; set; }
    public FiscalDocument? FiscalDocument { get; set; }
    public Guid ProductId { get; set; }
    public string SkuSnapshot { get; set; } = "";
    public string NcmSnapshot { get; set; } = "";
    public string? CestSnapshot { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Cfop { get; set; } = "";
    public string? Cst { get; set; }
    public string? Csosn { get; set; }
    public string TaxesJson { get; set; } = "{}";
}

public sealed class FiscalDocumentEvent : TenantEntity
{
    public Guid FiscalDocumentId { get; set; }
    public FiscalDocument? FiscalDocument { get; set; }
    public string EventType { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Protocol { get; set; }
    public string? Message { get; set; }
    public string RawPayload { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
}

public sealed class DfeDistributionCursor : TenantEntity
{
    public string Cnpj { get; set; } = "";
    public string Environment { get; set; } = "homologation";
    public long LastNsu { get; set; }
    public long MaxNsu { get; set; }
    public DateTimeOffset? LastRunAt { get; set; }
}

public static class FiscalDocumentStatus
{
    public const string Draft = "draft";
    public const string PendingAuthorization = "pending_authorization";
    public const string Authorized = "authorized";
    public const string Rejected = "rejected";
    public const string Cancelled = "cancelled";
    public const string Imported = "imported";
}

public static class FiscalDocumentKind
{
    public const string Outbound = "outbound";
    public const string Inbound = "inbound";
    public const string Service = "service";
}

public sealed record FiscalOperationContext(string SourceUf, string DestinationUf, string Regime, string OperationType, IReadOnlyCollection<FiscalOperationItem> Items)
{
    public static FiscalOperationContext From(SalesOrder order, IssueFiscalDocumentRequest request)
        => new(request.SourceUf, request.DestinationUf, request.Regime, request.OperationType,
            order.Lines.Select(x => new FiscalOperationItem(x.Id, x.ProductId, x.NcmSnapshot, x.CestSnapshot, x.Total)).ToArray());
}

public sealed record FiscalOperationItem(Guid LineId, Guid ProductId, string Ncm, string? Cest, decimal Total);
public sealed record TaxCalculationResult(IReadOnlyCollection<TaxItemCalculation> Items, decimal TotalIcms, decimal TotalIcmsSt, decimal TotalPis, decimal TotalCofins, decimal TotalIbs, decimal TotalCbs);
public sealed record TaxItemCalculation(Guid LineId, string Cfop, string? Cst, string? Csosn, string Origin, decimal BaseAmount, decimal Icms, decimal IcmsSt, decimal Pis, decimal Cofins, decimal Ibs, decimal Cbs, string? BenefitCode, string LegalBasis);
public sealed record StoredXml(string Hash, string Uri);

public sealed record UpsertFiscalRuleRequest(
    string SourceUf,
    string DestinationUf,
    string Regime,
    string OperationType,
    string Ncm,
    string? Cest,
    string Cfop,
    string? Cst,
    string? Csosn,
    string Origin,
    decimal IcmsRate,
    decimal IcmsStRate,
    decimal PisRate,
    decimal CofinsRate,
    decimal IbsRate,
    decimal CbsRate,
    string? BenefitCode,
    string? ExceptionCode,
    string LegalBasis,
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    bool Active);

public sealed record IssueFiscalDocumentRequest(
    Guid SalesOrderId,
    string Model,
    string Series,
    string Number,
    string? AccessKey,
    string Environment,
    string? Provider,
    string SourceUf,
    string DestinationUf,
    string Regime,
    string OperationType,
    bool Contingency,
    string? ContingencyReason);

public sealed record CancelFiscalDocumentRequest(string Provider, string Reason);
public sealed record InutilizeNumberRequest(string Provider, string Model, string Series, int FromNumber, int ToNumber, string Justification, string Environment);
public sealed record DfeDistributionRequest(string Provider, string Cnpj, string Environment, long LastNsu);
public sealed record RecipientManifestationRequest(string Provider, string AccessKey, string EventType, string Justification, string Environment);
public sealed record ImportInboundXmlRequest(string Provider, string Xml, Guid? PurchaseOrderId);
public sealed record ProviderOperationResult(bool Success, string Status, string Message, string? Protocol, string RawPayload);
public sealed record DfeDistributionResult(long LastNsu, long MaxNsu, IReadOnlyCollection<DfeSummary> Documents);
public sealed record DfeSummary(string AccessKey, string Nsu, string Schema, string SummaryXml);
public sealed record InboundXmlImportResult(Guid FiscalDocumentId, string AccessKey, string XmlHash, string XmlUri);

public sealed record FiscalRuleSummary(Guid Id, string SourceUf, string DestinationUf, string Ncm, string? Cest, string Cfop, DateOnly ValidFrom, DateOnly? ValidTo)
{
    public static FiscalRuleSummary From(FiscalRule rule) => new(rule.Id, rule.SourceUf, rule.DestinationUf, rule.Ncm, rule.Cest, rule.Cfop, rule.ValidFrom, rule.ValidTo);
}

public sealed record FiscalDocumentSummary(Guid Id, string Model, string Kind, string Status, string AccessKey, string Number, string Series, string? Protocol, string XmlHash, DateTimeOffset IssuedAt)
{
    public static FiscalDocumentSummary From(FiscalDocument document) => new(document.Id, document.Model, document.Kind, document.Status, document.AccessKey, document.Number, document.Series, document.AuthorizationProtocol, document.XmlHash, document.IssuedAt);
}

public sealed record FiscalDocumentCreated(Guid FiscalDocumentId, Guid CompanyId, Guid BranchId, string Model, DateTimeOffset OccurredAt) : IDomainEvent;
public sealed record FiscalDocumentAuthorizationChanged(Guid FiscalDocumentId, Guid CompanyId, Guid BranchId, string Status, DateTimeOffset OccurredAt) : IDomainEvent;

public static class FiscalMapping
{
    public static void ConfigureFiscal(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FiscalRule>(builder =>
        {
            builder.ToTable("fiscal_rules", "fiscal");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.SourceUf).HasMaxLength(2).IsRequired();
            builder.Property(x => x.DestinationUf).HasMaxLength(2).IsRequired();
            builder.Property(x => x.Regime).HasMaxLength(60).IsRequired();
            builder.Property(x => x.OperationType).HasMaxLength(80).IsRequired();
            builder.Property(x => x.Ncm).HasMaxLength(16).IsRequired();
            builder.Property(x => x.Cest).HasMaxLength(16);
            builder.Property(x => x.Cfop).HasMaxLength(8).IsRequired();
            builder.Property(x => x.Cst).HasMaxLength(8);
            builder.Property(x => x.Csosn).HasMaxLength(8);
            builder.Property(x => x.Origin).HasMaxLength(4).IsRequired();
            builder.Property(x => x.BenefitCode).HasMaxLength(60);
            builder.Property(x => x.ExceptionCode).HasMaxLength(80);
            builder.Property(x => x.LegalBasis).HasMaxLength(800).IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.SourceUf, x.DestinationUf, x.Regime, x.OperationType, x.Ncm, x.Cest, x.ValidFrom });
        });

        modelBuilder.Entity<FiscalDocument>(builder =>
        {
            builder.ToTable("fiscal_documents", "fiscal");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.Model).HasMaxLength(8).IsRequired();
            builder.Property(x => x.Kind).HasMaxLength(40).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(60).IsRequired();
            builder.Property(x => x.Provider).HasMaxLength(80).IsRequired();
            builder.Property(x => x.Environment).HasMaxLength(40).IsRequired();
            builder.Property(x => x.AccessKey).HasMaxLength(80).IsRequired();
            builder.Property(x => x.Number).HasMaxLength(20).IsRequired();
            builder.Property(x => x.Series).HasMaxLength(8).IsRequired();
            builder.Property(x => x.AuthorizationProtocol).HasMaxLength(80);
            builder.Property(x => x.XmlHash).HasMaxLength(80).IsRequired();
            builder.Property(x => x.XmlStorageUri).HasMaxLength(600).IsRequired();
            builder.Property(x => x.PayloadSnapshot).HasColumnType("jsonb").IsRequired();
            builder.Property(x => x.TaxSnapshot).HasColumnType("jsonb").IsRequired();
            builder.Property(x => x.ContingencyReason).HasMaxLength(500);
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.Model, x.Series, x.Number }).IsUnique();
            builder.HasIndex(x => new { x.CompanyId, x.AccessKey });
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.Status, x.IssuedAt });
        });

        modelBuilder.Entity<FiscalDocumentItem>(builder =>
        {
            builder.ToTable("fiscal_document_items", "fiscal");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.SkuSnapshot).HasMaxLength(80).IsRequired();
            builder.Property(x => x.NcmSnapshot).HasMaxLength(16).IsRequired();
            builder.Property(x => x.CestSnapshot).HasMaxLength(16);
            builder.Property(x => x.Cfop).HasMaxLength(8).IsRequired();
            builder.Property(x => x.Cst).HasMaxLength(8);
            builder.Property(x => x.Csosn).HasMaxLength(8);
            builder.Property(x => x.TaxesJson).HasColumnType("jsonb").IsRequired();
            builder.HasOne(x => x.FiscalDocument).WithMany(x => x.Items).HasForeignKey(x => x.FiscalDocumentId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.CompanyId, x.ProductId });
        });

        modelBuilder.Entity<FiscalDocumentEvent>(builder =>
        {
            builder.ToTable("fiscal_document_events", "fiscal");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.EventType).HasMaxLength(80).IsRequired();
            builder.Property(x => x.Status).HasMaxLength(80).IsRequired();
            builder.Property(x => x.Protocol).HasMaxLength(80);
            builder.Property(x => x.Message).HasMaxLength(800);
            builder.Property(x => x.RawPayload).HasColumnType("jsonb").IsRequired();
            builder.HasOne(x => x.FiscalDocument).WithMany(x => x.Events).HasForeignKey(x => x.FiscalDocumentId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.FiscalDocumentId, x.OccurredAt });
        });

        modelBuilder.Entity<DfeDistributionCursor>(builder =>
        {
            builder.ToTable("dfe_distribution_cursors", "fiscal");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.Cnpj).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Environment).HasMaxLength(40).IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.Cnpj, x.Environment }).IsUnique();
        });
    }
}
