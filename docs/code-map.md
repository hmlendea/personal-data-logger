# Code Map

This document maps production source areas back to the documentation that explains them. The primary conceptual index is [feature-map.md](feature-map.md); system-level structure is documented in [ARCHITECTURE.md](../ARCHITECTURE.md).

| Source area | Responsibility and principal symbols | Documentation | Tests |
| --- | --- | --- | --- |
| [PersonalDataLogger/Program.cs](../PersonalDataLogger/Program.cs) | Composition root; `Program.Main`, `CreateIOC` | [ARCHITECTURE.md](../ARCHITECTURE.md), [feature-map.md](feature-map.md) | No direct tests |
| [PersonalDataLogger/Service/EmailWorker.cs](../PersonalDataLogger/Service/EmailWorker.cs) | Polling, connection retries, checkpoint, age filtering, sender routing; `WatchEmails`, `GetAvailableEmails`, `ProcessAvailableEmails`, `LoadCheckpoint`, `SaveCheckpoint` | [ARCHITECTURE.md](../ARCHITECTURE.md), [feature-map.md](feature-map.md) | [EmailWorkerTests.cs](../PersonalDataLogger.UnitTests/Service/EmailWorkerTests.cs): connection retry and lifecycle coverage |
| [PersonalDataLogger/Service/Processors/EmailProcessor.cs](../PersonalDataLogger/Service/Processors/EmailProcessor.cs) | IMAP lifecycle and UID retrieval; `LogIn`, `GetAvailableEmails`, `LogOut` | [ARCHITECTURE.md](../ARCHITECTURE.md), [feature-map.md](feature-map.md) | No direct tests |
| [PersonalDataLogger/Service/TimedLogWorker.cs](../PersonalDataLogger/Service/TimedLogWorker.cs) | Timed-log task orchestration; `WatchTimedLogs`, `WatchTimedLog` | [ARCHITECTURE.md](../ARCHITECTURE.md), [feature-map.md](feature-map.md) | No direct tests |
| [PersonalDataLogger/Service/ProfiBalanceTimedLog.cs](../PersonalDataLogger/Service/ProfiBalanceTimedLog.cs) | Daily scheduling and event construction; `GetNextExecution`, `Execute` | [ARCHITECTURE.md](../ARCHITECTURE.md), [feature-map.md](feature-map.md) | `ProfiBalanceTimedLogTests` |
| [PersonalDataLogger/Service/Processors/](../PersonalDataLogger/Service/Processors/) | Platform email transformations and IMAP adapter | [feature-map.md](feature-map.md) | Matching `*ProcessorTests` classes |
| [PersonalDataLogger/Client/PersonalLogManagerService.cs](../PersonalDataLogger/Client/PersonalLogManagerService.cs) | Common event API, timezone conversion, HMAC request | [ARCHITECTURE.md](../ARCHITECTURE.md), [feature-map.md](feature-map.md) | `PersonalLogManagerServiceTests` |
| [PersonalDataLogger/Client/ProfiAccountsService.cs](../PersonalDataLogger/Client/ProfiAccountsService.cs) | Profi API request, response mapping, enabled balance sum | [ARCHITECTURE.md](../ARCHITECTURE.md), [feature-map.md](feature-map.md) | `ProfiAccountsServiceTests` |
| [PersonalDataLogger/Client/](../PersonalDataLogger/Client/) | API interfaces and DTOs | [feature-map.md](feature-map.md) | `GetProfiAccountsRequestTests`, client tests |
| [PersonalDataLogger/Configuration/](../PersonalDataLogger/Configuration/) | Settings POCOs and Profi configuration predicate | [ARCHITECTURE.md](../ARCHITECTURE.md), [feature-map.md](feature-map.md) | No direct binding tests |
| [PersonalDataLogger/Service/Models/](../PersonalDataLogger/Service/Models/) | Email, batch, and checkpoint data definitions | [ARCHITECTURE.md](../ARCHITECTURE.md), [feature-map.md](feature-map.md) | No direct model tests |
| [PersonalDataLogger/Logging/](../PersonalDataLogger/Logging/) | Operation and structured log-key definitions | [ARCHITECTURE.md](../ARCHITECTURE.md), [feature-map.md](feature-map.md) | Indirect coverage only |
| [PersonalDataLogger/appsettings.json](../PersonalDataLogger/appsettings.json) | Runtime settings and secret placeholders | [README.md](../README.md), [ARCHITECTURE.md](../ARCHITECTURE.md), [feature-map.md](feature-map.md) | No configuration tests |
| [PersonalDataLogger/PersonalDataLogger.csproj](../PersonalDataLogger/PersonalDataLogger.csproj) | Executable target and package references | [README.md](../README.md), [ARCHITECTURE.md](../ARCHITECTURE.md) | CI build |
| [PersonalDataLogger.UnitTests/](../PersonalDataLogger.UnitTests/) | NUnit unit-test project | [feature-map.md](feature-map.md), [documentation-coverage.md](documentation-coverage.md) | The files in this directory |
| [.github/workflows/dotnet.yml](../.github/workflows/dotnet.yml) | CI restore/build/test | [README.md](../README.md), [feature-map.md](feature-map.md) | Workflow execution |
| [release.sh](../release.sh) | Release delegation | [README.md](../README.md), [feature-map.md](feature-map.md) | No automated release test |

## Unmapped Production Behaviour

No substantial production source file is intentionally omitted from the map. Configuration POCOs, DTOs, interfaces, logging definitions, and test project metadata are grouped by directory where their semantics are uniform; individual classes are named in [feature-map.md](feature-map.md) or [ARCHITECTURE.md](../ARCHITECTURE.md).