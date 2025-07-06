# Semantic Release for .NET
A command-line tool for automating version management and package publishing following semantic versioning principles in .NET projects.
## Features
- Automated version management based on commit messages
- Configurable release workflow
- CI/CD integration support
- Dry-run capability for testing
- Flexible configuration options

## Installation
``` bash
dotnet tool install --global dotnet-semantic-release
```
## Usage
Basic usage:
``` bash
dotnet-semantic-release
```
### Command Line Options
- `--dry-run`: Run without making any changes
- `--ci`: Run in CI mode
- `--config-path` or `-c`: Path to the configuration file
- `--working-dir` or `-w`: Path to the working directory (defaults to current directory)

### Examples
Run in dry-run mode:
``` bash
dotnet-semantic-release --dry-run
```
Specify a custom configuration file:
``` bash
dotnet-semantic-release --config-path ./custom-config.json
```
Run in CI mode with a specific working directory:
``` bash
dotnet-semantic-release --ci --working-dir ./my-project
```
## Configuration
The tool can be configured using a JSON configuration file. By default, it looks for configuration in the working directory, but you can specify a custom path using the `--config-path` option.
### Default Configuration Location
The tool will look for configuration in the following locations:
1. Custom path specified via `--config-path`
2. Working directory specified via `--working-dir`
3. Current directory (if no working directory specified)

## CI/CD Integration
The tool provides built-in support for CI/CD environments through the `--ci` flag. When running in CI mode, the tool adapts its behavior for automated environments.
### Using in GitHub Actions
Example workflow:
``` yaml
name: Release
on:
  push:
    branches:
      - main

jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
      - name: Run Semantic Release
        run: dotnet-semantic-release --ci
```
## Development
### Prerequisites
- .NET 8.0 SDK or later
- Git

### Building from Source
``` bash
git clone https://github.com/yourusername/dotnet-semantic-release.git
cd dotnet-semantic-release
dotnet build
```
### Running Tests
``` bash
dotnet test
```
## Contributing
Contributions are welcome! Please feel free to submit a Pull Request.
## License
This project is licensed under the terms found in the LICENSE file in the root directory.
## Troubleshooting
If you encounter issues:
1. Ensure you're using the latest version
2. Try running with `--dry-run` first to test your configuration
3. Check that your working directory contains the necessary project files
4. Verify your configuration file is properly formatted

## Support
For issues and feature requests, please use the GitHub issue tracker.
