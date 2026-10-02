# Feature Map

This map links each substantial capability to its entry point, implementation, configuration, composition, data, tests, and operational support. Links are repository-relative and intentionally point to source files rather than unstable line numbers.

## Email Ingestion and Dispatch

| Responsibility | Location | Symbol or detail |
| --- | --- | --- |
| Entry point | [PersonalDataLogger/Program.cs](../PersonalDataLogger/Program.cs) | `Program.Main` starts `IEmailWorker.WatchEmails` |
| Principal implementation | [PersonalDataLogger/Service/EmailWorker.cs](../PersonalDataLogger/Service/EmailWorker.cs) | `EmailWorker.WatchEmails`, `ProcessAvailableEmails` |
| IMAP adapter | [PersonalDataLogger/Service/Processors/EmailProcessor.cs](../PersonalDataLogger/Service/Processors/EmailProcessor.cs) | `EmailProcessor.LogIn`, `GetAvailableEmails`, `LogOut` |
| Data definitions | [PersonalDataLogger/Service/Models/AvailableEmail.cs](../PersonalDataLogger/Service/Models/AvailableEmail.cs), [AvailableEmailBatch.cs](../PersonalDataLogger/Service/Models/AvailableEmailBatch.cs) | Email and batch records |
| Checkpoint persistence | [PersonalDataLogger/Service/Models/EmailCheckpoint.cs](../PersonalDataLogger/Service/Models/EmailCheckpoint.cs), [EmailWorker.cs](../PersonalDataLogger/Service/EmailWorker.cs) | `LoadCheckpoint`, `SaveCheckpoint`; `imap-checkpoint.json` |
| Configuration | [PersonalDataLogger/Configuration/ImapSettings.cs](../PersonalDataLogger/Configuration/ImapSettings.cs), [PersonalDataLogger/appsettings.json](../PersonalDataLogger/appsettings.json) | Server, credentials, port, `MaxEmailAge` |
| Registration | [PersonalDataLogger/Program.cs](../PersonalDataLogger/Program.cs) | `CreateIOC`; singleton `IEmailProcessor` and `IEmailWorker` |
| Tests | [EmailWorkerTests.cs](../PersonalDataLogger.UnitTests/Service/EmailWorkerTests.cs) | Connection retries, UID retention across failed fetches, non-retryable failures, and worker lifecycle; no direct `EmailProcessor` tests |
| Integration/e2e | None | IMAP-to-API process has no automated integration test |

The process loads the checkpoint, resets it when IMAP UID validity changes, filters emails by age, routes by sender substring, dispatches a platform processor, and saves the last UID after each email. Unmatched and over-age messages advance the checkpoint without producing an event.

IMAP connection failures during login or retrieval are retried after five seconds with a fresh client and the same requested UID. Retries have no fixed limit and do not advance the checkpoint. Authentication failures and errors outside IMAP retrieval are not covered by this retry policy.

## Platform Email Transformations

| Platform | Implementation | Data sink | Tests |
| --- | --- | --- |
| AliExpress verification | [AliExpressProcessor.cs](../PersonalDataLogger/Service/Processors/AliExpressProcessor.cs), `AliExpressProcessor.ProcessEmail` | `IPersonalLogManagerService` | [AliExpressProcessorTests.cs](../PersonalDataLogger.UnitTests/Service/Processors/AliExpressProcessorTests.cs), `AliExpressProcessorTests` |
| Gandi new-device login | [GandiProcessor.cs](../PersonalDataLogger/Service/Processors/GandiProcessor.cs), `GandiProcessor.ProcessEmail` | `IPersonalLogManagerService` | [GandiProcessorTests.cs](../PersonalDataLogger.UnitTests/Service/Processors/GandiProcessorTests.cs), `GandiProcessorTests` |
| PayPal new-device login | [PayPalProcessor.cs](../PersonalDataLogger/Service/Processors/PayPalProcessor.cs), `PayPalProcessor.ProcessEmail` | `IPersonalLogManagerService` | [PayPalProcessorTests.cs](../PersonalDataLogger.UnitTests/Service/Processors/PayPalProcessorTests.cs), `PayPalProcessorTests` |
| Profi prize notification | [ProfiProcessor.cs](../PersonalDataLogger/Service/Processors/ProfiProcessor.cs), `ProfiProcessor.ProcessEmail` | `IPersonalLogManagerService` | [ProfiProcessorTests.cs](../PersonalDataLogger.UnitTests/Service/Processors/ProfiProcessorTests.cs), `ProfiProcessorTests` |
| Opsgenie on-call change | [OpsGenieEmailProcessor.cs](../PersonalDataLogger/Service/Processors/OpsGenieEmailProcessor.cs), `OpsGenieEmailProcessor.ProcessEmail` | `IPersonalLogManagerService` | [OpsGenieEmailProcessorTests.cs](../PersonalDataLogger.UnitTests/Service/Processors/OpsGenieEmailProcessorTests.cs), `OpsGenieEmailProcessorTests` |

Each processor receives `AvailableEmail`, recognises platform-specific subject/body signals, extracts fields, and sends a template/data dictionary through the common client. Interface contracts are in the corresponding `I*Processor.cs` files. Malformed-email, routing, and end-to-end coverage is incomplete; the processor unit tests are the direct coverage boundary.

## Personal Log Manager Integration

| Responsibility | Location | Symbol or detail |
| --- | --- | --- |
| Client contract | [IPersonalLogManagerService.cs](../PersonalDataLogger/Client/IPersonalLogManagerService.cs) | `SendPersonalLogToManager` overloads |
| Principal implementation | [PersonalLogManagerService.cs](../PersonalDataLogger/Client/PersonalLogManagerService.cs) | HMAC request construction and `ConvertToRomanianTime` |
| Request model | [StoreLogRequest.cs](../PersonalDataLogger/Client/StoreLogRequest.cs) | Event template, timestamp, and data payload |
| Registration | [Program.cs](../PersonalDataLogger/Program.cs) | `IPersonalLogManagerService` and `INuciApiClient` singletons |
| Configuration | [PersonalLogManagerSettings.cs](../PersonalDataLogger/Configuration/PersonalLogManagerSettings.cs), [appsettings.json](../PersonalDataLogger/appsettings.json) | Base URL, API key, client ID, HMAC key |
| Callers | Platform processors and [ProfiBalanceTimedLog.cs](../PersonalDataLogger/Service/ProfiBalanceTimedLog.cs) | Event submission |
| Tests | [PersonalLogManagerServiceTests.cs](../PersonalDataLogger.UnitTests/Client/PersonalLogManagerServiceTests.cs), `PersonalLogManagerServiceTests` | Authentication, payload, and timezone cases |
| Integration/e2e | None | Live API boundary is not automated |

The client converts timestamps to `Europe/Bucharest`, sends the standard event request, and logs the operation. Failed external calls are handled at the client boundary and are not queued for retry.

## Profi Account Balance Timed Log

| Responsibility | Location | Symbol or detail |
| --- | --- | --- |
| Conditional entry point | [Program.cs](../PersonalDataLogger/Program.cs) | `CreateIOC` registers and `Main` starts timed logging when `ProfiBotServerSettings.IsConfigured` |
| Scheduler | [TimedLogWorker.cs](../PersonalDataLogger/Service/TimedLogWorker.cs) | `WatchTimedLogs`, `WatchTimedLog` |
| Principal implementation | [ProfiBalanceTimedLog.cs](../PersonalDataLogger/Service/ProfiBalanceTimedLog.cs) | `GetNextExecution`, `Execute` |
| Account client | [ProfiAccountsService.cs](../PersonalDataLogger/Client/ProfiAccountsService.cs) | `GetEnabledAccountsBalance`, account retrieval and response mapping |
| Data models | [ProfiAccount.cs](../PersonalDataLogger/Client/ProfiAccount.cs), [GetProfiAccountsRequest.cs](../PersonalDataLogger/Client/GetProfiAccountsRequest.cs), [GetProfiAccountsResponse.cs](../PersonalDataLogger/Client/GetProfiAccountsResponse.cs) | Request, response, account, and balance data |
| Configuration | [ProfiBotServerSettings.cs](../PersonalDataLogger/Configuration/ProfiBotServerSettings.cs), [appsettings.json](../PersonalDataLogger/appsettings.json) | Endpoint, username, account label, API credentials |
| Tests | [ProfiBalanceTimedLogTests.cs](../PersonalDataLogger.UnitTests/Service/ProfiBalanceTimedLogTests.cs), `ProfiBalanceTimedLogTests`; [ProfiAccountsServiceTests.cs](../PersonalDataLogger.UnitTests/Client/ProfiAccountsServiceTests.cs), `ProfiAccountsServiceTests` | Schedule, aggregation, HTTP and response branches |
| Integration/e2e | None | Profi Bot Server boundary is not automated |

The timed log executes at the next local 06:30, obtains accounts, sums enabled-account balances, formats a `BotsTotalBalanceMeasurement` event in invariant culture, and sends it through the common log client.

## Configuration, Logging, and Composition

| Capability | Implementation and configuration | Tests |
| --- | --- | --- |
| Configuration binding | [Program.cs](../PersonalDataLogger/Program.cs), [Configuration/](../PersonalDataLogger/Configuration/), [appsettings.json](../PersonalDataLogger/appsettings.json) | No startup-binding tests |
| Dependency registration | [Program.cs](../PersonalDataLogger/Program.cs), `Program.CreateIOC` | No composition-root tests |
| Structured operations | [MyOperation.cs](../PersonalDataLogger/Logging/MyOperation.cs), [MyLogInfoKey.cs](../PersonalDataLogger/Logging/MyLogInfoKey.cs) | Exercised indirectly by service tests |
| CI verification | [.github/workflows/dotnet.yml](../.github/workflows/dotnet.yml) | Restore, build, and `dotnet test` on push/PR |
| Release packaging | [release.sh](../release.sh) | External deployment helper; no automated release test |

## Algorithms

### Email Checkpoint and Routing

- **Purpose:** process each IMAP UID once per UID-validity generation.
- **Inputs:** `EmailCheckpoint`, `AvailableEmailBatch`, `ImapSettings.MaxEmailAge`.
- **Procedure:** compare UID validity; reset to UID zero on change; calculate `UtcNow - max(0, MaxEmailAge)`; route recent messages by sender substring; persist each message UID.
- **Branches and edge cases:** missing/corrupt checkpoint falls back to a fresh checkpoint; UID reset re-fetches; negative age becomes zero; over-age and unknown senders are skipped but checkpointed.
- **Implementation:** [EmailWorker.cs](../PersonalDataLogger/Service/EmailWorker.cs), `ProcessAvailableEmails`, `LoadCheckpoint`, `SaveCheckpoint`.
- **Tests:** no direct tests; relevant model and processor tests do not validate the orchestration algorithm.

### Daily Timed-Log Scheduling

- **Purpose:** select the next local 06:30 execution.
- **Inputs:** current `DateTimeOffset`.
- **Output:** a `DateTimeOffset` today or tomorrow with the local UTC offset.
- **Procedure:** construct today at `RunTime`; add one day when the time has passed; apply the local offset.
- **Invariant:** returned execution is strictly later than the supplied local time.
- **Implementation:** [ProfiBalanceTimedLog.cs](../PersonalDataLogger/Service/ProfiBalanceTimedLog.cs), `GetNextExecution`; scheduling loop in [TimedLogWorker.cs](../PersonalDataLogger/Service/TimedLogWorker.cs), `WatchTimedLog`.
- **Tests:** [ProfiBalanceTimedLogTests.cs](../PersonalDataLogger.UnitTests/Service/ProfiBalanceTimedLogTests.cs), `ProfiBalanceTimedLogTests`.

### Enabled-Account Balance Aggregation

- **Purpose:** calculate the balance reported by the scheduled Profi event.
- **Inputs:** `ProfiAccount[]` returned by the Profi Bot Server.
- **Output:** decimal sum of accounts where `IsEnabled` is true.
- **Implementation:** [ProfiAccountsService.cs](../PersonalDataLogger/Client/ProfiAccountsService.cs), `CalculateEnabledAccountsBalance` and `GetEnabledAccountsBalance`.
- **Tests:** [ProfiAccountsServiceTests.cs](../PersonalDataLogger.UnitTests/Client/ProfiAccountsServiceTests.cs), `ProfiAccountsServiceTests`.

## Traceability Gaps

- No automated integration or end-to-end tests cover IMAP, external HTTP APIs, configuration binding, or the complete email-to-event process.
- `EmailWorker`, `EmailProcessor`, `TimedLogWorker`, and `Program.CreateIOC` have no direct tests.
- The processor tests do not substitute for routing, checkpoint, persistence, or live protocol tests.