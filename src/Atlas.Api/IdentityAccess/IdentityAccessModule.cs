using System.Security.Claims;
using Atlas.Api.Administration;
using Atlas.Api.Common;
using Atlas.Api.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api.IdentityAccess;

public sealed class IdentityAccessModule : IEndpointModule
{
    public string Name => "Identity and Access";

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/identity")
            .WithTags(Name)
            .RequireAuthorization();

        group.MapGet("/me", async Task<Ok<CurrentUserResponse>> (
            ClaimsPrincipal user,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var roles = user.Claims
                .Where(x => x.Type is ClaimTypes.Role or "roles" or "realm_access.roles")
                .Select(x => x.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var permissions = await db.ModulePermissions.AsNoTracking()
                .Where(x => x.CompanyId == tenant.CompanyId && x.BranchId == tenant.BranchId && roles.Contains(x.RoleName))
                .Select(x => new PermissionGrant(x.Module, x.Permission, x.RequiresMfa))
                .ToListAsync(ct);

            return TypedResults.Ok(new CurrentUserResponse(
                user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub") ?? "",
                user.Identity?.Name ?? user.FindFirstValue("preferred_username") ?? "",
                tenant.CompanyId,
                tenant.BranchId,
                roles,
                permissions));
        });

        group.MapPost("/profiles", async Task<Created<UserProfileSummary>> (
            UpsertUserProfileRequest request,
            AtlasDbContext db,
            ITenantContext tenant,
            CancellationToken ct) =>
        {
            var profile = new UserProfile
            {
                CompanyId = tenant.CompanyId,
                BranchId = tenant.BranchId,
                SubjectId = request.SubjectId.Trim(),
                DisplayName = request.DisplayName.Trim(),
                Email = request.Email.Trim(),
                SensitiveProfile = request.SensitiveProfile,
                Active = request.Active
            };
            db.Set<UserProfile>().Add(profile);
            await db.SaveChangesAsync(ct);
            return TypedResults.Created($"/api/identity/profiles/{profile.Id}", UserProfileSummary.From(profile));
        }).RequireAuthorization(AuthorizationPolicies.Administration);
    }
}

public sealed class UserProfile : TenantEntity
{
    public string SubjectId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool SensitiveProfile { get; set; }
    public bool Active { get; set; } = true;
}

public sealed record PermissionGrant(string Module, string Permission, bool RequiresMfa);
public sealed record CurrentUserResponse(string SubjectId, string Username, Guid CompanyId, Guid BranchId, IReadOnlyCollection<string> Roles, IReadOnlyCollection<PermissionGrant> Permissions);
public sealed record UpsertUserProfileRequest(string SubjectId, string DisplayName, string Email, bool SensitiveProfile, bool Active);
public sealed record UserProfileSummary(Guid Id, string SubjectId, string DisplayName, string Email, bool SensitiveProfile, bool Active)
{
    public static UserProfileSummary From(UserProfile profile) => new(profile.Id, profile.SubjectId, profile.DisplayName, profile.Email, profile.SensitiveProfile, profile.Active);
}

public static class IdentityAccessMapping
{
    public static void ConfigureIdentityAccess(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>(builder =>
        {
            builder.ToTable("user_profiles", "identity");
            builder.ConfigureTenantEntity();
            builder.Property(x => x.SubjectId).HasMaxLength(160).IsRequired();
            builder.Property(x => x.DisplayName).HasMaxLength(180).IsRequired();
            builder.Property(x => x.Email).HasMaxLength(180).IsRequired();
            builder.HasIndex(x => new { x.CompanyId, x.SubjectId }).IsUnique();
        });
    }
}
