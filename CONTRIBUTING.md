# Contributing to Clipboard Manager

Thank you for considering a contribution! This document outlines the conventions
and workflow to follow so that pull requests are easy to review and merge.

---

## Code Style

- **Language:** C# 12 targeting .NET 8
- **Namespaces:** file-scoped (`namespace Foo;`)
- **Type inference:** use `var` where the right-hand type is obvious
- **Formatting:** 4-space indent, `{ }` on same line for control flow
- **Naming:** PascalCase for types/methods, camelCase with `_` prefix for private fields
- **XAML:** one attribute per line for elements with more than two attributes

Running `dotnet build` must produce **zero warnings** before opening a PR.

---

## Branch Naming

| Prefix | When to use |
|---|---|
| `feature/short-description` | New functionality |
| `fix/short-description` | Bug fix |
| `chore/short-description` | Tooling, CI, docs, dependency updates |
| `refactor/short-description` | Code restructuring without behaviour change |

Branch off `main` and target `main` with your PR.

---

## Commit Messages

Use the imperative mood in the subject line (≤ 72 characters):

```
Add export-as-CSV option to HistoryWindow
Fix null-ref when clipboard contains empty file list
Bump xunit to 2.9.3
```

- No day numbers or session identifiers in commit messages
- Reference issues with `Fixes #123` or `Closes #123` in the body

---

## Pull Request Checklist

Before submitting a PR, make sure all of the following are true:

- [ ] `dotnet build -c Release` succeeds with **zero errors and zero new warnings**
- [ ] `dotnet test` passes (all tests green)
- [ ] New behaviour is covered by at least one unit test (if testable without a live WPF runtime)
- [ ] README is updated if the feature adds or changes user-visible behaviour
- [ ] No temporary debug code, commented-out blocks, or TODO left in production paths

---

## Setting Up a Development Environment

### Prerequisites
- Windows 10 or 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git

### Steps

```powershell
git clone https://github.com/GetGabed/Clipboard-Manager.git
cd Clipboard-Manager
dotnet restore
dotnet build
dotnet test
dotnet run --project src\ClipboardManager
```

---

## Reporting Issues

Use the GitHub Issue templates:
- **Bug report** — crashes, wrong behaviour, UI glitches
- **Feature request** — new ideas or improvements

---

## Licence

By contributing you agree that your code will be made available under the
[MIT Licence](LICENSE) that covers this project.
