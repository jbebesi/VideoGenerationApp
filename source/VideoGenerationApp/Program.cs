using VideoGenerationApp.Components;
using VideoGenerationApp.Services;
using VideoGenerationApp.Services.Generation;
using VideoGenerationApp.Configuration;
using VideoGenerationApp.Logging;
using Microsoft.Extensions.Options;
using VideoGenerationApp.Dto;
using ComfyUI.Client.Extensions;

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

builder.Services.AddComfyUIClient(builder.Configuration);

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

// Register ComfyUI services with the new client
builder.Services.AddScoped<IComfyUIFileService, ComfyUIFileService>();
builder.Services.AddScoped<IComfyUIAudioService, ComfyUIAudioService>();
builder.Services.AddScoped<IComfyUIImageService, ComfyUIImageService>();
builder.Services.AddScoped<IComfyUIVideoService, ComfyUIVideoService>();

// Register file services
builder.Services.AddScoped<IGeneratedFileService, GeneratedFileService>();

// Register Generation Workflow services (scoped) - these depend on the queue service
builder.Services.AddScoped<AudioGenerationWorkflow>();
builder.Services.AddScoped<ImageGenerationWorkflow>();
builder.Services.AddScoped<VideoGenerationWorkflow>();

// Register Generation Queue Service as singleton hosted service
builder.Services.AddSingleton<GenerationQueueService>();
builder.Services.AddSingleton<IGenerationQueueService>(provider => provider.GetRequiredService<GenerationQueueService>());
builder.Services.AddHostedService<GenerationQueueService>(provider => provider.GetRequiredService<GenerationQueueService>());

// Circuit-aware state for sharing parsed output across pages and navigation
builder.Services.AddSingleton<OllamaOutputState>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStatusCodePagesWithReExecute("/not-found");
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
