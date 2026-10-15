# Privacy and Personal Data

This document describes how Personal Data Logger handles personal data. It is a self-hosted .NET 10 background service that processes emails and forwards events to a Personal Log Manager API.

## 📑 Table of Contents
- [What This Document Covers](#-what-this-document-covers)
- [Self-Hosted Deployments](#-self-hosted-deployments)
- [Data We Handle](#-data-we-handle)
- [Processing and Use](#-processing-and-use)
- [Storage, Retention, and Deletion](#-storage-retention-and-deletion)
- [External Processing and Integrations](#-external-processing-and-integrations)
- [User Controls and Requests](#-user-controls-and-requests)
- [International Transfers](#-international-transfers)
- [Data Protection and Security](#-data-protection-and-security)
- [Document Changes](#-document-changes)
- [Contact](#-contact)

## 🔎 What This Document Covers

This document describes how Personal Data Logger at https://github.com/hmlendea/personal-data-logger handles personal data. It covers the application behaviour and verified integrations described below. Where the software is self-hosted, the instance operator may have separate responsibilities described below.

## 🏠 Self-Hosted Deployments

This document covers both the project maintainers (who do not operate an instance) and self-hosted instance operators (users who run the application). The instance operator controls the instance's configuration, local storage (including the checkpoint file), logs, backups, access controls, retention, and request handling. The application does not send any data to the project maintainers by default.

The application sends data to the configured Personal Log Manager API and, if configured, to the Profi Bot Server for fetching account balances. The instance operator can disable these flows by not configuring the respective services or by setting the configuration to point to a local instance.

## 📥 Data We Handle

### Data Provided to the Application
- IMAP server credentials (server, port, username, password) and email content (subject, sender, timestamp, body) from the connected IMAP mailbox.
- Personal Log Manager API credentials (base URL, API key, HMAC shared secret key, client ID).
- Profi Bot Server credentials (if configured: base URL, account name, client ID, HMAC shared secret key, user API key, username) and scheduled hours for balance checks.
- AliExpress email address (for filtering).
- Employer name (personal setting).

### Data Generated or Collected by the Application
- The application processes email content to extract relevant information (such as transaction details) and generates events to be sent to the Personal Log Manager API.
- The application generates and stores an IMAP checkpoint (last processed UID and UID validity) in a file named `imap-checkpoint.json` in the application's base directory to avoid reprocessing emails.

### Data Received from Integrations
- The application receives email data from the IMAP server.
- The application receives responses from the Personal Log Manager API when sending events.
- If configured, the application receives account balance data from the Profi Bot Server.

## 🧭 Processing and Use

The application processes the data described above for these verified functions:
- Reading and filtering emails from an IMAP mailbox — Email content (subject, sender, timestamp, body)
- Processing emails from known senders (AliExpress, Gandi, Opsgenie, PayPal, Profi Bot Server) to extract transaction or event details — Email content
- Sending processed events to a Personal Log Manager API — Extracted event details
- Fetching account balances from a Profi Bot Server at scheduled times — None (the request does not contain personal data, but the response may contain account balances which are personal data)
- Storing an IMAP checkpoint to track the last processed email — UID and UID validity (not personal data)

## 🗄️ Storage, Retention, and Deletion

The application stores the IMAP checkpoint in a file (`imap-checkpoint.json`) in the base directory. This file contains the last processed UID and UID validity (integers, not personal data). The checkpoint is updated after each email is processed and is retained until the application is run again. The instance operator can delete this file to reset the checkpoint.

No other personal data is stored by the application. Email content is processed in memory and not written to disk.

## 🔗 External Processing and Integrations

| Service or integration | Purpose | Data involved | Configuration or documentation |
|------------------------|---------|---------------|--------------------------------|
| IMAP Server | Receiving emails for processing | Email content (subject, sender, timestamp, body) | imapSettings in appsettings.json |
| Personal Log Manager API | Sending processed events | Extracted event details (e.g., transaction amount, description, date) | personalLogManagerSettings in appsettings.json |
| Profi Bot Server | Fetching account balances (if configured) | Account balance information (non-personal? but may be considered personal) | profiBotServerSettings in appsettings.json |

## ⚙️ User Controls and Requests

The following documented controls or request procedures are available:
- Deleting the IMAP checkpoint file (`imap-checkpoint.json`) — To reset the email processing checkpoint and reprocess emails from the beginning of the mailbox (subject to the maxEmailAge filter).
- Stopping the application — To cease all data processing and external communication.

## 🌍 International Transfers

The application may transfer data across national or regional borders depending on the location of the IMAP server, the Personal Log Manager API, and the Profi Bot Server (if configured). The instance operator controls these locations through the configuration and is responsible for ensuring compliance with applicable data transfer regulations.

## 🛡️ Data Protection and Security

The application relies on the instance operator to secure the configuration (including secrets) and the runtime environment. The application does not implement encryption of data at rest (beyond what the operator provides) or in transit (it uses standard IMAP and HTTPS connections, which are encrypted by the underlying libraries). The instance operator should ensure that the host system is secured, that the configuration is kept secret, and that network communications are trusted.

Note: The application logs may contain personal data (such as email subjects) if the logging level is set to include such information. The instance operator controls the logging configuration.

## 🔄 Document Changes

Update this document when application data flows, storage, integrations, or deployment responsibilities change. The current version is published at https://github.com/hmlendea/personal-data-logger/blob/master/PRIVACY.md.

## 📬 Contact

For questions about application data handling, contact the project maintainers. For a self-hosted instance, contact the instance operator, unless the project explicitly handles the request. Include [none]; do not send passwords, access tokens, or other secrets.