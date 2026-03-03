# Changelog

All notable changes to Clipboard Manager are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versioning follows [Semantic Versioning](https://semver.org/).

---

## [1.0.0] — 2026-03-02

### Added
- **Excluded-app list** — clipboard is silently ignored when the foreground window belongs to a user-configured process (e.g. `keepass`, `1password`, `bitwarden`). Configurable in Settings.
- **Sensitive-content badge** — items whose text matches password/token/key patterns are tagged with a 🔒 badge in the history list.
- **Copy-and-close shortcut** — `Ctrl+Enter` copies the selected item and immediately closes the history window.
- **AutoExpireDays setting** — unpinned items older than N days are automatically removed on window open (0 = never, default).
- **Copy-frequency tracking** — each item's `CopyCount` increments every time it is re-copied from history; persisted across sessions.
- **Window position memory** — the history window remembers its last dragged position across sessions; falls back to cursor-relative placement when off-screen.
- **Dynamic tray menu label** — the "Open History" tray entry now shows the current hotkey string and updates live when the hotkey is rebound.
- **Filter status text** — the status bar shows `X of Y matches` when a search/pin filter is active, and `N items` otherwise.
- **Improved empty-state messaging** — clearer first-run hint text with hotkey reminder.
- **Excluded-apps and AutoExpireDays** exposed in Settings UI.
- `CHANGELOG.md` (this file).

### Changed
- **Search debounce** — search input waits 130 ms after the last keystroke before filtering, eliminating UI jank during fast typing.
- **`ClipboardStorageService.Items` caching** — the newest-first snapshot is built once per mutation, not on every access, reducing allocations significantly.
- **Settings window height** increased to accommodate new settings sections.
- **Empty-state copy** updated to "Copy anything — it will appear here instantly."

### Fixed
- Tray context menu always showed `Ctrl+Shift+V` even after rebinding the hotkey (regression from v0.9.0).

---

## [0.9.0] — 2026-02-28

### Added
- **Serilog** structured rolling-file logger (`%AppData%\ClipboardManager\logs\`). Clipboard content never logged.
- **DPAPI encryption** for `history.json` via `ProtectedData.Protect/Unprotect`. Migration fallback for plain-text v0.x files.
- **Live dark mode** — toggling Dark Mode in Settings applies immediately without restart.
- **Hotkey rebinding UI** — Settings window now has a capture TextBox; press any modifier+key combo to rebind.

### Fixed
- `IsPinned` mutation was not thread-safe; routed through `IClipboardStorageService.SetPinned()` under lock.
- `CircularBuffer<T>` was not truly generic (had `ClipboardItem` pattern matches); refactored to `Func<T,bool> isPinned` delegate.
- Image thumbnails had no size cap; now clamped to 640 px on the longest edge.
- Release CI workflow shipped without running tests; `dotnet test` step added.
- Scaffold leftovers (`MainWindow.xaml`, `MonitorIntervalMs`) removed.

---

## [0.8.0] — 2026-02-20

### Added
- **CI workflow** (`ci.yml`) — build + test + coverage upload on push/PR to `main`.
- **Release workflow** (`release.yml`) — publish win-x64 EXE, portable ZIP, and GitHub Release on `v*.*.*` tag push.
- **Inno Setup installer** (`installer/ClipboardManager.iss`) — no-elevation, per-user, startup task, uninstalls `%APPDATA%\ClipboardManager`.
- **Portable ZIP build** (`scripts/build-portable.ps1`).
- **Full release pipeline script** (`scripts/release.ps1`) — test → publish → ZIP → installer.
- **CONTRIBUTING.md**, **SECURITY.md**, bug report and feature request issue templates.
- **MIT License**.

---

## [0.7.0] — 2026-02-15

### Added
- Self-contained win-x64 publish profile (single-file, ReadyToRun).
- `StartupHelper.cs` — fixed `Assembly.Location` (empty in single-file apps) with `Environment.ProcessPath` fallback.

---

## [0.6.0] — 2026-02-10

### Added
- **History persistence** (`HistoryPersistenceService`) — text history saved to `%AppData%\ClipboardManager\history.json` on exit, restored on startup.
- **Settings persistence** (`SettingsService`) — JSON-backed settings with Reset Defaults.
- **Text transform operations** — 14 transforms (case, whitespace, base64, URL, HTML, reverse, count) via popup flyout.
- **File-drop capture** — shell icon extraction for file items.
- **Image capture** — thumbnail with hash-based dedup.

---

## [0.5.0] — 2026-02-05

### Added
- `CircularBuffer<T>` — fixed-capacity in-memory ring buffer with pin-aware eviction.
- Deduplication — re-copying an existing item promotes it to the top rather than duplicating it.
- `ClearUnpinned` command.
- Pin / Unpin with `P` key and toolbar button.
- Keyboard shortcuts: `Esc` close, `Enter` copy, `Delete` remove, `↓` list focus, `↑` to search.

---

## [0.2.0] — 2026-02-01

### Added
- Core clipboard monitor (`WM_CLIPBOARDUPDATE`) — event-driven, no polling.
- Global hotkey (`Ctrl+Shift+V` default) via `RegisterHotKey`.
- History window — borderless floating window with fade animations, multi-monitor DPI-aware cursor positioning.
- Search bar with live filtering and clear (✕) button.
- Pinned-only filter toggle.
- System tray icon, single-instance guard.

---

## [0.1.0] — 2026-01-28

### Added
- Initial project scaffold — WPF / .NET 8, MVVM structure, NuGet packages, service interfaces.
