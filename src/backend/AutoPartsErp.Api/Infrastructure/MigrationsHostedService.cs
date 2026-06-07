using Microsoft.EntityFrameworkCore;

namespace Atlas.Api.Infrastructure;

public sealed class MigrationsHostedService(IServiceProvider serviceProvider, AtlasOptions options, ILogger<MigrationsHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.ApplyMigrationsOnStartup)
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AtlasDbContext>();
        logger.LogInformation("Applying database migrations");
        await db.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
