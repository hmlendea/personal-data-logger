[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/funding)
[![Latest Release](https://img.shields.io/github/v/release/hmlendea/personal-data-logger)](https://github.com/hmlendea/personal-data-logger/releases/latest)
[![Build Status](https://github.com/hmlendea/personal-data-logger/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/personal-data-logger/actions/workflows/dotnet.yml)
[![License](https://img.shields.io/github/license/hmlendea/personal-data-logger)](https://github.com/hmlendea/personal-data-logger/blob/master/LICENSE)

# Personal Data Logger

Personal Data Logger is a .NET 10 background service that polls an IMAP inbox, processes supported email events, executes timed logs, and forwards the resulting events to a Personal Log Manager API.

## 📑 Table of Contents

- [Capabilities](#-capabilities)
- [Usage](#-usage)
- [System Requirements](#-system-requirements)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Project Structure](#-project-structure)
- [Architecture](#-architecture)
- [Development](#-development)
- [Deployment](#-deployment)
- [Contributing](#-contributing)
- [Project Engagement](#-project-engagement)
- [License](#-license)

## ✨ Capabilities

- Connects to an IMAP server and reads inbox emails
- Polls continuously (every 5 seconds)
- Keeps a persistent checkpoint based on IMAP UID to prevent duplicate processing
- Applies maximum email age filtering
- Retrieves Profi Bot Server account balances daily at 06:30 local time
- Processes email events from multiple platforms (AliExpress, Gandi, PayPal, Profi)
- Detects and handles Opsgenie on-call rotation notifications
- Converts timestamps to Romanian time (`Europe/Bucharest`) before sending to the API

## 🚀 Usage

```bash
dotnet run --project PersonalDataLogger/PersonalDataLogger.csproj
```

## 🖥️ System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| .NET SDK/Runtime | .NET 10.0 | .NET 10.0 LTS |
| Operating System | Linux, macOS, Windows | Linux |
| RAM | 256 MB | 512 MB |
| Network | IMAP and API connectivity | Dedicated network access |

## 📦 Installation

### Installation from Source

```bash
git clone https://github.com/hmlendea/personal-data-logger.git
cd personal-data-logger
dotnet build personal-data-logger.slnx
dotnet run --project PersonalDataLogger/PersonalDataLogger.csproj
```

## ⚙️ Configuration

### Configuration Files

| File | Scope | Purpose |
|------|-------|---------|
| `appsettings.json` | Application | IMAP, API, and service settings |

### Secret Management

Sensitive configuration must be supplied through environment-specific configuration or secure secret-management systems. Never commit the following to version control:

- API keys and HMAC secrets for `personalLogManagerSettings`
- IMAP server credentials
- Profi Bot Server API keys and HMAC secrets

Configuration example:

```json
{
  "personalLogManagerSettings": {
    "baseUrl": "https://example.local",
    "apiKey": "<api-key>",
    "hmacSharedSecretKey": "<hmac-secret>",
    "clientId": "<client-id>"
  },
  "imapSettings": {
    "server": "imap.example.local",
    "port": 993,
    "username": "user@example.local",
    "password": "<password>",
    "maxEmailAge": 1800
  },
  "personalSettings": {
    "employerName": "My Employer"
  },
  "aliExpressSettings": {
    "emailAddress": "user@example.local"
  },
  "profiBotServerSettings": {
    "accountName": "Hori",
    "accountsEndpoint": "/Users/{username}/accounts",
    "baseUrl": "https://profi.example.local",
    "clientId": "Bruno",
    "hmacSharedSecretKey": "<hmac-secret>",
    "userApiKey": "<user-api-key>",
    "username": "hori"
  },
  "nuciLoggerSettings": {
    "minimumLevel": "Debug",
    "logFilePath": "logfile.log",
    "isFileOutputEnabled": true
  }
}
```

## 🗂️ Project Structure

The repository contains a .NET solution with separate application and unit-test projects.

### Projects and Packages

| Project | Type | Purpose |
|---------|------|---------|
| `PersonalDataLogger/PersonalDataLogger.csproj` | Executable | Background service for collecting and forwarding personal events |
| `PersonalDataLogger.UnitTests/PersonalDataLogger.UnitTests.csproj` | NUnit test project | Unit tests for the application services and processors |

### Directories

| Directory | Purpose |
|-----------|---------|
| `PersonalDataLogger/Service/` | Email polling, timed log execution, and email processors |
| `PersonalDataLogger/Client/` | API client services and Profi account management |
| `PersonalDataLogger/Configuration/` | Configuration model classes |
| `PersonalDataLogger/Logging/` | Logging operation and key definitions |

## 🏗️ Architecture

See the [architecture documentation](ARCHITECTURE.md) for the system context, principal components, runtime flows, ownership boundaries, dependencies, constraints, and extension points.

## 🛠️ Development

### Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Setup

```bash
git clone https://github.com/hmlendea/personal-data-logger.git
cd personal-data-logger
dotnet restore personal-data-logger.slnx
```

### Build

```bash
dotnet build personal-data-logger.slnx
```

### Run

```bash
dotnet run --project PersonalDataLogger/PersonalDataLogger.csproj
```

### Test

```bash
dotnet test personal-data-logger.slnx
```

### Release

The repository includes `release.sh`, which delegates to the upstream deployment script used by the project maintainer.

```bash
bash ./release.sh 1.0.0
```

This script downloads and executes an external release helper from `https://raw.githubusercontent.com/hmlendea/deployment-scripts/master/release/dotnet/10.0.sh`.

**Note:** Piping into `bash` is an intensely controversial topic. Please review any external scripts before running them in your environment!

### Dependencies

| Package | Version | Scope | Purpose |
|---------|---------|-------|---------|
| NuciLog | Latest | Runtime | Structured logging and diagnostics |

## 🚢 Deployment

The service is designed to run as a background process on a host with network access to the configured IMAP server and Personal Log Manager API. It maintains application state in `imap-checkpoint.json` to track processed emails and prevent duplicate processing.

### Configuration and Secrets

Sensitive configuration (API keys, HMAC secrets, IMAP passwords) must be supplied through environment-specific configuration and never committed to the repository. Use secure secret management systems for production deployments.

## 🤝 Contributing

You are welcome to submit any suggestion, feedback, or modification to this project.

When doing so, please:
- Maintain cross-platform compatibility
- Submit focused pull requests that conform to the existing code style
- Maintain your branch synchronised with `master`
- Revise the documentation when functionality changes
- Properly test all modifications, including edge cases and error conditions
- Add tests for additional or modified functionality

## 💝 Project Engagement

Discovered a problem or have a suggestion? [Open an issue](https://github.com/hmlendea/personal-data-logger/issues)!

If you find this project useful, consider [funding it](https://hmlendea.go.ro/funding) or starring ⭐️ it on GitHub!

[![Donate](https://raw.githubusercontent.com/hmlendea/readme-assets/master/donate_generic.png)](https://hmlendea.go.ro/funding)

## 📄 License

This project is being distributed under the `GNU General Public License v3.0`.
See [LICENSE](./LICENSE) for further information.