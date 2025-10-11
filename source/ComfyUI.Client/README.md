# ComfyUI .NET Client

A comprehensive .NET HTTP client library for interacting with the ComfyUI API. This library provides strongly-typed models, automatic JSON validation, and easy-to-use methods for all ComfyUI endpoints.

## Features

- ✅ **Complete API Coverage**: All ComfyUI REST endpoints including main API and internal endpoints
- ✅ **JSON Validation**: Automatic request validation using FluentValidation
- ✅ **Strongly Typed**: Full C# models for all requests and responses
- ✅ **Dependency Injection**: Built-in support for Microsoft.Extensions.DependencyInjection
- ✅ **Configuration**: Flexible configuration options
- ✅ **Error Handling**: Comprehensive error handling with custom exceptions
- ✅ **Async/Await**: Full async support with cancellation tokens

## Installation

```bash
# Add the project reference (when published as NuGet package)
dotnet add package ComfyUI.Client
```

## Quick Start

### 1. Configuration

```csharp
// appsettings.json
{
  "ComfyUIClient": {
    "BaseUrl": "http://localhost:8188",
    "TimeoutSeconds": 300,
    "UseApiPrefix": true
  }
}
```

### 2. Dependency Injection Setup

```csharp
using ComfyUI.Client.Extensions;

// In Program.cs or Startup.cs
services.AddComfyUIClient(configuration);

// Or with options
services.AddComfyUIClient(options =>
{
    options.BaseUrl = "http://localhost:8188";
    options.TimeoutSeconds = 300;
    options.UseApiPrefix = true;
});
```

### 3. Usage

```csharp
using ComfyUI.Client.Services;
using ComfyUI.Client.Models.Requests;

public class MyService
{
    private readonly IComfyUIApiClient _comfyUIClient;

    public MyService(IComfyUIApiClient comfyUIClient)
    {
        _comfyUIClient = comfyUIClient;
    }

    public async Task<string> SubmitWorkflowAsync()
    {
        // Submit a workflow prompt
        var promptRequest = new PromptRequest
        {
            Prompt = new Dictionary<string, object>
            {
                ["1"] = new
                {
                    class_type = "LoadImage",
                    inputs = new { image = "example.png" }
                }
            },
            ClientId = "my-client-id"
        };

        var response = await _comfyUIClient.SubmitPromptAsync(promptRequest);
        return response.PromptId;
    }

    public async Task<SystemStatsResponse> GetSystemInfoAsync()
    {
        return await _comfyUIClient.GetSystemStatsAsync();
    }

    public async Task<QueueResponse> GetQueueStatusAsync()
    {
        return await _comfyUIClient.GetQueueAsync();
    }
}
```

## API Coverage

### Main Endpoints

#### GET Endpoints
- `GET /embeddings` - Get list of embeddings
- `GET /models` - List all model types  
- `GET /models/{folder}` - Get models in specific folder
- `GET /extensions` - Get JavaScript extensions
- `GET /view` - View images (with query params)
- `GET /view_metadata/{folder_name}` - Get metadata for folder
- `GET /system_stats` - Get system statistics
- `GET /features` - Get server features
- `GET /prompt` - Get current queue info
- `GET /object_info` - Get all node object info
- `GET /object_info/{node_class}` - Get specific node class info
- `GET /history` - Get execution history
- `GET /history/{prompt_id}` - Get specific prompt history
- `GET /queue` - Get current queue status

#### POST Endpoints
- `POST /upload/image` - Upload image
- `POST /upload/mask` - Upload mask image
- `POST /prompt` - Submit workflow prompt
- `POST /queue` - Manage queue (clear/delete)
- `POST /interrupt` - Interrupt execution
- `POST /free` - Free memory/unload models
- `POST /history` - Manage history (clear/delete)

### Internal Endpoints

- `GET /internal/logs` - Get formatted logs
- `GET /internal/logs/raw` - Get raw logs with terminal size
- `PATCH /internal/logs/subscribe` - Subscribe/unsubscribe to logs
- `GET /internal/folder_paths` - Get folder paths configuration
- `GET /internal/files/{directory_type}` - Get files in directory

## Examples

### Submit a Complex Workflow

```csharp
var workflow = new Dictionary<string, object>
{
    ["1"] = new
    {
        class_type = "CheckpointLoaderSimple",
        inputs = new { ckpt_name = "model.safetensors" }
    },
    ["2"] = new
    {
        class_type = "CLIPTextEncode",
        inputs = new 
        { 
            text = "a beautiful landscape",
            clip = new[] { "1", 1 }
        }
    }
    // ... more nodes
};

var request = new PromptRequest
{
    Prompt = workflow,
    ClientId = Guid.NewGuid().ToString()
};

var response = await client.SubmitPromptAsync(request);
Console.WriteLine($"Submitted workflow with ID: {response.PromptId}");
```

### Upload and Process Image

```csharp
// Upload image
var imageBytes = await File.ReadAllBytesAsync("input.jpg");
var uploadResponse = await client.UploadImageAsync(
    imageBytes, 
    "input.jpg", 
    type: "input"
);

Console.WriteLine($"Uploaded: {uploadResponse.Name}");

// Get image back
var retrievedImage = await client.GetImageAsync(
    uploadResponse.Name, 
    type: "input"
);

await File.WriteAllBytesAsync("downloaded.jpg", retrievedImage);
```

### Monitor Queue and System

```csharp
// Get system stats
var stats = await client.GetSystemStatsAsync();
Console.WriteLine($"VRAM Free: {stats.Devices[0].VramFree / 1024 / 1024} MB");

// Monitor queue
var queue = await client.GetQueueAsync();
Console.WriteLine($"Running: {queue.QueueRunning.Count}, Pending: {queue.QueuePending.Count}");

// Clear queue if needed
await client.ManageQueueAsync(new QueueRequest { Clear = true });
```

### Error Handling

```csharp
try
{
    var response = await client.SubmitPromptAsync(request);
}
catch (ComfyUIValidationException ex)
{
    Console.WriteLine($"Validation failed: {string.Join(", ", ex.ValidationErrors)}");
}
catch (ComfyUIApiException ex)
{
    Console.WriteLine($"API Error: {ex.Message}");
    if (ex.NodeErrors != null)
    {
        Console.WriteLine($"Node errors: {JsonSerializer.Serialize(ex.NodeErrors)}");
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Network error: {ex.Message}");
}
```

## Configuration Options

| Option | Default | Description |
|--------|---------|-------------|
| `BaseUrl` | `http://localhost:8188` | ComfyUI server URL |
| `TimeoutSeconds` | `300` | Request timeout in seconds |
| `UseApiPrefix` | `true` | Whether to use `/api` prefix for endpoints |

## Requirements

- .NET 8.0 or later
- ComfyUI server running and accessible

## License

This project is licensed under the same license as ComfyUI.