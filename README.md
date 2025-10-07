# VideoGenerationApp

![Build and Test](https://github.com/jbebesi/VideoGenerationApp/actions/workflows/build-and-test.yml/badge.svg)

A video generation application built with .NET 8.0 and Blazor.

## Continuous Integration

This project uses GitHub Actions for automated building and testing. The workflow runs on:
- Push to `main` or `master` branches
- Pull requests targeting `main` or `master` branches

### Build Status

All pull requests must pass the build and test checks before merging.

## Quick Start

### Automated Installation

The easiest way to set up all dependencies is to use our automated installation script:

**Windows:**
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install.ps1
```

**Linux/macOS:**
```bash
pwsh ./scripts/install.ps1
```

This will install:
- Ollama (AI model runtime)
- Python 3.10+ (for ComfyUI)
- ComfyUI (Stable Diffusion workflows)
- Required AI models

For more details, see [scripts/README.md](scripts/README.md).

## Development

### Prerequisites

- .NET 8.0 SDK or later
- Ollama (for local AI models)
- Python 3.10+ (for ComfyUI)
- ComfyUI (for audio/video generation)

You can install these manually or use the automated installation script above.

### Building the Project

```bash
dotnet restore
dotnet build --configuration Release
```

### Running Tests

```bash
dotnet test --configuration Release
```