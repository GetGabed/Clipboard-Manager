# GitHub Copilot Instructions — Clipboard Manager

These rules apply to **every session** in this workspace.

---

## Git Identity

Always use this identity for commits and tags:
```
name:  GetGabed
email: gabrieldubois.eng@gmail.com
```

Verify before committing:
```powershell
git config user.name "GetGabed"
git config user.email "gabrieldubois.eng@gmail.com"
```

---

## Versioning

Use semantic versioning — **never** include day numbers in tags or commit messages.

| Change type | Version bump | Example |
|---|---|---|
| Patch / minor fix | `0.0.X` | `v0.1.0` → `v0.1.1` |
| Feature / phase complete | `0.X.0` | `v0.1.0` → `v0.2.0` |
| Big / breaking release | `X.0.0` | `v0.9.0` → `v1.0.0` |

Current version history:
- `v0.1.0` — Project scaffold (WPF/MVVM structure, services, views, NuGet packages)
- `v0.2.0` — Core functionality (item promotion, ClearUnpinned, newest-first ordering, 13 unit tests, bug fixes)

Tag format:
```powershell
git tag -a "v0.X.0" -m "<short description of what this version delivers>"
git push origin v0.X.0
```

---

## Commit Message Style

- No day numbers (❌ `"Days 3-5: ..."`)
- Imperative mood, short summary line
- Examples:
  - ✅ `"Add item promotion and ClearUnpinned to storage service"`
  - ✅ `"Fix search bar cursor offset in Theme.xaml"`
  - ❌ `"Days 3-5: promote, ClearUnpinned, fixes"`

---

## Planning Docs

The `next-steps/` folder contains local planning notes and is in `.gitignore` (never pushed).
Always read `next-steps/current-status.md` at the start of a session for handoff state.
