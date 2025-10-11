using VideoGenerationApp.Components;
using VideoGenerationApp.Services;
using VideoGenerationApp.Services.Generation;
using VideoGenerationApp.Configuration;
using VideoGenerationApp.Logging;
using Microsoft.Extensions.Options;
using VideoGenerationApp.Dto;

var builder = WebApplication.CreateBuilder(args);

// Configure console logging with timestamps
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.FormatterName = "custom-timestamp";
});
builder.Logging.AddConsoleFormatter<CustomTimestampConsoleFormatter, Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions>();

// Configure settings
builder.Services.Configure<ComfyUISettings>(
    builder.Configuration.GetSection("ComfyUI"));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        // Enable detailed circuit errors to diagnose issues during rendering/interaction
        options.DetailedErrors = true;
    });

builder.Services.AddHttpClient("Ollama", client =>
{
    client.BaseAddress = new Uri("http://localhost:11434");
});
builder.Services.AddScoped<IOllamaService, OllamaService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<OllamaService>>();
    return new OllamaService(factory.CreateClient("Ollama"), logger);
});

// Register ComfyUI audio service
builder.Services.AddScoped<IComfyUIAudioService, ComfyUIAudioService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<ComfyUIAudioService>>();
    var environment = sp.GetRequiredService<IWebHostEnvironment>();
    var settings = sp.GetRequiredService<IOptions<ComfyUISettings>>();
    return new ComfyUIAudioService(httpClientFactory.CreateClient(), logger, environment, settings);
});

// Register ComfyUI image service
builder.Services.AddScoped<IComfyUIImageService, ComfyUIImageService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<ComfyUIImageService>>();
    var environment = sp.GetRequiredService<IWebHostEnvironment>();
    var settings = sp.GetRequiredService<IOptions<ComfyUISettings>>();
    return new ComfyUIImageService(httpClientFactory.CreateClient(), logger, environment, settings);
});

// Register ComfyUI video service
builder.Services.AddScoped<IComfyUIVideoService, ComfyUIVideoService>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var logger = sp.GetRequiredService<ILogger<ComfyUIVideoService>>();
    var environment = sp.GetRequiredService<IWebHostEnvironment>();
    var settings = sp.GetRequiredService<IOptions<ComfyUISettings>>();
    return new ComfyUIVideoService(httpClientFactory.CreateClient(), logger, environment, settings);
});

// Register Generation Services
builder.Services.AddSingleton<IGenerationService<AudioWorkflowConfig>, AudioGenerationService>();
builder.Services.AddSingleton<IGenerationService<ImageWorkflowConfig>, ImageGenerationService>();
builder.Services.AddSingleton<IGenerationService<VideoWorkflowConfig>, VideoGenerationService>();

// Register Generation Queue Service as hosted service
builder.Services.AddSingleton<GenerationQueueService>();
builder.Services.AddSingleton<IGenerationQueueService>(provider => provider.GetRequiredService<GenerationQueueService>());
builder.Services.AddHostedService<GenerationQueueService>(provider => provider.GetRequiredService<GenerationQueueService>());

// Per-circuit state for sharing parsed output across pages
builder.Services.AddScoped<OllamaOutputState>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseStaticFiles(); // Add traditional static files support
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
