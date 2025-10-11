using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using VideoGenerationApp.Configuration;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Services;
using Xunit;
using VideoGenerationApp.Services.Generation;

namespace VideoGenerationApp.Tests.Services
{
    public class GenerationQueueServiceVideoPayloadTests
    {
        [Fact]
        public async Task QueueVideoGenerationAsync_SendsExpectedPromptJson()
        {
            // Arrange: mock HttpMessageHandler to validate payload posted to /prompt
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            //Assert.Fail("The line 'Assert.Fail' is not valid in C#. Did you mean 'Assert.True(false, ...)' or 'throw new Exception(...)'?");
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri != null &&
                        req.RequestUri.AbsolutePath.Equals("/prompt", StringComparison.OrdinalIgnoreCase)),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (req, _) =>
                {
                    // Read request JSON
                    var body = await req.Content!.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(body);

                    // Extract only the "prompt" object (client_id is random)
                    Assert.True(doc.RootElement.TryGetProperty("prompt", out var promptElement), "Request JSON missing 'prompt' property.");

                    // Load expected prompt from file
                    var expectedPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "expected_video_prompt.json");
                    Assert.True(File.Exists(expectedPath), $"Expected JSON file not found: {expectedPath}");
                    var expectedJson = await File.ReadAllTextAsync(expectedPath);
                    using var expectedDoc = JsonDocument.Parse(expectedJson);

                    // Compare prompt payloads deeply (order-insensitive for object properties)
                    var areEqual = JsonDeepEquals(promptElement, expectedDoc.RootElement);
                    
                    if (!areEqual)
                    {
                        // Save actual JSON to a file for debugging
                        var actualJsonFormatted = JsonSerializer.Serialize(promptElement, new JsonSerializerOptions { WriteIndented = true });
                        var actualPath = Path.Combine(AppContext.BaseDirectory, "actual_video_prompt.json");
                        await File.WriteAllTextAsync(actualPath, actualJsonFormatted);
                        
                        // Fail with detailed comparison
                        Assert.True(false,
                            $"Prompt JSON did not match expected format.\n\n" +
                            $"Expected JSON structure should contain nodes: 1,2,3,4,5,6 with specific class_types.\n" +
                            $"Actual JSON saved to: {actualPath}\n" +
                            $"Expected JSON from: {expectedPath}\n\n" +
                            $"First 1000 chars of actual:\n{actualJsonFormatted[..Math.Min(1000, actualJsonFormatted.Length)]}...\n\n" +
                            $"Expected JSON:\n{expectedJson}");
                    }

                    // Return a successful ComfyUI response
                    var response = new ComfyUIWorkflowResponse { prompt_id = "test-video-prompt-xyz" };
                    var responseJson = JsonSerializer.Serialize(response);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                    };
                });

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:8188")
            };

            // Real ComfyUIVideoService with mocked HttpClient
            var loggerVideoMock = new Mock<ILogger<ComfyUIVideoService>>();
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
            var settings = new ComfyUISettings
            {
                ApiUrl = "http://localhost:8188",
                TimeoutMinutes = 2,
                PollIntervalSeconds = 1
            };
            var opts = Options.Create(settings);
            var comfyVideoService = new ComfyUIVideoService(httpClient, loggerVideoMock.Object, envMock.Object, opts);

            // Scope factory that returns the video service
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(p => p.GetService(typeof(IComfyUIVideoService)))
                .Returns(comfyVideoService);

            // Also provide audio and image services if ever resolved (not used in this test)
            serviceProviderMock
                .Setup(p => p.GetService(typeof(IComfyUIAudioService)))
                .Returns(new Mock<IComfyUIAudioService>().Object);
            serviceProviderMock
                .Setup(p => p.GetService(typeof(IComfyUIImageService)))
                .Returns(new Mock<IComfyUIImageService>().Object);

            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            scopeFactoryMock.Setup(f => f.CreateScope()).Returns(scopeMock.Object);

            var queueLoggerMock = new Mock<ILogger<GenerationQueueService>>();
            var queueService = new GenerationQueueService(new GenerationServiceFactory(serviceProviderMock.Object), queueLoggerMock.Object);

            // Deterministic config to match expected fixture
            var config = new VideoWorkflowConfig
            {
                ImageFilePath = "input.png",
                Width = 512,
                Height = 512,
                DurationSeconds = 2.0f,
                Fps = 24,
                MotionIntensity = 0.5f,
                Seed = 12345,
                OutputFilename = "video/ComfyUI",
                CheckpointName = "svd_xt.safetensors",
                CFGScale = 7.0f,
                Steps = 20,
                AugmentationLevel = 0.0f
            };

            // Act
            var taskId = await queueService.QueueVideoGenerationAsync("Payload Match Test", config);

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(taskId));
            // The main assertion is inside the handler (payload equality). If it didn’t throw, payload matched.
        }

        private static bool JsonDeepEquals(JsonElement a, JsonElement b)
        {
            if (a.ValueKind != b.ValueKind)
                return false;

            switch (a.ValueKind)
            {
                case JsonValueKind.Object:
                    {
                        var aProps = a.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                        var bProps = b.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                        foreach (var kv in bProps)
                        {
                            if (!aProps.TryGetValue(kv.Key, out var bv))
                            { 
                                Assert.Fail($"Property '{kv.Key}' not found in second object. Expected value:{kv.Value}");
                            }
                            if (!JsonDeepEquals(kv.Value, bv))
                            {
                                Assert.Fail($"Property '{kv.Value}' values do not match in {kv.Key}");
                            }
                        }
                        return true;
                    }
                case JsonValueKind.Array:
                    {
                        var aEnum = a.EnumerateArray().ToArray();
                        var bEnum = b.EnumerateArray().ToArray();
                        if (aEnum.Length != bEnum.Length)
                            return false;
                        for (int i = 0; i < aEnum.Length; i++)
                        {
                            if (!JsonDeepEquals(aEnum[i], bEnum[i]))
                                return false;
                        }
                        return true;
                    }
                case JsonValueKind.String:
                    return a.GetString() == b.GetString();
                case JsonValueKind.Number:
                    return a.GetRawText() == b.GetRawText();
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return a.GetBoolean() == b.GetBoolean();
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return true;
                default:
                    return a.GetRawText() == b.GetRawText();
            }
        }
    }
}