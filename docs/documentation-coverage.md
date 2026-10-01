# Documentation Coverage

This is the traceability audit for the current repository. It records both directions: concept to implementation and implementation to documentation.

## Concept-to-Code Audit

| Concept | Entry point | Principal implementation | Data/configuration | Registration | Tests | Status |
| --- | --- | --- | --- | --- | --- | --- |
| Email polling and dispatch | `Program.Main` | `EmailWorker.WatchEmails`, `ProcessAvailableEmails` | `AvailableEmail*`, `EmailCheckpoint`, `ImapSettings` | `Program.CreateIOC` | None direct | Documented; orchestration gap |
| IMAP retrieval | `EmailWorker.WatchEmails` | `EmailProcessor.LogIn`, `GetAvailableEmails`, `LogOut` | `ImapSettings` | `Program.CreateIOC` | None direct | Documented; protocol gap |
| AliExpress processing | Email sender route | `AliExpressProcessor.ProcessEmail` | `AvailableEmail`, `AliExpressSettings` | `Program.CreateIOC` | `AliExpressProcessorTests` | Covered by unit tests only |
| Gandi processing | Email sender route | `GandiProcessor.ProcessEmail` | `AvailableEmail` | `Program.CreateIOC` | `GandiProcessorTests` | Covered by unit tests only |
| PayPal processing | Email sender route | `PayPalProcessor.ProcessEmail` | `AvailableEmail` | `Program.CreateIOC` | `PayPalProcessorTests` | Covered by unit tests only |
| Profi notification processing | Email sender route | `ProfiProcessor.ProcessEmail` | `AvailableEmail` | `Program.CreateIOC` | `ProfiProcessorTests` | Covered by unit tests only |
| Opsgenie processing | Email sender route | `OpsGenieEmailProcessor.ProcessEmail` | `AvailableEmail`, `PersonalSettings` | `Program.CreateIOC` | `OpsGenieEmailProcessorTests` | Covered by unit tests only |
| Personal Log Manager submission | Processors and `ProfiBalanceTimedLog.Execute` | `PersonalLogManagerService.SendPersonalLogToManager` | `StoreLogRequest`, `PersonalLogManagerSettings` | `Program.CreateIOC` | `PersonalLogManagerServiceTests` | Covered by unit tests; live boundary gap |
| Timed-log scheduling | `Program.Main` when Profi configured | `TimedLogWorker.WatchTimedLogs`, `WatchTimedLog` | `ITimedLog`, `ProfiBotServerSettings` | Conditional `CreateIOC` registration | No worker tests | Documented; orchestration gap |
| Profi balance event | Timed-log scheduler | `ProfiBalanceTimedLog.GetNextExecution`, `Execute` | `ProfiAccount`, settings, event dictionary | Conditional `CreateIOC` registration | `ProfiBalanceTimedLogTests` | Unit covered |
| Profi account retrieval and sum | `ProfiBalanceTimedLog.Execute` | `ProfiAccountsService.GetEnabledAccountsBalance` | Request/response/account DTOs | Conditional `CreateIOC` registration | `ProfiAccountsServiceTests`, `GetProfiAccountsRequestTests` | Unit covered; live boundary gap |
| Configuration binding | `Program.CreateIOC` | `ConfigurationBuilder`, `config.Bind` | `appsettings.json`, settings POCOs | Composition root | None | Documented; validation gap |
| Logging and operations | All workers/clients | `MyOperation`, `MyLogInfoKey`, injected `ILogger` calls | `NuciLoggerSettings` | `Program.CreateIOC` | Indirect only | Documented; no direct tests |
| CI and release | GitHub workflow/release script | `dotnet.yml`, `release.sh` | Project files and external release script | GitHub Actions | CI build/test | Documented |

## Algorithm Audit

| Algorithm | Implementation | Direct tests | Gap |
| --- | --- | --- | --- |
| UID checkpoint recovery and routing | `EmailWorker.ProcessAvailableEmails`, `LoadCheckpoint`, `SaveCheckpoint` | None | Branches, persistence, and sender routing are untested as a process |
| Next daily 06:30 execution | `ProfiBalanceTimedLog.GetNextExecution` | `ProfiBalanceTimedLogTests` | Time-zone/DST integration behaviour is not tested against a real clock |
| Enabled-account sum | `ProfiAccountsService.CalculateEnabledAccountsBalance` | `ProfiAccountsServiceTests` | No live response-contract test |
| Platform parsing/template mapping | Each `*Processor.ProcessEmail` | Matching `*ProcessorTests` | No complete email-to-API integration test |

## Inverse Audit

All substantial production areas are indexed in [code-map.md](code-map.md). The following documentation links are the reverse navigation targets:

| Implementation area | Documentation |
| --- | --- |
| Composition, workers, clients, processors | [ARCHITECTURE.md](../ARCHITECTURE.md), [feature-map.md](feature-map.md) |
| DTOs, models, configuration, logging | [feature-map.md](feature-map.md), [ARCHITECTURE.md](../ARCHITECTURE.md) |
| Unit tests | [feature-map.md](feature-map.md), this audit |
| CI and release | [feature-map.md](feature-map.md), [README.md](../README.md) |

## Explicit Gaps

- No integration or end-to-end test project exists.
- No direct tests cover `Program`, `EmailWorker`, `EmailProcessor`, or `TimedLogWorker`.
- No automated check validates configuration binding, required secrets, or conditional Profi registration.
- No automated release/deployment test validates the external script invoked by `release.sh`.
- External API failure, retry absence, checkpoint file permissions, and IMAP UID reset behaviour are not covered by end-to-end tests.

These are documented limitations, not claims of complete coverage.