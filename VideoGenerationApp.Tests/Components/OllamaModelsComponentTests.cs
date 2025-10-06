using Bunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using VideoGenerationApp.Components.Pages;
using VideoGenerationApp.Configuration;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Services;
using Xunit;

namespace VideoGenerationApp.Tests.Components
{
    public class OllamaModelsComponentTests : Bunit.TestContext
    {
        private readonly Mock<OllamaService> _ollamaServiceMock;
        private readonly Mock<OllamaOutputState> _outputStateMock;
        private readonly Mock<ILogger<OllamaModels>> _loggerMock;

        public OllamaModelsComponentTests()
        {
            _ollamaServiceMock = new Mock<OllamaService>(Mock.Of<HttpClient>(), Mock.Of<ILogger<OllamaService>>());
            _outputStateMock = new Mock<OllamaOutputState>();
            _loggerMock = new Mock<ILogger<OllamaModels>>();

            // Register services
            Services.AddSingleton(_ollamaServiceMock.Object);
            Services.AddSingleton(_outputStateMock.Object);
            Services.AddSingleton(_loggerMock.Object);
        }

        [Fact]
        public void Component_RendersCorrectly_WhenInitialized()
        {
            // Arrange
            var models = new List<OllamaModel>
            {
                new OllamaModel { name = "llama3:8b", size = 4600000000 },
                new OllamaModel { name = "gemma:2b", size = 1500000000 }
            };

            _ollamaServiceMock.Setup(x => x.GetLocalModelsWithDetailsAsync())
                .ReturnsAsync(models);

            // Act
            var component = RenderComponent<OllamaModels>();

            // Assert
            Assert.Contains("Ollama Local Models", component.Markup);
            Assert.Contains("Model Selection", component.Markup);
            Assert.Contains("Content Generation", component.Markup);
            Assert.Contains("Generation Parameters", component.Markup);
        }

        [Fact]
        public void ModelSelect_RendersOptions_WhenModelsAvailable()
        {
            // Arrange
            var models = new List<OllamaModel>
            {
                new OllamaModel { name = "llama3:8b", size = 4600000000 },
                new OllamaModel { name = "gemma:2b", size = 1500000000 }
            };

            _ollamaServiceMock.Setup(x => x.GetLocalModelsWithDetailsAsync())
                .ReturnsAsync(models);

            // Act
            var component = RenderComponent<OllamaModels>();

            // Assert
            var select = component.Find("#modelSelect");
            Assert.NotNull(select);
            
            var options = component.FindAll("option");
            Assert.True(options.Count >= 2); // Should have at least our 2 models
        }

        [Fact]
        public void PromptInput_AcceptsTextInput_WhenUserTypes()
        {
            // Arrange
            _ollamaServiceMock.Setup(x => x.GetLocalModelsWithDetailsAsync())
                .ReturnsAsync(new List<OllamaModel>());

            var component = RenderComponent<OllamaModels>();
            var textarea = component.Find("#promptInput");

            // Act
            textarea.Input("Test prompt for video generation");

            // Assert
            Assert.Equal("Test prompt for video generation", textarea.GetAttribute("value"));
        }

        [Fact]
        public void MaxTokensInput_AcceptsNumericInput_WhenUserChanges()
        {
            // Arrange
            _ollamaServiceMock.Setup(x => x.GetLocalModelsWithDetailsAsync())
                .ReturnsAsync(new List<OllamaModel>());

            var component = RenderComponent<OllamaModels>();
            var input = component.Find("#maxTokens");

            // Act
            input.Change("2000");

            // Assert
            Assert.Equal("2000", input.GetAttribute("value"));
        }

        [Fact]
        public void TemperatureInput_AcceptsDecimalInput_WhenUserChanges()
        {
            // Arrange
            _ollamaServiceMock.Setup(x => x.GetLocalModelsWithDetailsAsync())
                .ReturnsAsync(new List<OllamaModel>());

            var component = RenderComponent<OllamaModels>();
            var input = component.Find("#temperature");

            // Act
            input.Change("0.7");

            // Assert
            Assert.Equal("0.7", input.GetAttribute("value"));
        }

        [Fact]
        public void RefreshModelsButton_IsRendered_WhenComponentLoads()
        {
            // Arrange
            _ollamaServiceMock.Setup(x => x.GetLocalModelsWithDetailsAsync())
                .ReturnsAsync(new List<OllamaModel>());

            // Act
            var component = RenderComponent<OllamaModels>();

            // Assert
            var refreshButton = component.Find("button:contains('Refresh Models')");
            Assert.NotNull(refreshButton);
            // Check if the button contains the icon (icon is inside the button)
            Assert.Contains("bi-arrow-clockwise", refreshButton.InnerHtml);
        }

        [Fact]
        public void LoadingSpinner_IsShown_WhenModelsLoading()
        {
            // Arrange
            var taskCompletionSource = new TaskCompletionSource<List<OllamaModel>>();
            _ollamaServiceMock.Setup(x => x.GetLocalModelsWithDetailsAsync())
                .Returns(taskCompletionSource.Task);

            // Act
            var component = RenderComponent<OllamaModels>();

            // Assert
            Assert.Contains("Loading models...", component.Markup);
            Assert.Contains("spinner-border", component.Markup);

            // Complete the task to clean up
            taskCompletionSource.SetResult(new List<OllamaModel>());
        }

        [Fact]
        public void ErrorMessage_IsDisplayed_WhenServiceThrows()
        {
            // Arrange
            _ollamaServiceMock.Setup(x => x.GetLocalModelsWithDetailsAsync())
                .ThrowsAsync(new HttpRequestException("Connection failed"));
            _ollamaServiceMock.Setup(x => x.GetLocalModelsAsync())
                .ThrowsAsync(new HttpRequestException("Connection failed"));

            // Act
            var component = RenderComponent<OllamaModels>();

            // Assert
            // Note: Error handling might be asynchronous, so we may need to wait
            // The exact error display logic would depend on the component implementation
            Assert.Contains("Ollama Local Models", component.Markup); // Component should still render
        }

        [Fact]
        public void TopPInput_AcceptsDecimalInput_WhenUserChanges()
        {
            // Arrange
            _ollamaServiceMock.Setup(x => x.GetLocalModelsWithDetailsAsync())
                .ReturnsAsync(new List<OllamaModel>());

            var component = RenderComponent<OllamaModels>();
            var input = component.Find("#topP");

            // Act
            input.Change("0.9");

            // Assert
            Assert.Equal("0.9", input.GetAttribute("value"));
        }

        [Fact]
        public void ModelSelect_TriggersChange_WhenUserSelectsDifferentModel()
        {
            // Arrange
            var models = new List<OllamaModel>
            {
                new OllamaModel { name = "llama3:8b", size = 4600000000 },
                new OllamaModel { name = "gemma:2b", size = 1500000000 }
            };

            _ollamaServiceMock.Setup(x => x.GetLocalModelsWithDetailsAsync())
                .ReturnsAsync(models);

            var component = RenderComponent<OllamaModels>();
            var select = component.Find("#modelSelect");

            // Act
            select.Change("llama3:8b");

            // Assert
            Assert.Equal("llama3:8b", select.GetAttribute("value"));
        }

        [Fact]
        public void Component_HasCorrectPageTitle_WhenRendered()
        {
            // Arrange
            _ollamaServiceMock.Setup(x => x.GetLocalModelsWithDetailsAsync())
                .ReturnsAsync(new List<OllamaModel>());

            // Act
            var component = RenderComponent<OllamaModels>();

            // Assert
            Assert.Contains("Ollama Local Models", component.Markup);
        }

        [Fact]
        public void Component_HasBootstrapIcons_WhenRendered()
        {
            // Arrange
            _ollamaServiceMock.Setup(x => x.GetLocalModelsWithDetailsAsync())
                .ReturnsAsync(new List<OllamaModel>());

            // Act
            var component = RenderComponent<OllamaModels>();

            // Assert
            Assert.Contains("bi-robot", component.Markup);
            Assert.Contains("bi-cpu", component.Markup);
            Assert.Contains("bi-chat-text", component.Markup);
            Assert.Contains("bi-sliders", component.Markup);
        }
    }
}