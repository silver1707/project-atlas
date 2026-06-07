using Atlas.Api.Common;

namespace Atlas.Api.Tests;

public sealed class FakeTenantContext : ITenantContext
{
    public Guid CompanyId { get; init; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public Guid BranchId { get; init; } = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public string UserId { get; init; } = "test-user";
    public string? IpAddress { get; init; } = "127.0.0.1";
    public bool HasExplicitTenant => true;
}
