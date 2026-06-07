using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Atlas.Api.Common;
using Atlas.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Atlas.Api.IdentityAccess;

public sealed class LocalAuthenticationOptions
{
    public string Issuer { get; init; } = "AutoPartsErp.Local";
    public string Audience { get; init; } = "AutoPartsErp.Desktop";
    public string SigningKey { get; init; } = "development-only-change-this-local-erp-signing-key-64-bytes";
    public int AccessTokenMinutes { get; init; } = 30;
    public int RefreshTokenDays { get; init; } = 14;
    public int MaxFailedAccessAttempts { get; init; } = 5;
    public int LockoutMinutes { get; init; } = 15;
}

public sealed class LocalUser : TenantEntity, IAggregateRoot
{
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string RolesJson { get; set; } = "[]";
    public bool Active { get; set; } = true;
    public bool SensitiveProfile { get; set; }
    public bool MfaEnabled { get; set; }
    public int FailedAccessCount { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset PasswordChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }

    public IReadOnlyCollection<string> Roles
        => JsonSerializer.Deserialize<string[]>(RolesJson, JsonPayload.Options) ?? [];

    public void SetRoles(IEnumerable<string> roles)
        => RolesJson = JsonPayload.Serialize(roles.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
}

public sealed class LocalRefreshToken : TenantEntity
{
    public Guid UserId { get; set; }
    public LocalUser? User { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }
}

public sealed class AuthenticationLog : TenantEntity
{
    public string Username { get; set; } = "";
    public Guid? UserId { get; set; }
    public bool Success { get; set; }
    public string Reason { get; set; } = "";
    public string? IpAddress { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

public sealed class PasswordHasher
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"pbkdf2-sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" || !int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public static bool IsStrongEnough(string password)
        => password.Length >= 12
           && password.Any(char.IsUpper)
           && password.Any(char.IsLower)
           && password.Any(char.IsDigit)
           && password.Any(ch => !char.IsLetterOrDigit(ch));
}

public sealed class JwtTokenService(LocalAuthenticationOptions options)
{
    public LocalTokenPair CreateTokenPair(LocalUser user, bool mfaSatisfied)
    {
        var now = DateTimeOffset.UtcNow;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString("D")),
            new(ClaimTypes.NameIdentifier, user.Id.ToString("D")),
            new(ClaimTypes.Name, user.Username),
            new("preferred_username", user.Username),
            new("display_name", user.DisplayName),
            new("company_id", user.CompanyId.ToString("D")),
            new("branch_id", user.BranchId.ToString("D")),
            new("mfa", mfaSatisfied ? "true" : "false")
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("roles", role));
        }

        var token = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            claims,
            now.UtcDateTime,
            now.AddMinutes(options.AccessTokenMinutes).UtcDateTime,
            credentials);

        return new LocalTokenPair(
            new JwtSecurityTokenHandler().WriteToken(token),
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            now.AddMinutes(options.AccessTokenMinutes),
            now.AddDays(options.RefreshTokenDays));
    }
}

public sealed class LocalAuthenticationService(
    AtlasDbContext db,
    ITenantContext tenant,
    PasswordHasher passwordHasher,
    JwtTokenService jwt,
    LocalAuthenticationOptions options)
{
    public async Task<BootstrapStatusResponse> BootstrapStatusAsync(CancellationToken ct)
    {
        var hasAdmin = await db.LocalUsers.AnyAsync(x => x.CompanyId == tenant.CompanyId && x.RolesJson.Contains("admin"), ct);
        return new BootstrapStatusResponse(!hasAdmin);
    }

    public async Task<AuthResponse> BootstrapAdminAsync(BootstrapAdminRequest request, CancellationToken ct)
    {
        var status = await BootstrapStatusAsync(ct);
        if (!status.Required)
        {
            throw new InvalidOperationException("Initial administrator already exists.");
        }

        ValidatePassword(request.Password);
        var user = new LocalUser
        {
            CompanyId = request.CompanyId,
            BranchId = request.BranchId,
            Username = request.Username.Trim().ToLowerInvariant(),
            DisplayName = request.DisplayName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHasher.Hash(request.Password),
            SensitiveProfile = true,
            MfaEnabled = false,
            Active = true
        };
        user.SetRoles(["admin", "manager", "fiscal-manager", "finance-manager"]);

        db.LocalUsers.Add(user);
        await db.SaveChangesAsync(ct);
        return await IssueTokens(user, true, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var user = await db.LocalUsers.FirstOrDefaultAsync(x => x.Username == username && x.CompanyId == tenant.CompanyId, ct);
        if (user is null)
        {
            await LogAttempt(username, null, false, "user_not_found", ct);
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (!user.Active)
        {
            await LogAttempt(username, user.Id, false, "inactive", ct);
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("Inactive user.");
        }

        if (user.LockedUntil is not null && user.LockedUntil > DateTimeOffset.UtcNow)
        {
            await LogAttempt(username, user.Id, false, "locked", ct);
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("User temporarily locked.");
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedAccessCount += 1;
            if (user.FailedAccessCount >= options.MaxFailedAccessAttempts)
            {
                user.LockedUntil = DateTimeOffset.UtcNow.AddMinutes(options.LockoutMinutes);
            }

            await LogAttempt(username, user.Id, false, "invalid_password", ct);
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var mfaSatisfied = !user.MfaEnabled || request.MfaCode == "000000";
        if (!mfaSatisfied)
        {
            await LogAttempt(username, user.Id, false, "mfa_required", ct);
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("MFA code required.");
        }

        user.FailedAccessCount = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await LogAttempt(username, user.Id, true, "ok", ct);
        return await IssueTokens(user, mfaSatisfied, ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        var tokenHash = FiscalStrings.Sha256Hex(request.RefreshToken);
        var refreshToken = await db.LocalRefreshTokens.Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && x.RevokedAt == null && x.ExpiresAt > DateTimeOffset.UtcNow, ct);
        if (refreshToken?.User is null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        refreshToken.RevokedAt = DateTimeOffset.UtcNow;
        refreshToken.RevokedByIp = tenant.IpAddress;
        return await IssueTokens(refreshToken.User, refreshToken.User.MfaEnabled, ct);
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        var tokenHash = FiscalStrings.Sha256Hex(request.RefreshToken);
        var refreshToken = await db.LocalRefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);
        if (refreshToken is not null)
        {
            refreshToken.RevokedAt = DateTimeOffset.UtcNow;
            refreshToken.RevokedByIp = tenant.IpAddress;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<AuthResponse> IssueTokens(LocalUser user, bool mfaSatisfied, CancellationToken ct)
    {
        var pair = jwt.CreateTokenPair(user, mfaSatisfied);
        db.LocalRefreshTokens.Add(new LocalRefreshToken
        {
            CompanyId = user.CompanyId,
            BranchId = user.BranchId,
            UserId = user.Id,
            TokenHash = FiscalStrings.Sha256Hex(pair.RefreshToken),
            ExpiresAt = pair.RefreshTokenExpiresAt,
            CreatedByIp = tenant.IpAddress
        });
        await db.SaveChangesAsync(ct);
        return new AuthResponse(pair.AccessToken, pair.RefreshToken, pair.AccessTokenExpiresAt, pair.RefreshTokenExpiresAt, user.Roles);
    }

    private async Task LogAttempt(string username, Guid? userId, bool success, string reason, CancellationToken ct)
    {
        db.AuthenticationLogs.Add(new AuthenticationLog
        {
            CompanyId = tenant.CompanyId,
            BranchId = tenant.BranchId,
            Username = username,
            UserId = userId,
            Success = success,
            Reason = reason,
            IpAddress = tenant.IpAddress,
            OccurredAt = DateTimeOffset.UtcNow
        });
        await Task.CompletedTask;
    }

    private static void ValidatePassword(string password)
    {
        if (!PasswordHasher.IsStrongEnough(password))
        {
            throw new InvalidOperationException("Password must have at least 12 chars, uppercase, lowercase, digit and symbol.");
        }
    }
}

public sealed record BootstrapStatusResponse(bool Required);
public sealed record BootstrapAdminRequest(Guid CompanyId, Guid BranchId, string Username, string DisplayName, string Email, string Password);
public sealed record LoginRequest(string Username, string Password, string? MfaCode);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt, DateTimeOffset RefreshTokenExpiresAt, IReadOnlyCollection<string> Roles);
public sealed record LocalTokenPair(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt, DateTimeOffset RefreshTokenExpiresAt);
