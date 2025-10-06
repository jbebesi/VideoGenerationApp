# Installation Scripts

This directory contains installation and setup scripts for VideoGenerationApp.

## install.sh

Automated installation script for VideoGenerationApp dependencies.

### What It Does

The `install.sh` script automates the installation and configuration of:

1. **Ollama** - Local LLM runtime for AI model inference
2. **Python 3.10+** - Required for ComfyUI
3. **ComfyUI** - AI workflow execution engine
4. **ACE Step Model** - Audio generation model (ace_step_v1_3.5b.safetensors)
5. **Ollama Models** - Small, efficient models (llama3.2:1b, qwen2.5:0.5b)

### Features

- **Idempotent**: Safe to run multiple times without causing issues
- **Detailed Logging**: All operations logged with timestamps to `/tmp/videogenapp-install-*.log`
- **Configurable**: Support for custom paths and versions
- **Skip Options**: Ability to skip individual installation steps
- **Cross-Platform**: Supports Linux and macOS
- **Automatic Detection**: Detects existing installations and skips unnecessary steps

### Usage

```bash
# Basic installation with all defaults
./install.sh

# Show help
./install.sh --help

# Skip components already installed
./install.sh --skip-ollama --skip-python

# Custom ComfyUI installation path
./install.sh --comfyui-path /opt/ComfyUI

# Verbose logging
./install.sh --verbose

# Install only models (skip everything else)
./install.sh --skip-ollama --skip-python --skip-comfyui
```

### Options

| Option | Description |
|--------|-------------|
| `--skip-ollama` | Skip Ollama installation |
| `--skip-python` | Skip Python installation |
| `--skip-comfyui` | Skip ComfyUI installation |
| `--skip-models` | Skip model downloads |
| `--ollama-version VERSION` | Specify Ollama version (default: latest) |
| `--python-version VERSION` | Specify Python version (default: 3.10) |
| `--comfyui-path PATH` | Custom ComfyUI installation path (default: ~/ComfyUI) |
| `--models-path PATH` | Custom models path (default: auto-detected from ComfyUI) |
| `--verbose` | Enable verbose logging |
| `--help` | Display help message |

### Environment Variables

| Variable | Description |
|----------|-------------|
| `COMFYUI_PATH` | Override default ComfyUI installation path |
| `OLLAMA_API_URL` | Ollama API URL (default: http://127.0.0.1:11434) |

### Examples

```bash
# Full installation
./install.sh

# Installation with existing Ollama
./install.sh --skip-ollama

# Custom paths
export COMFYUI_PATH=/opt/ComfyUI
./install.sh --comfyui-path /opt/ComfyUI

# Re-download models only
./install.sh --skip-ollama --skip-python --skip-comfyui
```

### What Gets Installed

#### Ollama
- Location: System-wide installation
- Service: Automatically started (Linux with systemd)
- Models: llama3.2:1b, qwen2.5:0.5b (small, efficient models)

#### Python
- Version: 3.10 or later
- Packages: python3, python3-pip, python3-venv, python3-dev
- Package Manager: apt-get (Debian/Ubuntu), yum/dnf (RHEL/Fedora), or brew (macOS)

#### ComfyUI
- Default Path: `~/ComfyUI`
- Components: Core ComfyUI, dependencies, custom nodes
- Custom Nodes: ComfyUI-Manager, audio-related nodes

#### Models
- ACE Step: `~/ComfyUI/models/checkpoints/ace_step_v1_3.5b.safetensors`
- Size: ~7GB (varies)
- Source: Hugging Face (ai-audio/ACE-Studio)

### After Installation

1. **Start Ollama** (if not auto-started):
   ```bash
   ollama serve
   ```

2. **Start ComfyUI**:
   ```bash
   cd ~/ComfyUI
   python3 main.py --api
   ```

3. **Set Environment Variable** (for VideoGenerationApp):
   ```bash
   export COMFYUI_PATH=~/ComfyUI
   ```
   
   Or add to your shell profile (~/.bashrc, ~/.zshrc, etc.):
   ```bash
   echo 'export COMFYUI_PATH=~/ComfyUI' >> ~/.bashrc
   ```

4. **Run VideoGenerationApp**:
   ```bash
   cd VideoGenerationApp
   dotnet run
   ```

### Troubleshooting

#### Ollama Not Starting
```bash
# Check if Ollama is installed
ollama --version

# Start manually
ollama serve

# Check service status (Linux with systemd)
systemctl status ollama
```

#### Python Version Issues
```bash
# Check Python version
python3 --version

# Should be 3.10 or higher
```

#### ComfyUI Not Found
```bash
# Check if ComfyUI is installed
ls -la ~/ComfyUI/main.py

# Verify COMFYUI_PATH
echo $COMFYUI_PATH

# Set manually if needed
export COMFYUI_PATH=~/ComfyUI
```

#### Model Download Failed
```bash
# Check if model exists
ls -lh ~/ComfyUI/models/checkpoints/ace_step_v1_3.5b.safetensors

# Download manually if needed
cd ~/ComfyUI/models/checkpoints
wget https://huggingface.co/ai-audio/ACE-Studio/resolve/main/ACE_Step_v1_3.5B.safetensors \
     -O ace_step_v1_3.5b.safetensors
```

#### Check Installation Logs
```bash
# Find latest log file
ls -lt /tmp/videogenapp-install-*.log | head -1

# View log
cat /tmp/videogenapp-install-*.log
```

### Requirements

#### Linux
- curl or wget
- git
- Package manager: apt-get, yum, or dnf
- sudo access (for system packages)

#### macOS
- Homebrew
- git (usually pre-installed or via Xcode Command Line Tools)

### Notes

- The script is **idempotent** - running it multiple times won't reinstall components that are already present
- All downloads and operations are logged to `/tmp/videogenapp-install-*.log`
- The ACE Step model is approximately 7GB and may take time to download
- Ollama models are pulled automatically if Ollama service is running

### Security

- The script uses official installation sources:
  - Ollama: https://ollama.com/install.sh
  - ComfyUI: https://github.com/comfyanonymous/ComfyUI
  - Models: Hugging Face repositories
- No sensitive data is collected or transmitted
- Review the script before running if you have security concerns

### License

This script is part of VideoGenerationApp and follows the same license.
