# VideoGenerationApp

![Build and Test](https://github.com/jbebesi/VideoGenerationApp/actions/workflows/build-and-test.yml/badge.svg)

A video generation application built with .NET 8.0 and Blazor.

## Continuous Integration

This project uses GitHub Actions for automated building and testing. The workflow runs on:
- Push to `main` or `master` branches
- Pull requests targeting `main` or `master` branches

### Build Status

All pull requests must pass the build and test checks before merging.

## Development

### Prerequisites

- .NET 8.0 SDK or later

### Building the Project

```bash
dotnet restore
dotnet build --configuration Release
```

### Running Tests

```bash
dotnet test --configuration Release
```