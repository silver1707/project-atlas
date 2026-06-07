using System.Security.Claims;

namespace Atlas.Api.Common;

public interface ITenantContext
{
    Guid CompanyId { get; }
    Guid BranchId { get; }
    string UserId { get; }
    string? IpAddress { get; }
    bool HasExplicitTenant { get; }
}

public sealed class TenantContext(IHttpContextAccessor accessor, AtlasOptions options) : ITenantContext
{
    public Guid CompanyId => TryHeader("X-Company-Id")
                             ?? TryClaim("company_id")
                             ?? options.DefaultCompanyId;

    public Guid BranchId => TryHeader("X-Branch-Id")
                            ?? TryClaim("branch_id")
                            ?? options.DefaultBranchId;

    public string UserId =>
        accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? accessor.HttpContext?.User.FindFirstValue("sub")
        ?? "system";

    public string? IpAddress =>
        accessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        ?? accessor.HttpContext?.Request.Headers["X-Forwarded-For"].FirstOrDefault();

    public bool HasExplicitTenant =>
        accessor.HttpContext?.Request.Headers.ContainsKey("X-Company-Id") == true
        || accessor.HttpContext?.User.HasClaim(claim => claim.Type == "company_id") == true;

    private Guid? TryHeader(string header)
    {
        var value = accessor.HttpContext?.Request.Headers[header].FirstOrDefault();
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private Guid? TryClaim(string claim)
    {
        var value = accessor.HttpContext?.User.FindFirstValue(claim);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}

public sealed class AtlasOptions
{
    public bool ApplyMigrationsOnStartup { get; init; }
    public string InstallationProfile { get; init; } = "simple";
    public bool EnableRedis { get; init; }
    public bool EnableRabbitMq { get; init; }
    public bool EnableOpenTelemetry { get; init; }
    public string ImmutableXmlStoragePath { get; init; } = "C:/AutoPartsErp/FiscalXml";
    public string DefaultFiscalProvider { get; init; } = "sandbox";
    public Guid DefaultCompanyId { get; init; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public Guid DefaultBranchId { get; init; } = Guid.Parse("22222222-2222-2222-2222-222222222222");
}
