[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/fund.html)
[![Latest Release](https://img.shields.io/github/v/release/hmlendea/personal-data-logger)](https://github.com/hmlendea/personal-data-logger/releases/latest)
[![Build Status](https://github.com/hmlendea/personal-data-logger/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/personal-data-logger/actions/workflows/dotnet.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://gnu.org/licenses/gpl-3.0)

# Personal Data Logger

Personal Data Logger is a .NET 10 background-style console service that polls an IMAP inbox, processes supported email events, executes timed logs, and forwards the resulting events to a Personal Log Manager API.

## What It Does

- Connects to an IMAP server and reads inbox emails.
- Polls continuously (every 5 seconds).
- Keeps a persistent checkpoint based on IMAP UID so emails are not processed twice.
- Applies a maximum email age filter (`ImapSettings.MaxEmailAge`).
- Retrieves Profi Bot Server account balances daily at 06:30 local time and logs the total balance of enabled accounts.
- Processes Opsgenie subjects:
	- `Your on-call rotation ... is starting now` -> sends `WorkOnCallShiftBeginning`
	- `Your on-call rotation ... is ending now` -> sends `WorkOnCallShiftEnding`
- Converts timestamps to Romanian time before sending to the API.

## Requirements

- .NET SDK/runtime targeting `net10.0`
- Access to an IMAP mailbox that receives the Netflix confirmation emails
- Access to the Personal Log Manager API
- Network access to both the IMAP server and the Personal Log Manager API

## Project Structure

- `Program.cs`: bootstrapping, configuration binding, DI registration, service start.
- `Service/EmailWorker.cs`: polling loop, checkpoint load/save, filtering, dispatch.
- `Service/TimedLogWorker.cs`: concurrent execution of registered timed logs.
- `Service/ProfiBalanceTimedLog.cs`: daily Profi balance retrieval and log creation.
- `Service/Processors/EmailProcessor.cs`: IMAP connectivity and email retrieval.
- `Service/Processors/OpsGenieEmailProcessor.cs`: subject-based Opsgenie event handling.
- `Client/PersonalLogManagerService.cs`: outbound API call and timezone conversion.
- `Client/ProfiAccountsService.cs`: authenticated Profi Bot Server account retrieval.

## Configuration

Configure `appsettings.json` before running.

Example:

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

Notes:

- `hmacSharedSecretKey` must match the settings class property name.
- `imapSettings.port` should be a number, not a string.
- `maxEmailAge` is in seconds.
- `profiBotServerSettings.accountName` is the account label stored in the balance log.
- `profiBotServerSettings.accountsEndpoint` uses `{username}` as the escaped username token.
- The Profi User API Key and HMAC shared secret must be supplied through secure environment-specific configuration.

## Timed Logs

The Profi balance log executes each day at 06:30 in the host's local timezone. It retrieves `/Users/{username}/accounts`, sums `balance` for records where `isEnabled` is `true`, and sends `BotsTotalBalanceMeasurement` with the platform, account, amount, and currency data.

Additional timed logs can implement `ITimedLog` and be registered with dependency injection.

## Usage

```bash
dotnet run
```

## Checkpointing and Idempotency

The service stores progress in `imap-checkpoint.json` (created in the app base directory).

- `UidValidity`: mailbox identity guard.
- `LastProcessedUid`: the last email UID that was checked.

Behavior:

- On normal operation, only emails with UID greater than `LastProcessedUid` are fetched.
- If mailbox `UidValidity` changes, checkpoint is reset safely.
- Checkpoint is updated after each checked email.

To force reprocessing from the beginning, stop the service and delete `imap-checkpoint.json`.

## Timezone Handling

Outgoing logs are sent in Romanian time (`Europe/Bucharest`).

- Linux/macOS timezone id: `Europe/Bucharest`
- Windows fallback timezone id: `GTB Standard Time`

## Logging

The project uses NuciLog.

Typical operation logs include:

- startup/shutdown
- IMAP login/logout
- email processing progress (UID/subject/date)
- outbound API request status

## Security Notes

- Do not commit secrets in `appsettings.json`.
- Prefer environment-specific config management for API keys and mailbox credentials.
- Review any release automation scripts before executing them.

## Development

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run
```

### Release

The repository includes `release.sh`, which delegates to the upstream deployment script used by the project maintainer.

```bash
bash ./release.sh 1.0.0
```

This script downloads and executes an external release helper from: `https://raw.githubusercontent.com/hmlendea/deployment-scripts/master/release/dotnet/10.0.sh`

**Note:** Piping into `bash` is an intensely controversial topic. Please review any external scripts before running them in your environment!

## Contributing

Contributions are welcome.

Please:

- keep changes cross-platform
- keep pull requests focused and consistent with existing style
- update documentation when behaviour changes
- add or update tests for new behaviour

## License

Licensed under the GNU General Public License v3.0 or later.
See [LICENSE](./LICENSE) for details.