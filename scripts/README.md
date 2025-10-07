# Installation Scripts

This directory contains scripts to automate the installation and configuration of VideoGenerationApp dependencies.

## install.ps1

A cross-platform PowerShell script that automates the installation of:
- **Ollama**: AI model runtime for local language models
- **Python**: Required for ComfyUI (minimum version 3.10)
- **ComfyUI**: Stable Diffusion workflow engine
- **AI Models**: Required models for both Ollama and ComfyUI

### Prerequisites

- **PowerShell 7.0+** (for cross-platform support)
  - Windows: Usually pre-installed, or download from [PowerShell GitHub](https://github.com/PowerShell/PowerShell)
  - Linux: `sudo snap install powershell --classic` or [installation guide](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-linux)
  - macOS: `brew install powershell/tap/powershell`

- **Git**: Required for cloning ComfyUI
  - Windows: [Git for Windows](https://git-scm.com/download/win)
  - Linux: `sudo apt install git` (or equivalent for your distro)
  - macOS: `brew install git`

### Usage

#### Basic Installation (Recommended)

Run with default settings to install everything:

**Windows:**
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install.ps1
```

**Linux/macOS:**
```bash
pwsh ./scripts/install.ps1
```

#### Custom Installation Examples

**Install with custom Ollama models:**
```powershell
.\scripts\install.ps1 -OllamaModels "llama3.2:1b,mistral:7b,codellama:7b"
```

**Install ComfyUI to custom location:**
```powershell
.\scripts\install.ps1 -ComfyUIPath "C:\AI\ComfyUI"
```

**Skip Ollama installation:**
```powershell
.\scripts\install.ps1 -SkipOllama
```

**Skip AI model downloads:**
```powershell
.\scripts\install.ps1 -SkipModels
```

**Only install Ollama and Python:**
```powershell
.\scripts\install.ps1 -SkipComfyUI
```

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `OllamaModels` | String | `"llama3.2:3b,qwen2.5:3b"` | Comma-separated list of Ollama models to download |
| `ComfyUIPath` | String | `"$HOME/ComfyUI"` | Installation path for ComfyUI |
| `SkipOllama` | Switch | `false` | Skip Ollama installation and model downloads |
| `SkipPython` | Switch | `false` | Skip Python installation check |
| `SkipComfyUI` | Switch | `false` | Skip ComfyUI installation |
| `SkipModels` | Switch | `false` | Skip AI model downloads for ComfyUI |
| `PythonVersion` | String | `"3.10"` | Minimum required Python version |

### What Gets Installed

#### Ollama
- **Installation**: Automatic download and installation
- **Service**: Automatically started
- **Default Models**: 
  - `llama3.2:3b` - Compact language model
  - `qwen2.5:3b` - Alternative language model
- **Port**: 11434 (default)

#### Python
- **Minimum Version**: 3.10
- **Validation**: Checks existing installation
- **pip**: Ensures pip is available and updated

#### ComfyUI
- **Source**: Cloned from [official repository](https://github.com/comfyanonymous/ComfyUI)
- **Default Path**: `$HOME/ComfyUI`
- **Dependencies**: Automatically installed from requirements.txt
- **API**: Configured to run with `--api` flag
- **Port**: 8188 (default)

#### AI Models for ComfyUI

The script downloads these models to the appropriate ComfyUI directories:

1. **ace_step_v1_3.5b.safetensors** (~3.5 GB)
   - Location: `ComfyUI/models/checkpoints/`
   - Purpose: Audio generation (singing/speech)
   - Source: AIHUB/ACE-Studio

2. **sd_xl_base_1.0.safetensors** (~6.9 GB)
   - Location: `ComfyUI/models/checkpoints/`
   - Purpose: Image generation
   - Source: Stable Diffusion XL

3. **svd_xt.safetensors** (~9.8 GB)
   - Location: `ComfyUI/models/checkpoints/`
   - Purpose: Video generation
   - Source: Stable Video Diffusion

**Note**: Model downloads can take significant time depending on your internet connection. Total download size is approximately 20+ GB.

### Idempotency

The script is designed to be **idempotent**, meaning:
- Running it multiple times is safe
- Already installed components are detected and skipped
- Existing models are not re-downloaded
- No data is lost or overwritten

### Environment Variables

The script sets the following environment variable:

- **COMFYUI_PATH**: Points to the ComfyUI installation directory
  - Used by VideoGenerationApp to locate ComfyUI
  - Windows: Set in User environment variables
  - Linux/macOS: Instructions provided to add to shell profile

### Logging

The script provides detailed logging with color-coded messages:
- **Cyan [INFO]**: Informational messages
- **Green [SUCCESS]**: Successful operations
- **Yellow [WARNING]**: Warnings and optional actions
- **Red [ERROR]**: Errors and failures
- **Magenta [===]**: Section headers

### Troubleshooting

#### Ollama Installation Fails
- **Windows**: Download manually from [ollama.com](https://ollama.com/download)
- **Linux**: Run `curl -fsSL https://ollama.com/install.sh | sh`
- **macOS**: Download from [ollama.com](https://ollama.com/download)

#### Python Version Too Old
- Download Python 3.10+ from [python.org](https://www.python.org/downloads/)
- On Linux: Use your package manager or pyenv
- Verify with: `python --version`

#### ComfyUI Clone Fails
- Ensure Git is installed and in PATH
- Check network connectivity
- Clone manually: `git clone https://github.com/comfyanonymous/ComfyUI.git`

#### Model Downloads Fail
- Downloads may timeout on slow connections
- Models are very large (several GB each)
- Download manually from URLs shown in error messages
- Place models in appropriate `ComfyUI/models/checkpoints/` directory

#### Permission Errors
- **Windows**: Run PowerShell as Administrator
- **Linux/macOS**: Use `sudo pwsh ./scripts/install.ps1` if needed

### Post-Installation

After successful installation:

1. **Start Ollama** (if not already running):
   ```bash
   ollama serve
   ```

2. **Start ComfyUI**:
   ```bash
   cd ~/ComfyUI
   python main.py --api
   ```
   Access at: http://127.0.0.1:8188

3. **Verify Ollama**:
   ```bash
   ollama list
   ```

4. **Run VideoGenerationApp**:
   ```bash
   dotnet run --project VideoGenerationApp
   ```

### Manual Installation

If you prefer to install components manually, refer to:
- [Ollama Installation](https://ollama.com/download)
- [ComfyUI Installation](https://github.com/comfyanonymous/ComfyUI#installing)
- [Python Installation](https://www.python.org/downloads/)

### Getting Help

For issues or questions:
1. Check this README
2. Review script output for error messages
3. Check individual component documentation
4. Open an issue on the repository

### License

This script is part of VideoGenerationApp and shares the same license.
