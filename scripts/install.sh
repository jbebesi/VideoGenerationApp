#!/bin/bash

################################################################################
# VideoGenerationApp Installation Script
################################################################################
#
# Description:
#   This script automates the installation of dependencies for VideoGenerationApp:
#   - Ollama (local LLM runtime)
#   - Python 3.10+ (required for ComfyUI)
#   - ComfyUI (AI workflow execution engine)
#   - ACE Step model (audio generation model for ComfyUI)
#
# Features:
#   - Idempotent: Safe to run multiple times
#   - Detailed logging with timestamps
#   - Configurable via parameters
#   - Skip options for individual components
#   - Automatic detection of existing installations
#
# Usage:
#   ./install.sh [OPTIONS]
#
# Options:
#   --skip-ollama           Skip Ollama installation
#   --skip-python           Skip Python installation
#   --skip-comfyui          Skip ComfyUI installation
#   --skip-models           Skip model downloads
#   --ollama-version VERSION Specify Ollama version (default: latest)
#   --python-version VERSION Specify Python version (default: 3.10)
#   --comfyui-path PATH     Custom ComfyUI installation path (default: ~/ComfyUI)
#   --models-path PATH      Custom models path (default: auto-detected from ComfyUI)
#   --verbose               Enable verbose logging
#   --help                  Display this help message
#
# Environment Variables:
#   COMFYUI_PATH           Override default ComfyUI installation path
#   OLLAMA_API_URL         Ollama API URL (default: http://127.0.0.1:11434)
#
# Examples:
#   # Full installation with defaults
#   ./install.sh
#
#   # Skip Ollama if already installed
#   ./install.sh --skip-ollama
#
#   # Install to custom ComfyUI path
#   ./install.sh --comfyui-path /opt/ComfyUI
#
#   # Verbose mode
#   ./install.sh --verbose
#
################################################################################

set -e  # Exit on error
set -u  # Exit on undefined variable

################################################################################
# Configuration and Default Values
################################################################################

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCRIPT_NAME="$(basename "${BASH_SOURCE[0]}")"
LOG_FILE="/tmp/videogenapp-install-$(date +%Y%m%d-%H%M%S).log"

# Default values
SKIP_OLLAMA=false
SKIP_PYTHON=false
SKIP_COMFYUI=false
SKIP_MODELS=false
OLLAMA_VERSION="latest"
PYTHON_VERSION="3.10"
COMFYUI_PATH="${COMFYUI_PATH:-$HOME/ComfyUI}"
MODELS_PATH=""
VERBOSE=false
OLLAMA_API_URL="${OLLAMA_API_URL:-http://127.0.0.1:11434}"

# Model URLs and paths
ACE_STEP_MODEL_NAME="ace_step_v1_3.5b.safetensors"
ACE_STEP_MODEL_URL="https://huggingface.co/ai-audio/ACE-Studio/resolve/main/ACE_Step_v1_3.5B.safetensors"
OLLAMA_MODELS=("llama3.2:1b" "qwen2.5:0.5b")  # Small, efficient models

################################################################################
# Logging Functions
################################################################################

log() {
    local level=$1
    shift
    local message="$*"
    local timestamp=$(date '+%Y-%m-%d %H:%M:%S')
    echo "[$timestamp] [$level] $message" | tee -a "$LOG_FILE"
}

log_info() {
    log "INFO" "$@"
}

log_warn() {
    log "WARN" "$@"
}

log_error() {
    log "ERROR" "$@"
}

log_success() {
    log "SUCCESS" "$@"
}

log_verbose() {
    if [ "$VERBOSE" = true ]; then
        log "VERBOSE" "$@"
    fi
}

################################################################################
# Helper Functions
################################################################################

show_help() {
    sed -n '/^# Usage:/,/^################################################################################/p' "$0" | \
        sed 's/^# //g' | sed 's/^#//g' | head -n -1
}

check_command() {
    local cmd=$1
    if command -v "$cmd" >/dev/null 2>&1; then
        return 0
    else
        return 1
    fi
}

get_os() {
    case "$(uname -s)" in
        Linux*)  echo "linux" ;;
        Darwin*) echo "macos" ;;
        *)       echo "unknown" ;;
    esac
}

get_arch() {
    case "$(uname -m)" in
        x86_64)  echo "amd64" ;;
        aarch64) echo "arm64" ;;
        arm64)   echo "arm64" ;;
        *)       echo "unknown" ;;
    esac
}

################################################################################
# Ollama Installation
################################################################################

install_ollama() {
    log_info "=== Installing Ollama ==="
    
    if check_command ollama; then
        local current_version=$(ollama --version 2>/dev/null | head -n1 || echo "unknown")
        log_info "Ollama is already installed: $current_version"
        log_info "Skipping Ollama installation (idempotent check)"
        return 0
    fi
    
    local os=$(get_os)
    log_info "Detected OS: $os"
    
    case "$os" in
        linux)
            log_info "Installing Ollama for Linux..."
            curl -fsSL https://ollama.com/install.sh | sh
            ;;
        macos)
            log_info "Installing Ollama for macOS..."
            if check_command brew; then
                brew install ollama
            else
                log_error "Homebrew not found. Please install Homebrew first: https://brew.sh"
                return 1
            fi
            ;;
        *)
            log_error "Unsupported operating system: $os"
            log_info "Please install Ollama manually from: https://ollama.com/download"
            return 1
            ;;
    esac
    
    # Verify installation
    if check_command ollama; then
        local installed_version=$(ollama --version 2>/dev/null | head -n1 || echo "unknown")
        log_success "Ollama installed successfully: $installed_version"
        
        # Start Ollama service
        log_info "Starting Ollama service..."
        if [ "$os" = "linux" ]; then
            if command -v systemctl >/dev/null 2>&1; then
                sudo systemctl start ollama || log_warn "Could not start Ollama service via systemd"
                sudo systemctl enable ollama || log_warn "Could not enable Ollama service"
            else
                log_warn "systemctl not found, you may need to start Ollama manually"
            fi
        else
            log_info "Ollama service can be started with: ollama serve"
        fi
    else
        log_error "Ollama installation failed"
        return 1
    fi
}

pull_ollama_models() {
    log_info "=== Pulling Ollama Models ==="
    
    if ! check_command ollama; then
        log_error "Ollama not found. Cannot pull models."
        return 1
    fi
    
    # Ensure Ollama is running
    log_info "Checking if Ollama service is running..."
    local retries=0
    local max_retries=5
    while [ $retries -lt $max_retries ]; do
        if curl -s "$OLLAMA_API_URL/api/version" >/dev/null 2>&1; then
            log_info "Ollama service is running"
            break
        else
            log_warn "Ollama service not responding, attempt $((retries + 1))/$max_retries"
            if [ $retries -eq 0 ]; then
                log_info "Starting Ollama service..."
                if [ "$(get_os)" = "linux" ]; then
                    if command -v systemctl >/dev/null 2>&1; then
                        sudo systemctl start ollama 2>/dev/null || true
                    fi
                fi
                # Also try to start in background
                (ollama serve >/dev/null 2>&1 &)
            fi
            sleep 3
            retries=$((retries + 1))
        fi
    done
    
    if [ $retries -eq $max_retries ]; then
        log_error "Ollama service is not running. Please start it manually with: ollama serve"
        return 1
    fi
    
    # Pull models
    for model in "${OLLAMA_MODELS[@]}"; do
        log_info "Checking if model '$model' exists..."
        if ollama list | grep -q "^${model%%:*}"; then
            log_info "Model '$model' already exists (idempotent check)"
        else
            log_info "Pulling model '$model'..."
            if ollama pull "$model"; then
                log_success "Model '$model' pulled successfully"
            else
                log_error "Failed to pull model '$model'"
            fi
        fi
    done
}

################################################################################
# Python Installation
################################################################################

check_python_version() {
    local required_version=$1
    
    if check_command python3; then
        local current_version=$(python3 --version 2>&1 | awk '{print $2}')
        log_info "Found Python: $current_version"
        
        # Compare versions
        local required_major=$(echo "$required_version" | cut -d. -f1)
        local required_minor=$(echo "$required_version" | cut -d. -f2)
        local current_major=$(echo "$current_version" | cut -d. -f1)
        local current_minor=$(echo "$current_version" | cut -d. -f2)
        
        if [ "$current_major" -gt "$required_major" ] || \
           ([ "$current_major" -eq "$required_major" ] && [ "$current_minor" -ge "$required_minor" ]); then
            log_success "Python version $current_version meets requirement (>= $required_version)"
            return 0
        else
            log_warn "Python version $current_version is below requirement (>= $required_version)"
            return 1
        fi
    else
        log_warn "Python3 not found"
        return 1
    fi
}

install_python() {
    log_info "=== Checking Python Installation ==="
    
    if check_python_version "$PYTHON_VERSION"; then
        log_info "Skipping Python installation (idempotent check)"
        return 0
    fi
    
    local os=$(get_os)
    log_info "Installing Python $PYTHON_VERSION or later..."
    
    case "$os" in
        linux)
            if check_command apt-get; then
                log_info "Using apt-get to install Python..."
                sudo apt-get update
                sudo apt-get install -y python3 python3-pip python3-venv python3-dev
            elif check_command yum; then
                log_info "Using yum to install Python..."
                sudo yum install -y python3 python3-pip python3-devel
            elif check_command dnf; then
                log_info "Using dnf to install Python..."
                sudo dnf install -y python3 python3-pip python3-devel
            else
                log_error "No supported package manager found (apt-get, yum, dnf)"
                return 1
            fi
            ;;
        macos)
            if check_command brew; then
                log_info "Using Homebrew to install Python..."
                brew install python@3.10
            else
                log_error "Homebrew not found. Please install Homebrew first: https://brew.sh"
                return 1
            fi
            ;;
        *)
            log_error "Unsupported operating system for automatic Python installation"
            return 1
            ;;
    esac
    
    # Verify installation
    if check_python_version "$PYTHON_VERSION"; then
        log_success "Python installed successfully"
    else
        log_error "Python installation verification failed"
        return 1
    fi
}

################################################################################
# ComfyUI Installation
################################################################################

install_comfyui() {
    log_info "=== Installing ComfyUI ==="
    
    # Check if already installed
    if [ -d "$COMFYUI_PATH" ]; then
        if [ -f "$COMFYUI_PATH/main.py" ]; then
            log_info "ComfyUI already installed at: $COMFYUI_PATH"
            log_info "Skipping ComfyUI installation (idempotent check)"
            
            # Update dependencies
            log_info "Updating ComfyUI dependencies..."
            cd "$COMFYUI_PATH"
            if [ -f "requirements.txt" ]; then
                python3 -m pip install --upgrade pip
                python3 -m pip install -r requirements.txt
                log_success "ComfyUI dependencies updated"
            fi
            return 0
        fi
    fi
    
    # Clone ComfyUI
    log_info "Cloning ComfyUI to: $COMFYUI_PATH"
    git clone https://github.com/comfyanonymous/ComfyUI.git "$COMFYUI_PATH"
    
    # Install dependencies
    cd "$COMFYUI_PATH"
    log_info "Installing ComfyUI dependencies..."
    python3 -m pip install --upgrade pip
    python3 -m pip install -r requirements.txt
    
    # Verify installation
    if [ -f "$COMFYUI_PATH/main.py" ]; then
        log_success "ComfyUI installed successfully at: $COMFYUI_PATH"
    else
        log_error "ComfyUI installation verification failed"
        return 1
    fi
}

install_comfyui_custom_nodes() {
    log_info "=== Installing ComfyUI Custom Nodes ==="
    
    local custom_nodes_dir="$COMFYUI_PATH/custom_nodes"
    mkdir -p "$custom_nodes_dir"
    
    # Install ComfyUI-Manager (optional but useful)
    local manager_path="$custom_nodes_dir/ComfyUI-Manager"
    if [ -d "$manager_path" ]; then
        log_info "ComfyUI-Manager already installed (idempotent check)"
    else
        log_info "Installing ComfyUI-Manager..."
        git clone https://github.com/ltdrdata/ComfyUI-Manager.git "$manager_path" || \
            log_warn "Failed to install ComfyUI-Manager (optional)"
    fi
    
    # Install audio-related custom nodes if needed
    local audio_nodes_path="$custom_nodes_dir/ComfyUI-AudioScheduler"
    if [ -d "$audio_nodes_path" ]; then
        log_info "Audio nodes already installed (idempotent check)"
    else
        log_info "Installing audio custom nodes..."
        # Note: This is a placeholder - adjust URL based on actual audio nodes needed
        git clone https://github.com/a1lazydog/ComfyUI-AudioScheduler.git "$audio_nodes_path" 2>/dev/null || \
            log_warn "Audio nodes repository not available or changed"
    fi
}

################################################################################
# Model Downloads
################################################################################

download_ace_step_model() {
    log_info "=== Downloading ACE Step Model ==="
    
    # Determine models path
    if [ -z "$MODELS_PATH" ]; then
        MODELS_PATH="$COMFYUI_PATH/models/checkpoints"
    fi
    
    mkdir -p "$MODELS_PATH"
    log_info "Models path: $MODELS_PATH"
    
    local model_file="$MODELS_PATH/$ACE_STEP_MODEL_NAME"
    
    # Check if model already exists
    if [ -f "$model_file" ]; then
        local file_size=$(du -h "$model_file" | cut -f1)
        log_info "ACE Step model already exists: $model_file ($file_size)"
        log_info "Skipping download (idempotent check)"
        return 0
    fi
    
    log_info "Downloading ACE Step model from: $ACE_STEP_MODEL_URL"
    log_info "This may take a while depending on your internet connection..."
    
    # Download with progress
    if check_command wget; then
        wget -O "$model_file.tmp" --progress=bar:force "$ACE_STEP_MODEL_URL" 2>&1 | \
            tee -a "$LOG_FILE"
        mv "$model_file.tmp" "$model_file"
    elif check_command curl; then
        curl -L -o "$model_file.tmp" --progress-bar "$ACE_STEP_MODEL_URL" 2>&1 | \
            tee -a "$LOG_FILE"
        mv "$model_file.tmp" "$model_file"
    else
        log_error "Neither wget nor curl found. Cannot download model."
        log_info "Please download manually from: $ACE_STEP_MODEL_URL"
        log_info "Save to: $model_file"
        return 1
    fi
    
    # Verify download
    if [ -f "$model_file" ]; then
        local file_size=$(du -h "$model_file" | cut -f1)
        log_success "ACE Step model downloaded successfully: $model_file ($file_size)"
    else
        log_error "Model download verification failed"
        return 1
    fi
}

################################################################################
# Main Installation Flow
################################################################################

parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --skip-ollama)
                SKIP_OLLAMA=true
                shift
                ;;
            --skip-python)
                SKIP_PYTHON=true
                shift
                ;;
            --skip-comfyui)
                SKIP_COMFYUI=true
                shift
                ;;
            --skip-models)
                SKIP_MODELS=true
                shift
                ;;
            --ollama-version)
                OLLAMA_VERSION="$2"
                shift 2
                ;;
            --python-version)
                PYTHON_VERSION="$2"
                shift 2
                ;;
            --comfyui-path)
                COMFYUI_PATH="$2"
                shift 2
                ;;
            --models-path)
                MODELS_PATH="$2"
                shift 2
                ;;
            --verbose)
                VERBOSE=true
                shift
                ;;
            --help|-h)
                show_help
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                show_help
                exit 1
                ;;
        esac
    done
}

print_summary() {
    log_info "=== Installation Summary ==="
    log_info "Configuration:"
    log_info "  - Skip Ollama: $SKIP_OLLAMA"
    log_info "  - Skip Python: $SKIP_PYTHON"
    log_info "  - Skip ComfyUI: $SKIP_COMFYUI"
    log_info "  - Skip Models: $SKIP_MODELS"
    log_info "  - Python Version: $PYTHON_VERSION"
    log_info "  - ComfyUI Path: $COMFYUI_PATH"
    log_info "  - Log File: $LOG_FILE"
    log_info ""
}

print_final_status() {
    log_info "=== Installation Complete ==="
    
    # Check what's installed
    log_info "Installed Components:"
    
    if check_command ollama; then
        local ollama_ver=$(ollama --version 2>/dev/null | head -n1)
        log_success "  ✓ Ollama: $ollama_ver"
    else
        log_info "  ✗ Ollama: Not installed"
    fi
    
    if check_python_version "$PYTHON_VERSION"; then
        local python_ver=$(python3 --version 2>&1)
        log_success "  ✓ Python: $python_ver"
    else
        log_info "  ✗ Python: Not meeting requirements"
    fi
    
    if [ -f "$COMFYUI_PATH/main.py" ]; then
        log_success "  ✓ ComfyUI: $COMFYUI_PATH"
    else
        log_info "  ✗ ComfyUI: Not installed"
    fi
    
    local model_file="$COMFYUI_PATH/models/checkpoints/$ACE_STEP_MODEL_NAME"
    if [ -f "$model_file" ]; then
        local file_size=$(du -h "$model_file" | cut -f1)
        log_success "  ✓ ACE Step Model: $file_size"
    else
        log_info "  ✗ ACE Step Model: Not downloaded"
    fi
    
    log_info ""
    log_info "Next Steps:"
    log_info "  1. Start Ollama: ollama serve"
    log_info "  2. Start ComfyUI: cd $COMFYUI_PATH && python3 main.py --api"
    log_info "  3. Set COMFYUI_PATH environment variable: export COMFYUI_PATH=$COMFYUI_PATH"
    log_info "  4. Run VideoGenerationApp"
    log_info ""
    log_info "Full installation log: $LOG_FILE"
}

main() {
    log_info "=== VideoGenerationApp Installation Script ==="
    log_info "Starting installation at $(date)"
    log_info "Log file: $LOG_FILE"
    log_info ""
    
    parse_arguments "$@"
    print_summary
    
    # Run installations
    if [ "$SKIP_OLLAMA" = false ]; then
        install_ollama || log_error "Ollama installation failed (continuing...)"
        pull_ollama_models || log_error "Ollama model pull failed (continuing...)"
    else
        log_info "Skipping Ollama installation (--skip-ollama)"
    fi
    
    if [ "$SKIP_PYTHON" = false ]; then
        install_python || log_error "Python installation failed (continuing...)"
    else
        log_info "Skipping Python installation (--skip-python)"
    fi
    
    if [ "$SKIP_COMFYUI" = false ]; then
        install_comfyui || log_error "ComfyUI installation failed (continuing...)"
        install_comfyui_custom_nodes || log_error "Custom nodes installation failed (continuing...)"
    else
        log_info "Skipping ComfyUI installation (--skip-comfyui)"
    fi
    
    if [ "$SKIP_MODELS" = false ]; then
        download_ace_step_model || log_error "Model download failed (continuing...)"
    else
        log_info "Skipping model downloads (--skip-models)"
    fi
    
    print_final_status
    log_success "Installation script completed at $(date)"
}

# Run main function
main "$@"
