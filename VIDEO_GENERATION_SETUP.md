# Video Generation Setup

## Prerequisites

Video generation in this application uses **Stable Video Diffusion (SVD)**, which requires a specific model to be installed in ComfyUI.

### Required Model

- **Model Name**: `svd_xt.safetensors`
- **Type**: Stable Video Diffusion (Image-to-Video)
- **Size**: ~10 GB
- **Location**: Should be placed in `ComfyUI/models/checkpoints/`

## Installation Options

### Option 1: Automated Installation (Recommended)

Run the automated installation script which downloads all required models including SVD:

**Windows:**
```powershell
pwsh -ExecutionPolicy Bypass -File .\scripts\install.ps1
```

**Linux/macOS:**
```bash
pwsh ./scripts/install.ps1
```

This script will:
1. Install ComfyUI if not already installed
2. Download SVD model and other required models
3. Configure everything automatically

### Option 2: Manual Installation

If you prefer to install manually:

1. Navigate to your ComfyUI models directory:
   ```bash
   cd ~/ComfyUI/models/checkpoints
   ```

2. Download the SVD model from Hugging Face:
   ```bash
   wget https://huggingface.co/stabilityai/stable-video-diffusion-img2vid-xt/resolve/main/svd_xt.safetensors
   ```
   
   Or download manually from: https://huggingface.co/stabilityai/stable-video-diffusion-img2vid-xt

3. Restart ComfyUI to recognize the new model

## Troubleshooting

### Error: "svd_xt.safetensors not in available models"

This error occurs when the SVD model has not been installed. Follow the installation steps above.

**Available models on your ComfyUI server:**
- Check the ComfyUI web interface at http://127.0.0.1:8188
- Navigate to the checkpoint loader node
- View the dropdown list of available models

If `svd_xt.safetensors` is not listed, the model needs to be downloaded.

### Error: "Return type mismatch between linked nodes"

This is a workflow structure error that should be fixed in the latest version. If you still see this error:
1. Pull the latest code from the repository
2. Ensure you're using version with the workflow fixes
3. Clear any cached workflows

## Model Information

### Stable Video Diffusion (SVD)

- **Purpose**: Generates video from a single image
- **Input**: Static image + motion parameters
- **Output**: Short video clip (typically 2-10 seconds)
- **Quality**: High-quality, smooth video generation
- **Processing Time**: Depends on hardware (several minutes per video)

### Workflow Parameters

The SVD workflow supports these key parameters:
- **Width/Height**: Video resolution (e.g., 512x512, 1024x1024)
- **Duration**: Video length in seconds
- **FPS**: Frame rate (24, 30, or 60 fps)
- **Motion Intensity**: Controls amount of movement (0.0 - 1.0)
- **Augmentation Level**: SVD-specific parameter for image conditioning (typically 0.0 - 0.3)

## System Requirements

- **GPU**: NVIDIA GPU with at least 8GB VRAM (16GB+ recommended)
- **RAM**: 16GB+ system RAM
- **Storage**: 15GB+ free space for models
- **Python**: 3.10 or higher
- **ComfyUI**: Latest version

## Performance Tips

1. **Start with lower resolutions** (512x512) for faster generation
2. **Use shorter durations** (2-5 seconds) for testing
3. **Adjust motion intensity** based on desired effect (0.5 is a good default)
4. **Monitor GPU memory** usage during generation

## Support

If you encounter issues:
1. Check that ComfyUI is running: `http://127.0.0.1:8188`
2. Verify SVD model is in `ComfyUI/models/checkpoints/`
3. Check ComfyUI console for error messages
4. Review application logs for detailed error information
