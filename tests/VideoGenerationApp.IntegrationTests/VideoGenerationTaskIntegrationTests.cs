using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using ComfyUI.Client.Models.Responses;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Services;
using VideoGenerationApp.Configuration;
using ComfyUI.Client.Services;

namespace VideoGenerationApp.IntegrationTests
{
    /// <summary>
    /// Integration tests for the VideoGenerationTask workflow creation
    /// Tests that the workflow dictionary is correctly generated from UI parameters
    /// </summary>
    public class VideoGenerationTaskIntegrationTests
    {
        private readonly Mock<IComfyUIApiClient> _mockComfyUIClient;
        private readonly Mock<IWebHostEnvironment> _mockWebHostEnvironment;
        private readonly Mock<IComfyUIVideoService> _mockVideoService;
        private readonly Mock<ILogger<VideoGenerationTask>> _mockLogger;

        public VideoGenerationTaskIntegrationTests()
        {
            _mockComfyUIClient = new Mock<IComfyUIApiClient>();
            _mockWebHostEnvironment = new Mock<IWebHostEnvironment>();
            _mockVideoService = new Mock<IComfyUIVideoService>();
            _mockLogger = new Mock<ILogger<VideoGenerationTask>>();

            // Setup basic mocks
            _mockWebHostEnvironment.Setup(x => x.WebRootPath).Returns("/test/wwwroot");
        }

        private string CreateUniqueTestFile(string extension = ".jpg")
        {
            var fileName = $"test_image_{Guid.NewGuid():N}{extension}";
            var testImagePath = Path.Combine(Path.GetTempPath(), fileName);
            
            if (extension == ".jpg")
                File.WriteAllBytes(testImagePath, new byte[] { 0xFF, 0xD8, 0xFF }); // Basic JPEG header
            else
                File.WriteAllText(testImagePath, $"dummy {extension} content");
                
            return testImagePath;
        }

        [Fact]
        public async Task VideoGenerationTask_CreatesCorrectWorkflowDictionary_WithBasicParameters()
        {
            // Arrange
            var testImagePath = CreateUniqueTestFile();
            
            var wrapper = new VideoWorkflowWrapper
            {
                TextPrompt = "A beautiful sunset over mountains",
                NegativePrompt = "blurry, low quality",
                ModelSet = "WAN_2_2_4Steps",
                Width = 640,
                Height = 640,
                Steps = 4,
                CFGScale = 1.0f,
                SamplerName = "uni_pc",
                Scheduler = "simple",
                Seed = 12345,
                ChunkLength = 77,
                VideoSegments = 2,
                Fps = 16,
                ModelSamplingShift = 8.0f,
                LoRAStrength = 1.0f,
                OutputFilename = "test_video",
                VideoCodec = "h264",
                OutputFormat = "mp4",
                ImageFilePath = testImagePath
            };

            // Mock successful image upload
            _mockComfyUIClient
                .Setup(x => x.UploadFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UploadResponse { Name = "uploaded_image.jpg" });

            Dictionary<string, object>? capturedWorkflow = null;
            
            _mockVideoService
                .Setup(x => x.SubmitWorkflowAsync(It.IsAny<Dictionary<string, object>>()))
                .Callback<object>(workflow => capturedWorkflow = workflow as Dictionary<string, object>)
                .ReturnsAsync("test-prompt-id");

            var task = new VideoGenerationTask(wrapper, _mockVideoService.Object, _mockComfyUIClient.Object, _mockWebHostEnvironment.Object);

            try
            {
                // Act
                await task.SubmitAsync();

                // Assert
                Assert.NotNull(capturedWorkflow);
            }
            finally
            {
                // Cleanup
                if (File.Exists(testImagePath))
                    File.Delete(testImagePath);
            }
            
            // Verify UNet loader
            Assert.True(capturedWorkflow.ContainsKey("1"));
            var unetNode = capturedWorkflow["1"] as Dictionary<string, object>;
            Assert.Equal("UNETLoader", unetNode!["class_type"]);
            var unetInputs = unetNode["inputs"] as Dictionary<string, object>;
            Assert.Equal("wan2.2_s2v_14B_fp8_scaled.safetensors", unetInputs!["unet_name"]);

            // Verify CLIP loader
            Assert.True(capturedWorkflow.ContainsKey("2"));
            var clipNode = capturedWorkflow["2"] as Dictionary<string, object>;
            Assert.Equal("CLIPLoader", clipNode!["class_type"]);
            var clipInputs = clipNode["inputs"] as Dictionary<string, object>;
            Assert.Equal("umt5_xxl_fp8_e4m3fn_scaled.safetensors", clipInputs!["clip_name"]);
            Assert.Equal("wan", clipInputs["type"]);

            // Verify VAE loader
            Assert.True(capturedWorkflow.ContainsKey("3"));
            var vaeNode = capturedWorkflow["3"] as Dictionary<string, object>;
            Assert.Equal("VAELoader", vaeNode!["class_type"]);
            var vaeInputs = vaeNode["inputs"] as Dictionary<string, object>;
            Assert.Equal("wan_2.1_vae.safetensors", vaeInputs!["vae_name"]);

            // Verify positive prompt encoding
            Assert.True(capturedWorkflow.ContainsKey("8"));
            var posNode = capturedWorkflow["8"] as Dictionary<string, object>;
            Assert.Equal("CLIPTextEncode", posNode!["class_type"]);
            var posInputs = posNode["inputs"] as Dictionary<string, object>;
            Assert.Equal("A beautiful sunset over mountains", posInputs!["text"]);

            // Verify negative prompt encoding
            Assert.True(capturedWorkflow.ContainsKey("9"));
            var negNode = capturedWorkflow["9"] as Dictionary<string, object>;
            Assert.Equal("CLIPTextEncode", negNode!["class_type"]);
            var negInputs = negNode["inputs"] as Dictionary<string, object>;
            Assert.Equal("blurry, low quality", negInputs!["text"]);

            // Verify LoRA loader (should be present for WAN_2_2_4Steps)
            Assert.True(capturedWorkflow.ContainsKey("10"));
            var loraNode = capturedWorkflow["10"] as Dictionary<string, object>;
            Assert.Equal("LoraLoaderModelOnly", loraNode!["class_type"]);
            var loraInputs = loraNode["inputs"] as Dictionary<string, object>;
            Assert.Equal("wan2.2_t2v_lightx2v_4steps_lora_v1.1_high_noise.safetensors", loraInputs!["lora_name"]);
            Assert.Equal(1.0f, loraInputs["strength_model"]);

            // Verify Model Sampling
            Assert.True(capturedWorkflow.ContainsKey("11"));
            var samplingNode = capturedWorkflow["11"] as Dictionary<string, object>;
            Assert.Equal("ModelSamplingSD3", samplingNode!["class_type"]);
            var samplingInputs = samplingNode["inputs"] as Dictionary<string, object>;
            Assert.Equal(8.0f, samplingInputs!["shift"]);

            // Verify WanSoundImageToVideo node
            Assert.True(capturedWorkflow.ContainsKey("12"));
            var wanNode = capturedWorkflow["12"] as Dictionary<string, object>;
            Assert.Equal("WanSoundImageToVideo", wanNode!["class_type"]);
            var wanInputs = wanNode["inputs"] as Dictionary<string, object>;
            Assert.Equal(640, wanInputs!["width"]);
            Assert.Equal(640, wanInputs["height"]);
            Assert.Equal(77, wanInputs["length"]);
            Assert.Equal(1, wanInputs["batch_size"]);

            // Verify KSampler
            Assert.True(capturedWorkflow.ContainsKey("13"));
            var samplerNode = capturedWorkflow["13"] as Dictionary<string, object>;
            Assert.Equal("KSampler", samplerNode!["class_type"]);
            var samplerInputs = samplerNode["inputs"] as Dictionary<string, object>;
            Assert.Equal(12345, samplerInputs!["seed"]);
            Assert.Equal("fixed", samplerInputs["control_after_generate"]);
            Assert.Equal(4, samplerInputs["steps"]);
            Assert.Equal(1.0f, samplerInputs["cfg"]);
            Assert.Equal("uni_pc", samplerInputs["sampler_name"]);
            Assert.Equal("simple", samplerInputs["scheduler"]);
            Assert.Equal(1.0f, samplerInputs["denoise"]);

            // Verify VAE Decode
            Assert.True(capturedWorkflow.ContainsKey("14"));
            var decodeNode = capturedWorkflow["14"] as Dictionary<string, object>;
            Assert.Equal("VAEDecode", decodeNode!["class_type"]);

            // Verify Create Video
            Assert.True(capturedWorkflow.ContainsKey("15"));
            var videoNode = capturedWorkflow["15"] as Dictionary<string, object>;
            Assert.Equal("CreateVideo", videoNode!["class_type"]);
            var videoInputs = videoNode["inputs"] as Dictionary<string, object>;
            Assert.Equal(16, videoInputs!["fps"]);

            // Verify Save Video
            Assert.True(capturedWorkflow.ContainsKey("16"));
            var saveNode = capturedWorkflow["16"] as Dictionary<string, object>;
            Assert.Equal("SaveVideo", saveNode!["class_type"]);
            var saveInputs = saveNode["inputs"] as Dictionary<string, object>;
            Assert.Equal("test_video", saveInputs!["filename_prefix"]);
            Assert.Equal("h264", saveInputs["codec"]);
            Assert.Equal("mp4", saveInputs["format"]);
        }

        [Fact]
        public async Task VideoGenerationTask_FailsCorrectly_WhenNoImageProvided()
        {
            // Arrange
            var wrapper = new VideoWorkflowWrapper
            {
                TextPrompt = "A beautiful sunset",
                ImageFilePath = "", // No image provided
            };

            var task = new VideoGenerationTask(wrapper, _mockVideoService.Object, _mockComfyUIClient.Object, _mockWebHostEnvironment.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => task.SubmitAsync());
            Assert.Contains("An input image is required for video generation", exception.Message);
        }

        [Fact]
        public async Task VideoGenerationTask_HandlesAudioCorrectly_WhenAudioProvided()
        {
            // Arrange
            var testAudioPath = CreateUniqueTestFile(".wav");
            var testImagePath = CreateUniqueTestFile(".jpg");
            
            var wrapper = new VideoWorkflowWrapper
            {
                TextPrompt = "A beautiful sunset",
                AudioFilePath = testAudioPath,
                ImageFilePath = testImagePath
            };

            // Mock successful uploads (image first, then audio)
            _mockComfyUIClient
                .SetupSequence(x => x.UploadFileAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UploadResponse { Name = "uploaded_image.jpg" })
                .ReturnsAsync(new UploadResponse { Name = "uploaded_audio.wav" });

            Dictionary<string, object>? capturedWorkflow = null;
            
            _mockVideoService
                .Setup(x => x.SubmitWorkflowAsync(It.IsAny<Dictionary<string, object>>()))
                .Callback<object>(workflow => capturedWorkflow = workflow as Dictionary<string, object>)
                .ReturnsAsync("test-prompt-id");

            var task = new VideoGenerationTask(wrapper, _mockVideoService.Object, _mockComfyUIClient.Object, _mockWebHostEnvironment.Object);

            try
            {
                // Act
                await task.SubmitAsync();

                // Assert
                Assert.NotNull(capturedWorkflow);
                
                // Verify audio encoder loader is present
                Assert.True(capturedWorkflow.ContainsKey("4"));
                var audioEncoderNode = capturedWorkflow["4"] as Dictionary<string, object>;
                Assert.Equal("AudioEncoderLoader", audioEncoderNode!["class_type"]);

                // Verify load audio node is present
                Assert.True(capturedWorkflow.ContainsKey("5"));
                var loadAudioNode = capturedWorkflow["5"] as Dictionary<string, object>;
                Assert.Equal("LoadAudio", loadAudioNode!["class_type"]);

                // Verify audio encoder encode node is present
                Assert.True(capturedWorkflow.ContainsKey("7"));
                var encodeAudioNode = capturedWorkflow["7"] as Dictionary<string, object>;
                Assert.Equal("AudioEncoderEncode", encodeAudioNode!["class_type"]);

                // Verify WanSoundImageToVideo includes audio encoder output
                Assert.True(capturedWorkflow.ContainsKey("12"));
                var wanNode = capturedWorkflow["12"] as Dictionary<string, object>;
                var wanInputs = wanNode!["inputs"] as Dictionary<string, object>;
                Assert.True(wanInputs!.ContainsKey("audio_encoder_output"));

                // Verify CreateVideo includes audio
                Assert.True(capturedWorkflow.ContainsKey("15"));
                var videoNode = capturedWorkflow["15"] as Dictionary<string, object>;
                var videoInputs = videoNode!["inputs"] as Dictionary<string, object>;
                Assert.True(videoInputs!.ContainsKey("audio"));
            }
            finally
            {
                // Cleanup
                if (File.Exists(testAudioPath))
                    File.Delete(testAudioPath);
                if (File.Exists(testImagePath))
                    File.Delete(testImagePath);
            }
        }
    }
}