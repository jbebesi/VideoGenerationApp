using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ComfyUI.Client.Configuration;
using ComfyUI.Client.Services;

namespace ComfyUI.Client.Extensions;

/// <summary>
/// Extension methods for IServiceCollection to register ComfyUI client services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds ComfyUI client services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddComfyUIClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ComfyUIClientOptions>(configuration.GetSection("ComfyUIClient"));
        
        services.AddHttpClient<IComfyUIApiClient, ComfyUIApiClient>((serviceProvider, client) =>
        {
            var options = Microsoft.Extensions.Options.Options.Create(configuration.GetSection("ComfyUIClient").Get<ComfyUIClientOptions>() ?? new ComfyUIClientOptions());
            client.BaseAddress = new Uri(options.Value.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds);
        });

        services.AddScoped<IComfyUIApiClient, ComfyUIApiClient>();
        
        return services;
    }

    /// <summary>
    /// Adds ComfyUI client services to the service collection with options
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddComfyUIClient(this IServiceCollection services, Action<ComfyUIClientOptions> configureOptions)
    {
        var options = new ComfyUIClientOptions();
        configureOptions(options);
        
        services.Configure<ComfyUIClientOptions>(opt =>
        {
            opt.BaseUrl = options.BaseUrl;
            opt.TimeoutSeconds = options.TimeoutSeconds;
        });
        
        services.AddHttpClient<IComfyUIApiClient, ComfyUIApiClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        services.AddScoped<IComfyUIApiClient, ComfyUIApiClient>();
        
        return services;
    }
}