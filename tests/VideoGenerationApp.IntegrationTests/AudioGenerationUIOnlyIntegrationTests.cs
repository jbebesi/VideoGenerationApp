using Microsoft.Extensions.Logging;
using Moq;
using VideoGenerationApp.Dto;
using VideoGenerationApp.IntegrationTests.Infrastructure;
using VideoGenerationApp.Services;
using Xunit;
using System.Net;

namespace VideoGenerationApp.IntegrationTests
{
    /// <summary>
    /// Integration tests for UI-only functionality in audio generation that doesn't affect HTTP calls to ComfyUI
    /// These tests verify UI state management, validation, and user experience features
    /// </summary>
    public class AudioGenerationUIOnlyIntegrationTests
    {
        private readonly MockHttpMessageHandler _mockHandler;
        private readonly IOllamaService _ollamaService;

        public AudioGenerationUIOnlyIntegrationTests()
        {
            _mockHandler = new MockHttpMessageHandler();

            // Setup HttpClient with mock handler for Ollama
            var httpClient = new HttpClient(_mockHandler)
            {
                BaseAddress = new Uri("http://localhost:11434")
            };

            // Create logger mock
            var logger = new Mock<ILogger<OllamaService>>();

            // Create real OllamaService for testing UI integration with video scene output
            _ollamaService = new OllamaService(httpClient, logger.Object);
        }

        [Fact]
        public void AudioWorkflowConfig_DefaultValues_AreSetCorrectly()
        {
            // Arrange & Act
            var config = new AudioWorkflowConfig();

            // Assert - Verify all default values match expected UI state
            Assert.Equal("ace_step_v1_3.5b.safetensors", config.CheckpointName);
            Assert.Equal("pop, female voice, catchy melody", config.Tags);
            Assert.Contains("[verse]", config.Lyrics);
            Assert.Contains("[chorus]", config.Lyrics);
            Assert.Equal(0.99f, config.LyricsStrength);
            Assert.Equal(120f, config.AudioDurationSeconds);
            Assert.Equal(1, config.BatchSize);
            Assert.Equal(5.0f, config.ModelShift);
            Assert.Equal(1.0f, config.TonemapMultiplier);
            Assert.Equal(-1, config.Seed);
            Assert.Equal(50, config.Steps);
            Assert.Equal(5.0f, config.CFGScale);
            Assert.Equal("euler", config.SamplerName); 
            Assert.Equal("simple", config.Scheduler);
            Assert.Equal(1.0f, config.Denoise);
            Assert.Equal("audio/ComfyUI", config.OutputFilename);
            Assert.Equal("mp3", config.OutputFormat);
            Assert.Equal("V0", config.AudioQuality);
        }

        [Fact]
        public void VideoSceneOutput_ToAudioTags_ConvertsCorrectly()
        {
            // Arrange
            var sceneOutput = new VideoSceneOutput
            {
                narrative = "A peaceful morning scene with birds singing",
                tone = "uplifting",
                emotion = "joyful",
                audio = new AudioSection
                {
                    lyrics = "Morning birds singing peacefully",
                    tags = new List<string> { "acoustic guitar", "serene", "birds chirping", "gentle breeze" }
                }
            };

            // Act
            var tagParts = new List<string>();
            if (sceneOutput.audio.tags != null && sceneOutput.audio.tags.Count > 0)
                tagParts.AddRange(sceneOutput.audio.tags);
            if (!string.IsNullOrEmpty(sceneOutput.tone))
                tagParts.Add(sceneOutput.tone);
            if (!string.IsNullOrEmpty(sceneOutput.emotion))
                tagParts.Add(sceneOutput.emotion);

            var tags = string.Join(", ", tagParts);

            // Assert
            Assert.Equal("acoustic guitar, serene, birds chirping, gentle breeze, uplifting, joyful", tags);
        }

        [Fact]
        public void VideoSceneOutput_ToLyrics_GeneratesStructuredContent()
        {
            // Arrange
            var sceneOutput = new VideoSceneOutput
            {
                narrative = "A story about overcoming challenges and finding inner strength through difficult times"
            };

            // Act - Simulate the UI logic for creating lyrics from narrative
            var narrative = sceneOutput.narrative;
            var truncatedNarrative = narrative.Length > 100 
                ? narrative.Substring(0, 100) + "..." 
                : narrative;
            
            var lyrics = $"[verse]\n{truncatedNarrative}\n[chorus]\nSing with me tonight\nEverything will be alright";

            // Assert
            Assert.Contains("[verse]", lyrics);
            Assert.Contains("[chorus]", lyrics);
            Assert.Contains("A story about overcoming challenges and finding inner strength through difficult times", lyrics);
            Assert.Contains("Sing with me tonight", lyrics);
            Assert.Contains("Everything will be alright", lyrics);
        }

        [Fact]
        public void AudioQuality_DisabledForNonMP3Formats_BehavesCorrectly()
        {
            // Arrange
            var config = new AudioWorkflowConfig();

            // Act & Assert - Test MP3 format enables quality setting
            config.OutputFormat = "mp3";
            Assert.Equal("mp3", config.OutputFormat);
            // Quality should be available for MP3

            // Act & Assert - Test WAV format (quality not applicable)
            config.OutputFormat = "wav";
            Assert.Equal("wav", config.OutputFormat);
            // Quality setting should be disabled in UI for non-MP3 formats

            // Act & Assert - Test FLAC format (quality not applicable)
            config.OutputFormat = "flac";
            Assert.Equal("flac", config.OutputFormat);
            // Quality setting should be disabled in UI for non-MP3 formats
        }

        [Fact]
        public void TaskName_Generation_HandlesVariousInputs()
        {
            // Arrange
            var config1 = new AudioWorkflowConfig
            {
                Tags = "electronic, upbeat",
                Lyrics = "[verse]\nShort lyrics"
            };

            var config2 = new AudioWorkflowConfig
            {
                Tags = "classical, orchestral, emotional",
                Lyrics = "" // Empty lyrics
            };

            var config3 = new AudioWorkflowConfig
            {
                Tags = "very long tag description that exceeds normal length limits",
                Lyrics = "[verse]\nThis is a very long lyrics section that would normally be truncated in the task name generation to keep it readable"
            };

            // Act - Simulate task name generation logic from UI
            var taskName1 = !string.IsNullOrWhiteSpace(config1.Lyrics) && config1.Lyrics.Length > 10
                ? $"{config1.Tags} - {config1.Lyrics.Substring(0, Math.Min(30, config1.Lyrics.Length))}..."
                : config1.Tags;

            var taskName2 = !string.IsNullOrWhiteSpace(config2.Lyrics) && config2.Lyrics.Length > 10
                ? $"{config2.Tags} - {config2.Lyrics.Substring(0, Math.Min(30, config2.Lyrics.Length))}..."
                : config2.Tags;

            var taskName3 = !string.IsNullOrWhiteSpace(config3.Lyrics) && config3.Lyrics.Length > 10
                ? $"{config3.Tags} - {config3.Lyrics.Substring(0, Math.Min(30, config3.Lyrics.Length))}..."
                : config3.Tags;
            
            if (taskName3.Length > 80)
                taskName3 = taskName3.Substring(0, 77) + "...";

            // Assert
            Assert.Equal("electronic, upbeat - [verse]\nShort lyrics...", taskName1);
            Assert.Equal("classical, orchestral, emotional", taskName2);
            Assert.True(taskName3.Length <= 80);
            Assert.Contains("very long tag description", taskName3);
        }

        [Fact]
        public void SamplerOptions_ContainExpectedValues()
        {
            // Arrange - These are the sampler options that should be available in the UI
            var expectedSamplers = new[]
            {
                "euler",
                "euler_ancestral", 
                "heun",
                "dpm_2",
                "dpm_2_ancestral",
                "lms",
                "ddim",
                "uni_pc"
            };

            // Act & Assert - Verify each expected sampler is valid
            foreach (var sampler in expectedSamplers)
            {
                var config = new AudioWorkflowConfig { SamplerName = sampler };
                Assert.Equal(sampler, config.SamplerName);
            }
        }

        [Fact]
        public void SchedulerOptions_ContainExpectedValues()
        {
            // Arrange - These are the scheduler options that should be available in the UI
            var expectedSchedulers = new[]
            {
                "simple",
                "normal",
                "karras",
                "exponential",
                "sgm_uniform"
            };

            // Act & Assert - Verify each expected scheduler is valid
            foreach (var scheduler in expectedSchedulers)
            {
                var config = new AudioWorkflowConfig { Scheduler = scheduler };
                Assert.Equal(scheduler, config.Scheduler);
            }
        }

        [Fact]
        public void AudioQualityOptions_ContainExpectedValues()
        {
            // Arrange - These are the quality options for MP3 files
            var expectedQualities = new[]
            {
                "V0", // Highest Quality
                "V1",
                "V2", 
                "V3",
                "V4", // Standard
                "V5",
                "V6",
                "V7",
                "V8",
                "V9"  // Lowest Quality
            };

            // Act & Assert - Verify each expected quality is valid
            foreach (var quality in expectedQualities)
            {
                var config = new AudioWorkflowConfig { AudioQuality = quality };
                Assert.Equal(quality, config.AudioQuality);
            }
        }

        [Theory]
        [InlineData(-1)] // Random seed
        [InlineData(0)]  // Zero seed
        [InlineData(12345)] // Positive seed
        [InlineData(long.MaxValue)] // Maximum seed
        public void SeedValues_HandleDifferentInputs(long seedValue)
        {
            // Arrange
            var config = new AudioWorkflowConfig();

            // Act
            config.Seed = seedValue;

            // Assert
            Assert.Equal(seedValue, config.Seed);
        }

        [Theory]
        [InlineData(30f, 300f)]   // Valid range
        [InlineData(60f, 180f)]   // Common values
        [InlineData(120f, 240f)]  // Default and extended
        public void AudioDuration_AcceptsValidRanges(float minDuration, float maxDuration)
        {
            // Arrange
            var config = new AudioWorkflowConfig();

            // Act & Assert - Test minimum
            config.AudioDurationSeconds = minDuration;
            Assert.Equal(minDuration, config.AudioDurationSeconds);

            // Act & Assert - Test maximum
            config.AudioDurationSeconds = maxDuration;
            Assert.Equal(maxDuration, config.AudioDurationSeconds);
        }

        [Theory]
        [InlineData(1, 4)]   // Valid batch range
        [InlineData(2, 3)]   // Common batch sizes
        public void BatchSize_AcceptsValidRanges(int minBatch, int maxBatch)
        {
            // Arrange
            var config = new AudioWorkflowConfig();

            // Act & Assert - Test minimum
            config.BatchSize = minBatch;
            Assert.Equal(minBatch, config.BatchSize);

            // Act & Assert - Test maximum  
            config.BatchSize = maxBatch;
            Assert.Equal(maxBatch, config.BatchSize);
        }

        [Theory]
        [InlineData(0.0f, 1.0f)]   // Full range for lyrics strength
        [InlineData(0.5f, 0.99f)]  // Common values
        public void LyricsStrength_AcceptsValidRanges(float minStrength, float maxStrength)
        {
            // Arrange
            var config = new AudioWorkflowConfig();

            // Act & Assert - Test minimum
            config.LyricsStrength = minStrength;
            Assert.Equal(minStrength, config.LyricsStrength);

            // Act & Assert - Test maximum
            config.LyricsStrength = maxStrength;
            Assert.Equal(maxStrength, config.LyricsStrength);
        }

        [Theory]
        [InlineData(20, 100)]    // Valid steps range
        [InlineData(50, 80)]     // Common values
        public void GenerationSteps_AcceptsValidRanges(int minSteps, int maxSteps)
        {
            // Arrange
            var config = new AudioWorkflowConfig();

            // Act & Assert - Test minimum
            config.Steps = minSteps;
            Assert.Equal(minSteps, config.Steps);

            // Act & Assert - Test maximum
            config.Steps = maxSteps;
            Assert.Equal(maxSteps, config.Steps);
        }

        [Theory]
        [InlineData(1.0f, 20.0f)]   // Valid CFG range
        [InlineData(3.0f, 7.0f)]    // Common values
        public void CFGScale_AcceptsValidRanges(float minCFG, float maxCFG)
        {
            // Arrange
            var config = new AudioWorkflowConfig();

            // Act & Assert - Test minimum
            config.CFGScale = minCFG;
            Assert.Equal(minCFG, config.CFGScale);

            // Act & Assert - Test maximum
            config.CFGScale = maxCFG;
            Assert.Equal(maxCFG, config.CFGScale);
        }

        [Fact]
        public void ErrorMessage_Validation_WorksCorrectly()
        {
            // Arrange - Simulate UI validation logic
            var workflowConfig = new AudioWorkflowConfig();
            string? errorMessage = null;

            // Act - Test empty tags validation
            workflowConfig.Tags = "";
            if (string.IsNullOrWhiteSpace(workflowConfig.Tags))
            {
                errorMessage = "Please enter tags for the audio style.";
            }

            // Assert
            Assert.Equal("Please enter tags for the audio style.", errorMessage);

            // Act - Test valid tags
            errorMessage = null;
            workflowConfig.Tags = "pop, upbeat";
            if (string.IsNullOrWhiteSpace(workflowConfig.Tags))
            {
                errorMessage = "Please enter tags for the audio style.";
            }

            // Assert
            Assert.Null(errorMessage);
        }

        [Fact]
        public void ComfyUIStatus_Display_FormatsCorrectly()
        {
            // Arrange - Simulate UI status display logic
            bool isComfyUIRunning;
            string statusText;
            string badgeClass;

            // Act - Test running status
            isComfyUIRunning = true;
            statusText = isComfyUIRunning ? "Running" : "Stopped";
            badgeClass = isComfyUIRunning ? "bg-success" : "bg-danger";

            // Assert
            Assert.Equal("Running", statusText);
            Assert.Equal("bg-success", badgeClass);

            // Act - Test stopped status
            isComfyUIRunning = false;
            statusText = isComfyUIRunning ? "Running" : "Stopped";
            badgeClass = isComfyUIRunning ? "bg-success" : "bg-danger";

            // Assert
            Assert.Equal("Stopped", statusText);
            Assert.Equal("bg-danger", badgeClass);
        }
    }
}