namespace Atlas.Api.Common;

public interface IEndpointModule
{
    string Name { get; }
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}

public interface IModuleInstaller
{
    string Name { get; }
    void Register(IServiceCollection services, IConfiguration configuration);
}

public static class EndpointModuleExtensions
{
    public static IServiceCollection AddEndpointModule<T>(this IServiceCollection services)
        where T : class, IEndpointModule
        => services.AddSingleton<IEndpointModule, T>();

    public static WebApplication MapAtlasModules(this WebApplication app)
    {
        var modules = app.Services.GetRequiredService<IEnumerable<IEndpointModule>>();
        foreach (var module in modules)
        {
            module.MapEndpoints(app);
        }

        return app;
    }
}
