using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VideoGenerationApp.Configuration;
using VideoGenerationApp.Dto;
using VideoGenerationApp.IntegrationTests.Infrastructure;
using VideoGenerationApp.Services;
using VideoGenerationApp.Services.Generation;
using ComfyUI.Client.Configuration;
using ComfyUI.Client.Services;
using Xunit;

namespace VideoGenerationApp.IntegrationTests
{
    /// <summary>
    /// Integration tests for ImageGenerationWorkflow
    /// Tests all parameters from GenerateImage.razor UI component
    /// These tests verify that UI parameters are properly transmitted via HTTP requests to ComfyUI
    /// </summary>
    public class ImageGenerationWorkflowIntegrationTests
    {
        private readonly MockHttpMessageHandler _mockHandler;
        private readonly IComfyUIImageService _imageService;

        public ImageGenerationWorkflowIntegrationTests()
        {
            _mockHandler = new MockHttpMessageHandler();

            // Setup HttpClient with mock handler
            var httpClient = new HttpClient(_mockHandler)
            {
                BaseAddress = new Uri("http://localhost:8188")
            };

            // Setup ComfyUI client options
            var clientOptions = Options.Create(new ComfyUIClientOptions
            {
                BaseUrl = "http://localhost:8188",
                UseApiPrefix = false
            });

            // Setup ComfyUI settings
            var comfySettings = Options.Create(new ComfyUISettings
            {
                ApiUrl = "http://localhost:8188",
                TimeoutMinutes = 5
            });

            // Create real ComfyUIApiClient with mocked HttpClient
            var apiClient = new ComfyUIApiClient(httpClient, clientOptions);

            // Create logger mocks
            var imageServiceLogger = new Mock<ILogger<ComfyUIImageService>>();

            // Create mock environment
            var mockEnvironment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            mockEnvironment.Setup(x => x.ContentRootPath).Returns("/tmp");

            // Create mock file service
            var mockFileService = new Mock<IComfyUIFileService>();

            // Create real ComfyUIImageService
            _imageService = new ComfyUIImageService(
                apiClient,
                imageServiceLogger.Object,
                mockEnvironment.Object,
                comfySettings,
                mockFileService.Object
            );
        }

        /// <summary>
        /// Helper method to submit an image generation task and capture the HTTP request
        /// This simulates what happens when GenerateImage.razor calls ImageGenerationWorkflow.GenerateAsync
        /// </summary>
        private async Task<string?> SubmitImageGenerationTaskAsync(ImageWorkflowConfig config)
        {
            // This is what the workflow does internally - creates a task and submits it
            var task = new ImageGenerationTask(config, _imageService);
            return await task.SubmitAsync();
        }

        [Fact]
        public async Task GetAvailableModelsAsync_SendsCorrectHttpRequest()
        {
            // Arrange
            // Mock the response for /object_info which returns model information
            var objectInfoResponse = @"{
                ""CheckpointLoaderSimple"": {
                    ""input"": {
                        ""required"": {
                            ""ckpt_name"": [[""model1.safetensors"", ""model2.safetensors""]]
                        }
                    }
                }
            }";
            _mockHandler.EnqueueJsonResponse(objectInfoResponse);

            // Act
            var models = await _imageService.GetImageModelsAsync();

            // Assert
            Assert.NotNull(_mockHandler.LastRequest);
            Assert.Equal("/object_info", _mockHandler.LastRequest.RequestUri?.AbsolutePath);
            Assert.Equal(HttpMethod.Get, _mockHandler.LastRequest.Method);
            Assert.Equal(2, models.Count);
        }

        [Fact]
        public async Task GenerateAsync_WithPositivePrompt_IncludesInWorkflow()
        {
            // Arrange
            var config = new ImageWorkflowConfig
            {
                PositivePrompt = "beautiful sunset, high quality"
            };
            _mockHandler.EnqueueJsonResponse("{\"prompt_id\": \"test-123\"}");

            // Act
            await SubmitImageGenerationTaskAsync(config);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            Assert.NotNull(requestBody);
            Assert.Contains("beautiful sunset, high quality", requestBody);
        }

        [Fact]
        public async Task GenerateAsync_WithNegativePrompt_IncludesInWorkflow()
        {
            // Arrange
            var config = new ImageWorkflowConfig
            {
                NegativePrompt = "ugly, blurry, distorted"
            };
            _mockHandler.EnqueueJsonResponse("{\"prompt_id\": \"test-123\"}");

            // Act
            await SubmitImageGenerationTaskAsync(config);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            Assert.NotNull(requestBody);
            Assert.Contains("ugly, blurry, distorted", requestBody);
        }

        [Fact]
        public async Task GenerateAsync_WithCheckpointName_IncludesInWorkflow()
        {
            // Arrange
            var config = new ImageWorkflowConfig
            {
                CheckpointName = "sd_xl_base_1.0.safetensors"
            };
            _mockHandler.EnqueueJsonResponse("{\"prompt_id\": \"test-123\"}");

            // Act
            await SubmitImageGenerationTaskAsync(config);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            Assert.NotNull(requestBody);
            Assert.Contains("sd_xl_base_1.0.safetensors", requestBody);
        }

        [Fact]
        public async Task GenerateAsync_WithWidth_IncludesInWorkflow()
        {
            // Arrange
            var config = new ImageWorkflowConfig
            {
                Width = 1536
            };
            _mockHandler.EnqueueJsonResponse("{\"prompt_id\": \"test-123\"}");

            // Act
            await SubmitImageGenerationTaskAsync(config);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            Assert.NotNull(requestBody);
            // Width should appear in the workflow JSON
            var json = JsonDocument.Parse(requestBody);
            var promptElement = json.RootElement.GetProperty("prompt");
            
            // Find the EmptyLatentImage node (usually has width parameter)
            bool foundWidth = false;
            foreach (var node in promptElement.EnumerateObject())
            {
                if (node.Value.TryGetProperty("class_type", out var classType) && 
                    classType.GetString() == "EmptyLatentImage")
                {
                    var inputs = node.Value.GetProperty("inputs");
                    if (inputs.TryGetProperty("width", out var width))
                    {
                        Assert.Equal(1536, width.GetInt32());
                        foundWidth = true;
                    }
                }
            }
            Assert.True(foundWidth, "Width parameter not found in workflow");
        }

        [Fact]
        public async Task GenerateAsync_WithHeight_IncludesInWorkflow()
        {
            // Arrange
            var config = new ImageWorkflowConfig
            {
                Height = 2048
            };
            _mockHandler.EnqueueJsonResponse("{\"prompt_id\": \"test-123\"}");

            // Act
            await SubmitImageGenerationTaskAsync(config);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            Assert.NotNull(requestBody);
            
            var json = JsonDocument.Parse(requestBody);
            var promptElement = json.RootElement.GetProperty("prompt");
            
            bool foundHeight = false;
            foreach (var node in promptElement.EnumerateObject())
            {
                if (node.Value.TryGetProperty("class_type", out var classType) && 
                    classType.GetString() == "EmptyLatentImage")
                {
                    var inputs = node.Value.GetProperty("inputs");
                    if (inputs.TryGetProperty("height", out var height))
                    {
                        Assert.Equal(2048, height.GetInt32());
                        foundHeight = true;
                    }
                }
            }
            Assert.True(foundHeight, "Height parameter not found in workflow");
        }

        [Fact]
        public async Task GenerateAsync_WithSeed_IncludesInWorkflow()
        {
            // Arrange
            var config = new ImageWorkflowConfig
            {
                Seed = 42
            };
            _mockHandler.EnqueueJsonResponse("{\"prompt_id\": \"test-123\"}");

            // Act
            await SubmitImageGenerationTaskAsync(config);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            Assert.NotNull(requestBody);
            
            var json = JsonDocument.Parse(requestBody);
            var promptElement = json.RootElement.GetProperty("prompt");
            
            bool foundSeed = false;
            foreach (var node in promptElement.EnumerateObject())
            {
                if (node.Value.TryGetProperty("class_type", out var classType) && 
                    classType.GetString() == "KSampler")
                {
                    var inputs = node.Value.GetProperty("inputs");
                    if (inputs.TryGetProperty("seed", out var seed))
                    {
                        Assert.Equal(42, seed.GetInt64());
                        foundSeed = true;
                    }
                }
            }
            Assert.True(foundSeed, "Seed parameter not found in workflow");
        }

        [Fact]
        public async Task GenerateAsync_WithSteps_IncludesInWorkflow()
        {
            // Arrange
            var config = new ImageWorkflowConfig
            {
                Steps = 30
            };
            _mockHandler.EnqueueJsonResponse("{\"prompt_id\": \"test-123\"}");

            // Act
            await SubmitImageGenerationTaskAsync(config);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            Assert.NotNull(requestBody);
            
            var json = JsonDocument.Parse(requestBody);
            var promptElement = json.RootElement.GetProperty("prompt");
            
            bool foundSteps = false;
            foreach (var node in promptElement.EnumerateObject())
            {
                if (node.Value.TryGetProperty("class_type", out var classType) && 
                    classType.GetString() == "KSampler")
                {
                    var inputs = node.Value.GetProperty("inputs");
                    if (inputs.TryGetProperty("steps", out var steps))
                    {
                        Assert.Equal(30, steps.GetInt32());
                        foundSteps = true;
                    }
                }
            }
            Assert.True(foundSteps, "Steps parameter not found in workflow");
        }

        [Fact]
        public async Task GenerateAsync_WithCFGScale_IncludesInWorkflow()
        {
            // Arrange
            var config = new ImageWorkflowConfig
            {
                CFGScale = 8.5f
            };
            _mockHandler.EnqueueJsonResponse("{\"prompt_id\": \"test-123\"}");

            // Act
            await SubmitImageGenerationTaskAsync(config);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            Assert.NotNull(requestBody);
            
            var json = JsonDocument.Parse(requestBody);
            var promptElement = json.RootElement.GetProperty("prompt");
            
            bool foundCfg = false;
            foreach (var node in promptElement.EnumerateObject())
            {
                if (node.Value.TryGetProperty("class_type", out var classType) && 
                    classType.GetString() == "KSampler")
                {
                    var inputs = node.Value.GetProperty("inputs");
                    if (inputs.TryGetProperty("cfg", out var cfg))
                    {
                        Assert.Equal(8.5, cfg.GetDouble(), 0.01);
                        foundCfg = true;
                    }
                }
            }
            Assert.True(foundCfg, "CFG parameter not found in workflow");
        }

        [Fact]
        public async Task GenerateAsync_WithSamplerName_IncludesInWorkflow()
        {
            // Arrange
            var config = new ImageWorkflowConfig
            {
                SamplerName = "dpmpp_2m"
            };
            _mockHandler.EnqueueJsonResponse("{\"prompt_id\": \"test-123\"}");

            // Act
            await SubmitImageGenerationTaskAsync(config);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            Assert.NotNull(requestBody);
            
            var json = JsonDocument.Parse(requestBody);
            var promptElement = json.RootElement.GetProperty("prompt");
            
            bool foundSampler = false;
            foreach (var node in promptElement.EnumerateObject())
            {
                if (node.Value.TryGetProperty("class_type", out var classType) && 
                    classType.GetString() == "KSampler")
                {
                    var inputs = node.Value.GetProperty("inputs");
                    if (inputs.TryGetProperty("sampler_name", out var samplerName))
                    {
                        Assert.Equal("dpmpp_2m", samplerName.GetString());
                        foundSampler = true;
                    }
                }
            }
            Assert.True(foundSampler, "Sampler name not found in workflow");
        }

        [Fact]
        public async Task GenerateAsync_WithScheduler_IncludesInWorkflow()
        {
            // Arrange
            var config = new ImageWorkflowConfig
            {
                Scheduler = "karras"
            };
            _mockHandler.EnqueueJsonResponse("{\"prompt_id\": \"test-123\"}");

            // Act
            await SubmitImageGenerationTaskAsync(config);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            Assert.NotNull(requestBody);
            
            var json = JsonDocument.Parse(requestBody);
            var promptElement = json.RootElement.GetProperty("prompt");
            
            bool foundScheduler = false;
            foreach (var node in promptElement.EnumerateObject())
            {
                if (node.Value.TryGetProperty("class_type", out var classType) && 
                    classType.GetString() == "KSampler")
                {
                    var inputs = node.Value.GetProperty("inputs");
                    if (inputs.TryGetProperty("scheduler", out var scheduler))
                    {
                        Assert.Equal("karras", scheduler.GetString());
                        foundScheduler = true;
                    }
                }
            }
            Assert.True(foundScheduler, "Scheduler not found in workflow");
        }

        [Fact]
        public async Task GenerateAsync_WithDenoise_IncludesInWorkflow()
        {
            // Arrange
            var config = new ImageWorkflowConfig
            {
                Denoise = 0.85f
            };
            _mockHandler.EnqueueJsonResponse("{\"prompt_id\": \"test-123\"}");

            // Act
            await SubmitImageGenerationTaskAsync(config);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            Assert.NotNull(requestBody);
            
            var json = JsonDocument.Parse(requestBody);
            var promptElement = json.RootElement.GetProperty("prompt");
            
            bool foundDenoise = false;
            foreach (var node in promptElement.EnumerateObject())
            {
                if (node.Value.TryGetProperty("class_type", out var classType) && 
                    classType.GetString() == "KSampler")
                {
                    var inputs = node.Value.GetProperty("inputs");
                    if (inputs.TryGetProperty("denoise", out var denoise))
                    {
                        Assert.Equal(0.85, denoise.GetDouble(), 0.01);
                        foundDenoise = true;
                    }
                }
            }
            Assert.True(foundDenoise, "Denoise parameter not found in workflow");
        }

        [Fact]
        public async Task GenerateAsync_WithBatchSize_IncludesInWorkflow()
        {
            // Arrange
            var config = new ImageWorkflowConfig
            {
                BatchSize = 4
            };
            _mockHandler.EnqueueJsonResponse("{\"prompt_id\": \"test-123\"}");

            // Act
            await SubmitImageGenerationTaskAsync(config);

            // Assert
            var requestBody = await _mockHandler.GetRequestBodyAsync();
            Assert.NotNull(requestBody);
            
            var json = JsonDocument.Parse(requestBody);
            var promptElement = json.RootElement.GetProperty("prompt");
            
            bool foundBatchSize = false;
            foreach (var node in promptElement.EnumerateObject())
            {
                if (node.Value.TryGetProperty("class_type", out var classType) && 
                    classType.GetString() == "EmptyLatentImage")
                {
                    var inputs = node.Value.GetProperty("inputs");
                    if (inputs.TryGetProperty("batch_size", out var batchSize))
                    {
                        Assert.Equal(4, batchSize.GetInt32());
                        foundBatchSize = true;
                    }
                }
            }
            Assert.True(foundBatchSize, "Batch size not found in workflow");
        }

        // Note: OutputFilename and OutputFormat do not affect the HTTP request content to ComfyUI
        // They are used locally after receiving the response from ComfyUI
        // Therefore, no tests are needed for these parameters as per the requirements
    }
}
