# Security Policy

This policy defines how to report vulnerabilities in Personal Data Logger, the scope of accepted reports, and the official releases that receive security maintenance. Please report suspected vulnerabilities privately and permit the maintainers time to validate and remediate them prior to public disclosure.

## 📑 Table of Contents

- [Table of Contents](#-table-of-contents)
- [Supported Versions](#-supported-versions)
- [Reporting a Vulnerability](#-reporting-a-vulnerability)
- [Scope](#-scope)
- [Disclosure Policy](#-disclosure-policy)

## 🛡️ Supported Versions

Use this table to indicate which project versions currently receive security maintenance.

| Version | Distribution Channel | Supported |
|---------|--------------------|-----------|
| Latest version | GitHub Releases | ✅ |
| Latest version | GitHub repository (`master` branch) | ✅ |
| Latest version | Unofficial binary mirrors | ❌ |
| Latest version | Unofficial source mirrors | ❌ |
| Latest version | Unofficial third-party distribution channels | ❌ |
| Preceding versions | Any distribution channel | ❌ |

## 🚨 Reporting a Vulnerability

Please do not disclose suspected vulnerabilities publicly before maintainers have had an opportunity to validate and remediate them.

To report a vulnerability:
- [GitHub Security Advisories](https://github.com/hmlendea/personal-data-logger/security/advisories)
- Contact the maintainers directly

## 📌 Scope

The subsequent report categories are in scope for this repository:
- Vulnerabilities in application code, configuration handling, or bundled dependencies
- Exposure or unauthorised modification of credentials, personal data, email content, or authenticated API communications caused by this project

The subsequent categories are out of scope unless explicitly stated to the contrary:
- Vulnerabilities confined to external email providers, third-party APIs, or infrastructure not maintained by this project
- Social engineering, physical access, or disruptive denial-of-service testing

## 📢 Disclosure Policy

This project follows coordinated disclosure:
1. Vulnerabilities are investigated privately.
2. A remediation plan is prepared and validated.
3. Public disclosure is published after a fix, mitigation, or agreed risk decision is available.
4. Credit is attributed in accordance with reporter preference and project policy.