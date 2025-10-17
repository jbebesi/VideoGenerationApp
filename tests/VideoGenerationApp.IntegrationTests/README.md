# VideoGenerationApp Integration Tests

This project contains integration tests for the VideoGenerationApp, testing all UI-bound workflow functions for Image, Video, and Audio generation.

## Overview

The integration tests verify that parameters from the UI components (GenerateImage.razor, GenerateVideo.razor, GenerateAudio.razor) are correctly transmitted via HTTP requests to the ComfyUI API.

## Test Approach

- **Starting Point**: Tests start from functions directly bound to the UI (workflow generation methods)
- **Mocking**: Only the `HttpMessageHandler` is mocked to intercept HTTP requests
- **Verification**: Tests verify all input values from the UI are properly included in the HTTP request messages to ComfyUI

## Test Coverage

### Image Generation Tests (13 tests)
Located in: `ImageGenerationWorkflowIntegrationTests.cs`

**UI Parameters Tested:**
- ✅ PositivePrompt
- ✅ NegativePrompt
- ✅ CheckpointName
- ✅ Width
- ✅ Height
- ✅ Seed
- ✅ Steps
- ✅ CFGScale
- ✅ SamplerName
- ✅ Scheduler
- ✅ Denoise
- ✅ BatchSize
- ✅ GetAvailableModelsAsync endpoint

**Parameters Not Tested:**
- OutputFilename - Used locally after generation, not sent to ComfyUI
- OutputFormat - Used locally after generation, not sent to ComfyUI

### Video Generation Tests (13 tests)
Located in: `VideoGenerationWorkflowIntegrationTests.cs`

**UI Parameters Tested:**
- ✅ TextPrompt
- ✅ NegativePrompt
- ✅ Seed
- ✅ Steps
- ✅ CFGScale
- ✅ SamplerName
- ✅ Scheduler
- ✅ Fps
- ✅ ImagePath
- ✅ AudioPath
- ✅ GetAvailableModelsAsync endpoint
- ✅ GetUNETModelsAsync endpoint
- ✅ GetLoRAModelsAsync endpoint

**Parameters Not Tested:**
- CheckpointName - May not be used in all video workflows (depends on workflow type)
- Denoise - Not all video workflows use this parameter
- Width/Height - These may be derived from the input image
- FrameCount - Calculated from DurationSeconds and Fps
- DurationSeconds - Used to calculate FrameCount, but only Fps is sent
- MotionBucketId - Only used in SVD (Stable Video Diffusion) workflows
- AugmentationLevel - Only used in specific video generation workflows
- AnimationStyle - UI helper field, not directly used in ComfyUI workflows
- MotionIntensity - UI helper field, not directly used in ComfyUI workflows
- OutputFilename - Used as a prefix, doesn't affect HTTP request structure
- OutputFormat - Used locally after generation
- Quality - Used locally for encoding

### Audio Generation Tests (12 tests)
Located in: `AudioGenerationWorkflowIntegrationTests.cs`

**UI Parameters Tested:**
- ✅ Tags
- ✅ Lyrics
- ✅ CheckpointName
- ✅ Seed
- ✅ Steps
- ✅ CFGScale
- ✅ AudioDurationSeconds
- ✅ LyricsStrength
- ✅ GetAvailableModelsAsync endpoint
- ✅ GetCLIPModelsAsync endpoint
- ✅ GetVAEModelsAsync endpoint
- ✅ GetAudioEncoderModelsAsync endpoint

**Parameters Not Tested:**
- SamplerName - May vary by workflow implementation
- Scheduler - May vary by workflow implementation
- Denoise - May vary by workflow implementation
- BatchSize - Used for generating multiple versions, doesn't change core workflow
- ModelShift - ACE Step specific, may not appear in serialized JSON in expected format
- TonemapMultiplier - ACE Step specific, may not appear in serialized JSON in expected format
- OutputFilename - Used locally after generation
- OutputFormat - Used locally after generation
- AudioQuality - Used locally for encoding

## Test Infrastructure

### MockHttpMessageHandler
Located in: `Infrastructure/MockHttpMessageHandler.cs`

A custom `HttpMessageHandler` implementation that:
- Captures all HTTP requests for inspection
- Allows enqueueing mock responses
- Provides helpers to examine request content

## Running the Tests

```bash
# Run all integration tests
dotnet test tests/VideoGenerationApp.IntegrationTests/VideoGenerationApp.IntegrationTests.csproj

# Run specific test class
dotnet test --filter "FullyQualifiedName~ImageGenerationWorkflowIntegrationTests"
dotnet test --filter "FullyQualifiedName~VideoGenerationWorkflowIntegrationTests"
dotnet test --filter "FullyQualifiedName~AudioGenerationWorkflowIntegrationTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Test Results

✅ **38 tests passing**
- 13 Image Generation tests
- 13 Video Generation tests
- 12 Audio Generation tests

All tests verify that UI parameters are correctly transmitted via HTTP requests to the ComfyUI API, with only the HttpMessageHandler mocked as specified in the requirements.
