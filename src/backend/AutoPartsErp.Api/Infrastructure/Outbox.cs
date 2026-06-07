using Atlas.Api.Common;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Api.Infrastructure;

public sealed class OutboxMessage : Entity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string EventType { get; set; } = "";
    public string Payload { get; set; } = "{}";
    public string Headers { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public string? FailureReason { get; set; }
    public int Attempts { get; set; }

    public static OutboxMessage FromDomainEvent(IDomainEvent domainEvent, Guid companyId, Guid branchId)
        => new()
        {
            CompanyId = companyId,
            BranchId = branchId,
            EventType = domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
            Payload = JsonPayload.Serialize(domainEvent),
            Headers = JsonPayload.Serialize(new
            {
                source = "autoparts-erp.modular-monolith",
                eventKind = "domain",
                schemaVersion = 1
            }),
            OccurredAt = domainEvent.OccurredAt
        };
}

public sealed record IntegrationEventEnvelope(
    Guid Id,
    Guid CompanyId,
    Guid BranchId,
    string EventType,
    string Payload,
    string Headers,
    DateTimeOffset OccurredAt);

public interface IOutboxPublisher
{
    Task PublishAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken);
}

public sealed class RabbitMqOutboxPublisher(IBus bus) : IOutboxPublisher
{
    public Task PublishAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken)
        => bus.Publish(envelope, cancellationToken);
}

public sealed class LocalOutboxPublisher(ILogger<LocalOutboxPublisher> logger) : IOutboxPublisher
{
    public Task PublishAsync(IntegrationEventEnvelope envelope, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processed local outbox event {EventType} {EventId} without RabbitMQ", envelope.EventType, envelope.Id);
        return Task.CompletedTask;
    }
}

public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IOutboxPublisher publisher,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatch(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox dispatch failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task DispatchBatch(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
        var messages = await db.OutboxMessages
            .Where(x => x.ProcessedAt == null && x.Attempts < 10)
            .OrderBy(x => x.OccurredAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(new IntegrationEventEnvelope(
                    message.Id,
                    message.CompanyId,
                    message.BranchId,
                    message.EventType,
                    message.Payload,
                    message.Headers,
                    message.OccurredAt), cancellationToken);

                message.ProcessedAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                message.Attempts += 1;
                message.FailedAt = DateTimeOffset.UtcNow;
                message.FailureReason = ex.Message;
                logger.LogWarning(ex, "Could not publish outbox message {OutboxMessageId}", message.Id);
            }
        }

        if (messages.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

public static class OutboxMapping
{
    public static void ConfigureOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("outbox_messages", "integrations");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.EventType).HasMaxLength(300).IsRequired();
            builder.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
            builder.Property(x => x.Headers).HasColumnType("jsonb").IsRequired();
            builder.HasIndex(x => new { x.ProcessedAt, x.OccurredAt });
            builder.HasIndex(x => new { x.CompanyId, x.BranchId, x.ProcessedAt });
        });
    }
}
