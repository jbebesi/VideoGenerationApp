using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using VideoGenerationApp.Configuration;
using VideoGenerationApp.Dto;
using VideoGenerationApp.Services;
using VideoGenerationApp.Services.Generation;
using Xunit;

namespace VideoGenerationApp.Tests.Services
{
    public class GenerationQueueServiceVideoPayloadTests
    {
        [Fact]
        public async Task GenerationQueueService_CanBeCreated()
        {
            // Test demonstrates the new simplified architecture where the queue service
            // only manages tasks and doesn't depend on specific generation services
            
            var queueLoggerMock = new Mock<ILogger<GenerationQueueService>>();
            var queueService = new GenerationQueueService(queueLoggerMock.Object);

            // Test that we can create the queue service successfully
            Assert.NotNull(queueService);
            
            // Test that the service implements the expected interface
            Assert.IsAssignableFrom<IGenerationQueueService>(queueService);
            Assert.IsAssignableFrom<IHostedService>(queueService);
        }

        [Fact]
        public async Task GeneratedFileService_CanLoadFiles()
        {
            // Test the new file service that handles file loading responsibilities
            
            var environmentMock = new Mock<IWebHostEnvironment>();
            var loggerMock = new Mock<ILogger<GeneratedFileService>>();
            
            // Setup a temporary directory for testing
            var tempDir = Path.GetTempPath();
            var testDir = Path.Combine(tempDir, "test_wwwroot");
            Directory.CreateDirectory(testDir);
            
            try
            {
                environmentMock.Setup(e => e.WebRootPath).Returns(testDir);
                
                var fileService = new GeneratedFileService(environmentMock.Object, loggerMock.Object);
                
                // Test that we can create the file service successfully
                Assert.NotNull(fileService);
                
                // Test loading files from empty directories
                var audioFiles = await fileService.GetAudioFilesAsync();
                var imageFiles = await fileService.GetImageFilesAsync();
                var videoFiles = await fileService.GetVideoFilesAsync();
                
                Assert.NotNull(audioFiles);
                Assert.NotNull(imageFiles);
                Assert.NotNull(videoFiles);
                
                // Should return empty lists when directories don't exist
                Assert.Empty(audioFiles);
                Assert.Empty(imageFiles);
                Assert.Empty(videoFiles);
            }
            finally
            {
                // Clean up test directory
                if (Directory.Exists(testDir))
                {
                    Directory.Delete(testDir, true);
                }
            }
        }
    }
}