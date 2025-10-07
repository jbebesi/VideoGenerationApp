<#
.SYNOPSIS
    Installation script for VideoGenerationApp dependencies

.DESCRIPTION
    This script automates the installation and configuration of:
    - Ollama (AI model runtime)
    - Python (for ComfyUI)
    - ComfyUI (Stable Diffusion workflows)
    - Required AI models for both Ollama and ComfyUI
    
    The script is idempotent and can be run multiple times safely.
    It works on both Windows and Linux with PowerShell Core.

.PARAMETER OllamaModels
    Comma-separated list of Ollama models to download.
    Default: "llama3.2:3b,qwen2.5:3b"

.PARAMETER ComfyUIPath
    Path where ComfyUI should be installed.
    Default: "$HOME/ComfyUI"

.PARAMETER SkipOllama
    Skip Ollama installation and model downloads.

.PARAMETER SkipPython
    Skip Python installation check.

.PARAMETER SkipComfyUI
    Skip ComfyUI installation.

.PARAMETER SkipModels
    Skip AI model downloads for ComfyUI.

.PARAMETER PythonVersion
    Minimum required Python version.
    Default: "3.10"

.EXAMPLE
    .\install.ps1
    Runs full installation with default settings.

.EXAMPLE
    .\install.ps1 -SkipOllama -ComfyUIPath "C:\AI\ComfyUI"
    Skips Ollama installation and installs ComfyUI to custom path.

.EXAMPLE
    .\install.ps1 -OllamaModels "llama3.2:1b,mistral:7b" -SkipModels
    Installs Ollama with custom models but skips ComfyUI model downloads.

.NOTES
    Author: VideoGenerationApp
    Requires: PowerShell 7.0+ for cross-platform support
#>

[CmdletBinding()]
param(
    [string]$OllamaModels = "llama3.2:3b,qwen2.5:3b",
    [string]$ComfyUIPath = "$HOME/ComfyUI",
    [switch]$SkipOllama,
    [switch]$SkipPython,
    [switch]$SkipComfyUI,
    [switch]$SkipModels,
    [string]$PythonVersion = "3.10"
)

# Set strict mode for better error handling
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Detect OS
$IsWindowsOS = $IsWindows -or ($PSVersionTable.PSVersion.Major -lt 6)
$IsLinuxOS = $IsLinux
$IsMacOS = $IsMacOS

# Color output helpers
function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Write-Step {
    param([string]$Message)
    Write-Host "`n=== $Message ===" -ForegroundColor Magenta
}

# Function to check if a command exists
function Test-Command {
    param([string]$Command)
    
    $oldPreference = $ErrorActionPreference
    $ErrorActionPreference = 'stop'
    
    try {
        if (Get-Command $Command -ErrorAction SilentlyContinue) {
            return $true
        }
    }
    catch {
        return $false
    }
    finally {
        $ErrorActionPreference = $oldPreference
    }
    
    return $false
}

# Function to compare version strings
function Test-MinimumVersion {
    param(
        [string]$Current,
        [string]$Required
    )
    
    try {
        $currentParts = $Current -split '\.' | ForEach-Object { [int]$_ }
        $requiredParts = $Required -split '\.' | ForEach-Object { [int]$_ }
        
        for ($i = 0; $i -lt [Math]::Max($currentParts.Length, $requiredParts.Length); $i++) {
            $currentPart = if ($i -lt $currentParts.Length) { $currentParts[$i] } else { 0 }
            $requiredPart = if ($i -lt $requiredParts.Length) { $requiredParts[$i] } else { 0 }
            
            if ($currentPart -gt $requiredPart) { return $true }
            if ($currentPart -lt $requiredPart) { return $false }
        }
        
        return $true
    }
    catch {
        Write-Warning "Failed to compare versions: $_"
        return $false
    }
}

# Function to install Ollama
function Install-Ollama {
    Write-Step "Installing Ollama"
    
    if (Test-Command "ollama") {
        Write-Success "Ollama is already installed"
        ollama --version
        return
    }
    
    Write-Info "Ollama not found. Installing..."
    
    if ($IsWindowsOS) {
        Write-Info "Downloading Ollama installer for Windows..."
        $installerUrl = "https://ollama.com/download/OllamaSetup.exe"
        $installerPath = "$env:TEMP\OllamaSetup.exe"
        
        try {
            Invoke-WebRequest -Uri $installerUrl -OutFile $installerPath -UseBasicParsing
            Write-Info "Running Ollama installer..."
            Start-Process -FilePath $installerPath -Wait -ArgumentList "/S"
            Remove-Item $installerPath -Force
            Write-Success "Ollama installed successfully"
        }
        catch {
            Write-Error "Failed to install Ollama: $_"
            Write-Info "Please install Ollama manually from https://ollama.com/download"
            throw
        }
    }
    elseif ($IsLinuxOS) {
        Write-Info "Installing Ollama for Linux..."
        try {
            $installScript = Invoke-WebRequest -Uri "https://ollama.com/install.sh" -UseBasicParsing
            $installScript.Content | bash
            Write-Success "Ollama installed successfully"
        }
        catch {
            Write-Error "Failed to install Ollama: $_"
            Write-Info "Please install Ollama manually: curl -fsSL https://ollama.com/install.sh | sh"
            throw
        }
    }
    elseif ($IsMacOS) {
        Write-Info "Installing Ollama for macOS..."
        Write-Warning "Please download and install Ollama from https://ollama.com/download"
        Write-Info "After installation, run this script again."
        throw "Ollama installation required"
    }
    else {
        Write-Error "Unsupported operating system"
        throw "Unsupported OS"
    }
}

# Function to start Ollama service
function Start-OllamaService {
    Write-Info "Ensuring Ollama service is running..."
    
    if ($IsWindowsOS) {
        # On Windows, Ollama typically runs as a service
        $ollamaProcess = Get-Process -Name "ollama" -ErrorAction SilentlyContinue
        if (-not $ollamaProcess) {
            Write-Info "Starting Ollama service..."
            Start-Process "ollama" -ArgumentList "serve" -WindowStyle Hidden
            Start-Sleep -Seconds 3
        }
    }
    else {
        # On Linux/Mac, check if ollama serve is running
        $running = $false
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:11434/api/tags" -Method Get -TimeoutSec 2 -UseBasicParsing -ErrorAction SilentlyContinue
            $running = $true
        }
        catch {
            $running = $false
        }
        
        if (-not $running) {
            Write-Info "Starting Ollama service..."
            Start-Process "ollama" -ArgumentList "serve" -WindowStyle Hidden -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 5
        }
    }
    
    Write-Success "Ollama service is running"
}

# Function to download Ollama models
function Install-OllamaModels {
    param([string[]]$Models)
    
    Write-Step "Downloading Ollama Models"
    
    # Start Ollama service
    Start-OllamaService
    
    foreach ($model in $Models) {
        Write-Info "Checking model: $model"
        
        # Check if model already exists
        $existingModels = ollama list 2>&1 | Out-String
        if ($existingModels -match [regex]::Escape($model)) {
            Write-Success "Model '$model' is already downloaded"
            continue
        }
        
        Write-Info "Downloading model: $model (this may take a while)..."
        try {
            ollama pull $model
            Write-Success "Model '$model' downloaded successfully"
        }
        catch {
            Write-Warning "Failed to download model '$model': $_"
            Write-Info "You can download it manually later with: ollama pull $model"
        }
    }
}

# Function to check and install Python
function Install-Python {
    Write-Step "Checking Python Installation"
    
    if (Test-Command "python3") {
        $pythonCmd = "python3"
    }
    elseif (Test-Command "python") {
        $pythonCmd = "python"
    }
    else {
        Write-Error "Python is not installed"
        Write-Info "Please install Python $PythonVersion or later from:"
        Write-Info "  Windows: https://www.python.org/downloads/"
        Write-Info "  Linux: Use your package manager (e.g., sudo apt install python3)"
        throw "Python installation required"
    }
    
    # Check Python version
    $versionOutput = & $pythonCmd --version 2>&1
    if ($versionOutput -match 'Python (\d+\.\d+)') {
        $currentVersion = $matches[1]
        Write-Info "Found Python $currentVersion"
        
        if (Test-MinimumVersion -Current $currentVersion -Required $PythonVersion) {
            Write-Success "Python version $currentVersion meets minimum requirement ($PythonVersion)"
        }
        else {
            Write-Error "Python version $currentVersion is below minimum requirement ($PythonVersion)"
            Write-Info "Please upgrade Python to version $PythonVersion or later"
            throw "Python upgrade required"
        }
    }
    else {
        Write-Warning "Could not determine Python version"
    }
    
    # Check if pip is available
    try {
        & $pythonCmd -m pip --version | Out-Null
        Write-Success "pip is available"
    }
    catch {
        Write-Warning "pip is not available"
        Write-Info "Installing pip..."
        & $pythonCmd -m ensurepip --upgrade
    }
    
    return $pythonCmd
}

# Function to install ComfyUI
function Install-ComfyUI {
    param([string]$Path)
    
    Write-Step "Installing ComfyUI"
    
    if (Test-Path $Path) {
        Write-Success "ComfyUI directory already exists at: $Path"
        
        # Check if it's a valid ComfyUI installation
        if (Test-Path "$Path/main.py") {
            Write-Success "ComfyUI appears to be installed"
            return $Path
        }
        else {
            Write-Warning "Directory exists but doesn't appear to be a ComfyUI installation"
        }
    }
    
    Write-Info "Cloning ComfyUI repository to: $Path"
    
    # Check if git is available
    if (-not (Test-Command "git")) {
        Write-Error "Git is not installed"
        Write-Info "Please install Git from https://git-scm.com/downloads"
        throw "Git installation required"
    }
    
    try {
        git clone https://github.com/comfyanonymous/ComfyUI.git $Path
        Write-Success "ComfyUI cloned successfully"
    }
    catch {
        Write-Error "Failed to clone ComfyUI: $_"
        throw
    }
    
    return $Path
}

# Function to install ComfyUI dependencies
function Install-ComfyUIDependencies {
    param(
        [string]$ComfyUIPath,
        [string]$PythonCmd
    )
    
    Write-Step "Installing ComfyUI Dependencies"
    
    $requirementsFile = "$ComfyUIPath/requirements.txt"
    
    if (-not (Test-Path $requirementsFile)) {
        Write-Warning "requirements.txt not found at: $requirementsFile"
        return
    }
    
    Write-Info "Installing Python dependencies..."
    Push-Location $ComfyUIPath
    
    try {
        & $PythonCmd -m pip install --upgrade pip
        & $PythonCmd -m pip install -r requirements.txt
        Write-Success "ComfyUI dependencies installed successfully"
    }
    catch {
        Write-Warning "Some dependencies may have failed to install: $_"
        Write-Info "You may need to install them manually"
    }
    finally {
        Pop-Location
    }
}

# Function to download ComfyUI models
function Install-ComfyUIModels {
    param([string]$ComfyUIPath)
    
    Write-Step "Downloading ComfyUI AI Models"
    
    # Define models to download
    $models = @(
        @{
            Name = "ace_step_v1_3.5b.safetensors"
            Url = "https://huggingface.co/AIHUB/ACE-Studio/resolve/main/ace_step_v1_3.5b.safetensors"
            SubDir = "checkpoints"
            Description = "ACE Step audio generation model"
        },
        @{
            Name = "sd_xl_base_1.0.safetensors"
            Url = "https://huggingface.co/stabilityai/stable-diffusion-xl-base-1.0/resolve/main/sd_xl_base_1.0.safetensors"
            SubDir = "checkpoints"
            Description = "Stable Diffusion XL base model"
        },
        @{
            Name = "svd_xt.safetensors"
            Url = "https://huggingface.co/stabilityai/stable-video-diffusion-img2vid-xt/resolve/main/svd_xt.safetensors"
            SubDir = "checkpoints"
            Description = "Stable Video Diffusion model"
        }
    )
    
    $modelsPath = "$ComfyUIPath/models"
    
    foreach ($model in $models) {
        $targetDir = "$modelsPath/$($model.SubDir)"
        $targetPath = "$targetDir/$($model.Name)"
        
        # Create directory if it doesn't exist
        if (-not (Test-Path $targetDir)) {
            New-Item -Path $targetDir -ItemType Directory -Force | Out-Null
            Write-Info "Created directory: $targetDir"
        }
        
        # Check if model already exists
        if (Test-Path $targetPath) {
            $fileSize = (Get-Item $targetPath).Length / 1GB
            Write-Success "Model '$($model.Name)' already exists ($([math]::Round($fileSize, 2)) GB)"
            continue
        }
        
        Write-Info "Downloading $($model.Description): $($model.Name)"
        Write-Info "This may take a significant amount of time depending on your connection..."
        Write-Warning "Note: These models are very large (several GB each)"
        
        try {
            # Try using curl if available (faster and more reliable for large files)
            if (Test-Command "curl") {
                Write-Info "Using curl for download..."
                curl -L -o $targetPath $model.Url
            }
            else {
                Write-Info "Using PowerShell for download..."
                # Use .NET WebClient for better performance on large files
                $webClient = New-Object System.Net.WebClient
                $webClient.DownloadFile($model.Url, $targetPath)
                $webClient.Dispose()
            }
            
            $fileSize = (Get-Item $targetPath).Length / 1GB
            Write-Success "Downloaded $($model.Name) successfully ($([math]::Round($fileSize, 2)) GB)"
        }
        catch {
            Write-Warning "Failed to download $($model.Name): $_"
            Write-Info "You can download it manually from: $($model.Url)"
            Write-Info "Save it to: $targetPath"
            
            # Clean up partial download
            if (Test-Path $targetPath) {
                Remove-Item $targetPath -Force -ErrorAction SilentlyContinue
            }
        }
    }
}

# Function to set environment variables
function Set-EnvironmentConfiguration {
    param([string]$ComfyUIPath)
    
    Write-Step "Configuring Environment Variables"
    
    $envVarName = "COMFYUI_PATH"
    $currentValue = [Environment]::GetEnvironmentVariable($envVarName, [EnvironmentVariableTarget]::User)
    
    if ($currentValue -eq $ComfyUIPath) {
        Write-Success "Environment variable $envVarName is already set correctly"
    }
    else {
        Write-Info "Setting environment variable: $envVarName = $ComfyUIPath"
        
        if ($IsWindowsOS) {
            try {
                [Environment]::SetEnvironmentVariable($envVarName, $ComfyUIPath, [EnvironmentVariableTarget]::User)
                Write-Success "Environment variable set successfully"
                Write-Warning "You may need to restart your terminal/IDE for changes to take effect"
            }
            catch {
                Write-Warning "Failed to set environment variable: $_"
                Write-Info "Please set it manually: COMFYUI_PATH=$ComfyUIPath"
            }
        }
        else {
            Write-Info "Add this line to your ~/.bashrc or ~/.zshrc:"
            Write-Host "export COMFYUI_PATH=`"$ComfyUIPath`"" -ForegroundColor Yellow
        }
    }
}

# Function to display summary
function Show-Summary {
    param(
        [bool]$OllamaInstalled,
        [bool]$PythonChecked,
        [bool]$ComfyUIInstalled,
        [bool]$ModelsDownloaded,
        [string]$ComfyUIPath
    )
    
    Write-Step "Installation Summary"
    
    if ($OllamaInstalled) {
        Write-Success "✓ Ollama installed and configured"
    }
    
    if ($PythonChecked) {
        Write-Success "✓ Python version verified"
    }
    
    if ($ComfyUIInstalled) {
        Write-Success "✓ ComfyUI installed at: $ComfyUIPath"
    }
    
    if ($ModelsDownloaded) {
        Write-Success "✓ AI models downloaded"
    }
    
    Write-Host "`n"
    Write-Info "Next Steps:"
    Write-Info "1. Start Ollama service (if not already running): ollama serve"
    Write-Info "2. Start ComfyUI: cd $ComfyUIPath && python main.py --api"
    Write-Info "3. Access ComfyUI at: http://127.0.0.1:8188"
    Write-Info "4. Run the VideoGenerationApp"
    
    Write-Host "`n"
    Write-Success "Installation completed!"
}

# Main execution
try {
    Write-Host @"
╔══════════════════════════════════════════════════════════════╗
║     VideoGenerationApp - Installation Script                ║
║     Cross-platform installer for Ollama, Python, ComfyUI    ║
╚══════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

    Write-Info "OS Detected: $(if ($IsWindowsOS) { 'Windows' } elseif ($IsLinuxOS) { 'Linux' } elseif ($IsMacOS) { 'macOS' } else { 'Unknown' })"
    Write-Info "PowerShell Version: $($PSVersionTable.PSVersion)"
    
    $ollamaInstalled = $false
    $pythonChecked = $false
    $comfyUIInstalled = $false
    $modelsDownloaded = $false
    
    # Install Ollama
    if (-not $SkipOllama) {
        Install-Ollama
        
        # Download Ollama models
        $modelList = $OllamaModels -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
        if ($modelList.Count -gt 0) {
            Install-OllamaModels -Models $modelList
        }
        
        $ollamaInstalled = $true
    }
    else {
        Write-Info "Skipping Ollama installation (--SkipOllama specified)"
    }
    
    # Check Python
    $pythonCmd = "python"
    if (-not $SkipPython) {
        $pythonCmd = Install-Python
        $pythonChecked = $true
    }
    else {
        Write-Info "Skipping Python check (--SkipPython specified)"
    }
    
    # Install ComfyUI
    if (-not $SkipComfyUI) {
        $resolvedComfyUIPath = Install-ComfyUI -Path $ComfyUIPath
        Install-ComfyUIDependencies -ComfyUIPath $resolvedComfyUIPath -PythonCmd $pythonCmd
        Set-EnvironmentConfiguration -ComfyUIPath $resolvedComfyUIPath
        
        # Download models
        if (-not $SkipModels) {
            Install-ComfyUIModels -ComfyUIPath $resolvedComfyUIPath
            $modelsDownloaded = $true
        }
        else {
            Write-Info "Skipping model downloads (--SkipModels specified)"
        }
        
        $comfyUIInstalled = $true
    }
    else {
        Write-Info "Skipping ComfyUI installation (--SkipComfyUI specified)"
    }
    
    # Show summary
    Show-Summary -OllamaInstalled $ollamaInstalled -PythonChecked $pythonChecked `
                  -ComfyUIInstalled $comfyUIInstalled -ModelsDownloaded $modelsDownloaded `
                  -ComfyUIPath $ComfyUIPath
}
catch {
    Write-Error "Installation failed: $_"
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}
