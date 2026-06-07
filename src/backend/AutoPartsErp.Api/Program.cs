using System.Threading.RateLimiting;
using Atlas.Api.Administration;
using Atlas.Api.Catalog;
using Atlas.Api.Common;
using Atlas.Api.Crm;
using Atlas.Api.Finance;
using Atlas.Api.Fiscal;
using Atlas.Api.IdentityAccess;
using Atlas.Api.Infrastructure;
using Atlas.Api.Integrations;
using Atlas.Api.Inventory;
using Atlas.Api.Purchasing;
using Atlas.Api.Reporting;
using Atlas.Api.Sales;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    var atlasOptions = builder.Configuration.GetSection("Atlas").Get<AtlasOptions>() ?? new AtlasOptions();
    var localAuthOptions = builder.Configuration.GetSection("LocalAuthentication").Get<LocalAuthenticationOptions>() ?? new LocalAuthenticationOptions();
    builder.Services.AddSingleton(atlasOptions);
    builder.Services.AddSingleton(localAuthOptions);
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ITenantContext, TenantContext>();
    builder.Services.AddScoped<TenantSessionInterceptor>();

    builder.Services.AddDbContext<AtlasDbContext>((serviceProvider, options) =>
    {
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("Postgres"),
            npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "administration"));
        options.AddInterceptors(serviceProvider.GetRequiredService<TenantSessionInterceptor>());
    });

    if (atlasOptions.EnableRedis && !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Redis")))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("Redis");
            options.InstanceName = "autoparts-erp:";
        });
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
    }

    if (atlasOptions.EnableRabbitMq)
    {
        builder.Services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();
            bus.UsingRabbitMq((context, cfg) =>
            {
                var rabbit = builder.Configuration.GetSection("RabbitMq");
                cfg.Host(rabbit["Host"] ?? "localhost", rabbit["VirtualHost"] ?? "/", host =>
                {
                    host.Username(rabbit["Username"] ?? "atlas");
                    host.Password(rabbit["Password"] ?? "autoparts_dev_password");
                });
                cfg.ConfigureEndpoints(context);
            });
        });
        builder.Services.AddSingleton<IOutboxPublisher, RabbitMqOutboxPublisher>();
    }
    else
    {
        builder.Services.AddSingleton<IOutboxPublisher, LocalOutboxPublisher>();
    }

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(localAuthOptions.SigningKey)),
                ValidateAudience = true,
                ValidAudience = localAuthOptions.Audience,
                ValidateIssuer = true,
                ValidIssuer = localAuthOptions.Issuer,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
                NameClaimType = "preferred_username",
                RoleClaimType = "roles"
            };
        });

    builder.Services.AddAuthorization(options => options.AddAtlasPolicies());

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("desktop-local", policy => policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173", "tauri://localhost")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
    });

    builder.Services.AddProblemDetails();
    builder.Services.AddHealthChecks();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "AutoParts ERP API Local",
            Version = "v1",
            Description = "API local do AutoParts ERP Desktop para autopecas, distribuidores automotivos e caminhoes no Brasil."
        });
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 600,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 50,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    if (atlasOptions.EnableOpenTelemetry)
    {
        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("autoparts-erp-api"))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
                tracing.AddHttpClientInstrumentation();
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(otlpEndpoint));
                }
            });
    }

    builder.Services.AddHostedService<MigrationsHostedService>();
    builder.Services.AddHostedService<OutboxDispatcher>();

    builder.Services.AddScoped<ProductSearchService>();
    builder.Services.AddScoped<PurchaseSuggestionService>();
    builder.Services.AddScoped<InventoryService>();
    builder.Services.AddScoped<SalesService>();
    builder.Services.AddScoped<TaxEngine>();
    builder.Services.AddScoped<FiscalDocumentService>();
    builder.Services.AddScoped<FinanceReconciliationService>();
    builder.Services.AddScoped<AftermarketCatalogGateway>();
    builder.Services.AddScoped<LocalAuthenticationService>();
    builder.Services.AddSingleton<PasswordHasher>();
    builder.Services.AddSingleton<JwtTokenService>();
    builder.Services.AddSingleton<ImmutableXmlStore>();
    builder.Services.AddSingleton<FiscalProviderRouter>();
    builder.Services.AddSingleton<IFiscalDocumentProvider, SandboxFiscalProvider>();
    builder.Services.AddSingleton<IAftermarketCatalogAdapter, NullAftermarketCatalogAdapter>();

    builder.Services
        .AddEndpointModule<AdministrationModule>()
        .AddEndpointModule<IdentityAccessModule>()
        .AddEndpointModule<CatalogModule>()
        .AddEndpointModule<PurchasingModule>()
        .AddEndpointModule<InventoryModule>()
        .AddEndpointModule<SalesModule>()
        .AddEndpointModule<FiscalModule>()
        .AddEndpointModule<FinanceModule>()
        .AddEndpointModule<CrmModule>()
        .AddEndpointModule<ReportingModule>()
        .AddEndpointModule<IntegrationsModule>();

    var app = builder.Build();

    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://autoparts-erp.local/problems/internal-error",
                title = "Erro interno",
                status = 500,
                traceId = context.TraceIdentifier
            });
        });
    });

    app.UseSerilogRequestLogging();
    app.UseCors("desktop-local");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AutoParts ERP API v1");
        options.RoutePrefix = "swagger";
    });

    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
    app.MapHealthChecks("/health/live").AllowAnonymous();
    app.MapHealthChecks("/health/ready").AllowAnonymous();
    app.MapAtlasModules();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Atlas API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
