# Image and Video Generation Features

This document describes the new image and video generation features added to the VideoGenerationApp.

## Overview

Two new pages have been added to the application:

1. **Generate Image** (`/generate-image`) - Generate images using configurable parameters
2. **Generate Video** (`/generate-video`) - Generate videos by combining audio, images, and text

Both pages integrate seamlessly with the existing Generation Queue system.

## Features

### Image Generation Page

The Image Generation page allows users to:

- **Use Ollama Output**: Automatically populate the positive prompt with the `visual_description` field from Ollama's parsed output
- **Tunable Parameters**:
  - Positive Prompt: Describe what you want in the image
  - Negative Prompt: Describe what you don't want
  - Width/Height: Image dimensions (512-2048 pixels)
  - Steps: Number of generation steps (1-150)
  - CFG Scale: How closely to follow the prompt (1-30)
  - Seed: For reproducible results (-1 for random)
  - Sampler: Choice of sampling algorithms (Euler, DPM++, etc.)

### Video Generation Page

The Video Generation page allows users to:

- **Select Audio**: Choose from previously generated audio files
- **Select Image**: Choose from previously generated images as a base
- **Auto-duration Matching**: When audio is selected, duration automatically matches the audio length
- **Editable Pre-prompt**: The text prompt can be edited and is pre-populated from Ollama's narrative field
- **Tunable Parameters**:
  - Text Prompt/Description
  - Duration in seconds
  - Width/Height: Video resolution
  - FPS: 24, 30, or 60 frames per second
  - Animation Style: Static, Smooth, or Dynamic
  - Motion Intensity: 0.0 (none) to 1.0 (maximum)
  - Steps, CFG Scale, and Quality settings

### Integration with Ollama

Both pages integrate with the Ollama output:

- **Image Page**: Uses `visual_description` field as the default positive prompt
- **Video Page**: Uses `narrative` field as the default text prompt and can incorporate `video_actions`

### Generation Queue

Both image and video generation tasks are added to the centralized Generation Queue where users can:

- Monitor generation progress
- View task status (Pending, Queued, Processing, Completed, Failed)
- Cancel tasks if needed
- Access generated files once complete

## Technical Implementation

### New DTOs

- **ImageWorkflowConfig**: Configuration for image generation with all tunable parameters
- **VideoWorkflowConfig**: Configuration for video generation with audio/image references and parameters

### Updated Components

- **GenerationTask**: Now supports three types: Audio, Image, and Video
- **GenerationQueueService**: Extended with `QueueImageGenerationAsync` and `QueueVideoGenerationAsync` methods
- **NavMenu**: Added navigation links for the new pages

### Navigation

The navigation menu now includes:

- Home
- Ollama Models (text generation)
- Generate Audio
- **Generate Image** (NEW)
- **Generate Video** (NEW)
- Generation Queue

## Testing

Comprehensive tests have been added:

- **ImageWorkflowConfigTests**: Validates default values and customization
- **VideoWorkflowConfigTests**: Validates default values, customization, and optional inputs
- **GenerationQueueServiceTests**: Extended to test image and video task queueing

### Test Results

All new tests pass successfully:
- ✅ ImageWorkflowConfig default values
- ✅ ImageWorkflowConfig customization
- ✅ VideoWorkflowConfig default values
- ✅ VideoWorkflowConfig customization
- ✅ VideoWorkflowConfig optional inputs
- ✅ Queue image generation task
- ✅ Queue video generation task

### CI/CD

A GitHub Actions workflow (`.github/workflows/dotnet.yml`) has been created that:

- Runs on push and pull requests to master/main branches
- Builds the solution
- Runs all tests
- Uploads test results as artifacts

## Usage Workflow

### Typical Usage Flow:

1. **Generate Text/Prompt** using Ollama Models page
2. **Generate Audio** using the prompts and music/voice descriptions
3. **Generate Image** using the visual descriptions from Ollama
4. **Generate Video** by combining the audio and image with the narrative
5. **Monitor Progress** in the Generation Queue page

## Future Enhancements

The current implementation provides the UI and queue integration. Future work could include:

- Implementation of actual ComfyUI image generation workflows
- Implementation of actual ComfyUI video generation workflows
- Additional sampler and scheduler options
- Advanced animation controls
- Batch generation support

## Notes

- Image and video generation are currently queued as "Pending" tasks since the full ComfyUI workflow integration is marked for future implementation
- The queue system is designed to handle these tasks once the workflows are implemented
- All UI, parameters, and queue integration are fully functional and tested
