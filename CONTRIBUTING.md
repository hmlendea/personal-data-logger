# Contributing to Personal Data Logger

This document covers the guidelines and processes for contributing to this project, including how to report issues, suggest enhancements, submit code changes, and follow project standards.

## 📑 Table of Contents

- [How to Contribute](#-how-to-contribute)
  - [Reporting Issues](#reporting-issues)
  - [Suggesting Enhancements](#suggesting-enhancements)
  - [Code Contributions](#code-contributions)
    - [Prerequisites](#prerequisites)
    - [Development Setup](#development-setup)
    - [Making Changes](#making-changes)
    - [Pull Request Guidelines](#pull-request-guidelines)
  - [Code Style](#code-style)
  - [Testing](#testing)
  - [Documentation](#documentation)
- [Security](#-security)
- [License](#-license)

## 🤝 How to Contribute

### Reporting Issues

- Search existing issues first.
- Use the issue templates if available.
- Provide clear reproduction steps.
- Include environment details.

### Suggesting Enhancements

- Check the roadmap and existing discussions.
- Explain the use case and expected behaviour.
- Consider implementation complexity.

### Code Contributions

#### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

#### Development Setup

```bash
# Clone the repository
git clone https://github.com/hmlendea/personal-data-logger.git
cd personal-data-logger

# Install dependencies
dotnet restore personal-data-logger.slnx
```

#### Making Changes

1. Fork the repository.
2. Create a feature branch: `git checkout -b feature/your-feature-name`
3. Make your changes.
4. Run tests: `dotnet test personal-data-logger.slnx`
5. Run linters: `dotnet format personal-data-logger.slnx --verify-no-changes`
6. Commit with clear and descriptive messages.
7. Push to your fork.
8. Open a Pull Request.

#### Pull Request Guidelines

- Target the `master` branch.
- Keep PRs focused and atomic.
- Revise documentation if applicable.
- Include tests for any new functionality.
- Ensure the CI checks pass.

### Code Style

Follow the project's coding standards:
- Use `dotnet format` for code formatting
- Follow C# naming conventions (PascalCase for types/members, camelCase for parameters/locals)
- Use `var` when the type is obvious from the right-hand side
- Prefer expression-bodied members for simple properties and methods
- Run `dotnet format personal-data-logger.slnx --verify-no-changes` before committing.

### Testing

```bash
# Run all tests
dotnet test personal-data-logger.slnx

# Run specific test project
dotnet test PersonalDataLogger.UnitTests/PersonalDataLogger.UnitTests.csproj
dotnet test PersonalDataLogger.IntegrationTests/PersonalDataLogger.IntegrationTests.csproj
```

### Documentation

- Revise relevant docs for changes.
- Follow the documentation style guide.
- Preview changes locally if possible.

## 🔒 Security

Report security vulnerabilities per the [Security Policy](SECURITY.md).

## 📜 Code of Conduct

All participants are expected to follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## 📄 License

By contributing, you agree that your contributions will be licensed under the GNU General Public License v3.0.