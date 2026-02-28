# Security Policy

## Supported Versions

Only the latest release is actively maintained and receives security fixes.

| Version | Supported |
|---|---|
| Latest release | ✅ |
| Older releases | ❌ |

## Reporting a Vulnerability

**Please do not open a public GitHub Issue for security vulnerabilities.**

Instead, report them via email:

> **gabrieldubois.eng@gmail.com**

Include the following in your report:
- A description of the vulnerability and its potential impact
- Step-by-step reproduction instructions
- Any proof-of-concept code or screenshots

### What to expect

| Step | Timeline |
|---|---|
| Acknowledgement | Within 72 hours |
| Initial assessment | Within 7 days |
| Fix / mitigation | Depends on severity; critical issues targeted within 14 days |
| Public disclosure | After a fix is released and users have had reasonable time to update |

We follow a **coordinated disclosure** model. We'll credit reporters in the release notes
unless you prefer to remain anonymous.

## Scope

This application runs locally on Windows and reads from the system clipboard.
Relevant security concerns include:

- Sensitive data stored in clipboard history (passwords, tokens, etc.)
- The optional persist-to-disk feature writing history to `%APPDATA%\ClipboardManager`
- Local privilege escalation via the installer or startup entry

Out of scope: issues in third-party dependencies (report those upstream).
