using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Atlas.Api.Common;

public interface IAggregateRoot;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}

public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredAt { get; }
    Guid CompanyId { get; }
    Guid BranchId { get; }
}

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected set; } = Guid.CreateVersion7();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public IReadOnlyCollection<IDomainEvent> DequeueDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }
}

public abstract class TenantEntity : Entity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public long RowVersion { get; set; }
}

public sealed record Money(decimal Amount, string Currency = "BRL")
{
    public static Money Zero(string currency = "BRL") => new(0, currency);
}

public sealed record Dimensions(decimal? HeightCm, decimal? WidthCm, decimal? LengthCm);

public sealed record Address(
    string Street,
    string Number,
    string District,
    string City,
    string State,
    string PostalCode,
    string Country = "BR");

public sealed record Period(DateOnly StartsOn, DateOnly? EndsOn)
{
    public bool Includes(DateOnly day) => StartsOn <= day && (EndsOn is null || EndsOn >= day);
}

public static class FiscalStrings
{
    public static string OnlyDigits(string value) => new(value.Where(char.IsDigit).ToArray());

    public static string NormalizeCode(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();

    public static string Sha256Hex(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public static class JsonPayload
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public static string Serialize(object value) => JsonSerializer.Serialize(value, value.GetType(), Options);
}

public sealed record PagedRequest(int Page = 1, int PageSize = 50)
{
    public int Skip => Math.Max(0, Page - 1) * Size;
    public int Size => Math.Clamp(PageSize, 1, 200);
}

public sealed record PagedResponse<T>(IReadOnlyCollection<T> Items, int Total, int Page, int PageSize);
