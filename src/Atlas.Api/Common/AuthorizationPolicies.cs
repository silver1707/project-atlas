using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Atlas.Api.Common;

public static class AuthorizationPolicies
{
    public const string SensitiveFiscal = "sensitive:fiscal";
    public const string SensitiveFinance = "sensitive:finance";
    public const string Administration = "admin:tenant";

    public static AuthorizationOptions AddAtlasPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(SensitiveFiscal, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("mfa", "true");
            policy.RequireAssertion(ctx => HasAnyRole(ctx.User, "fiscal-manager", "admin"));
        });

        options.AddPolicy(SensitiveFinance, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("mfa", "true");
            policy.RequireAssertion(ctx => HasAnyRole(ctx.User, "finance-manager", "admin"));
        });

        options.AddPolicy(Administration, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireClaim("mfa", "true");
            policy.RequireAssertion(ctx => HasAnyRole(ctx.User, "admin"));
        });

        return options;
    }

    private static bool HasAnyRole(ClaimsPrincipal user, params string[] roles)
        => roles.Any(role => user.IsInRole(role) || user.HasClaim("realm_access.roles", role) || user.HasClaim("roles", role));
}
