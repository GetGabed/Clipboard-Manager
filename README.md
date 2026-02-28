# Clipboard Manager

A lightweight, high-performance Windows system tray application that captures and stores clipboard history.

## Features
- 📋 Captures text, image and file clipboard events automatically
- 🔍 Instant fuzzy search across history
- 📌 Pin favourite items to prevent eviction
- ⌨️  Global hotkey `Ctrl+Shift+V` to summon/dismiss the history window
- 🗑  Per-item delete + full clear
- ⚙️  Settings: max items, start-with-Windows, dark mode, disk persistence
- 🚀 Low footprint: target &lt;500 ms startup, &lt;50 MB RAM

## Requirements
- Windows 10 / 11
- .NET 8.0 Runtime (or SDK for development)

## Building from source
```bash
git clone https://github.com/GetGabed/Clipboard-Manager.git
cd Clipboard-Manager
dotnet build
dotnet run --project src/ClipboardManager
```

## Testing

![Tests](https://img.shields.io/badge/tests-97%20passing-brightgreen)
![Business Logic Coverage](https://img.shields.io/badge/coverage%20(Models%2C%20Helpers%2C%20Services)-≥70%25-green)

Tests are in `tests/ClipboardManager.Tests/` and run with:

```bash
dotnet test
```

### Coverage by layer (testable units, v0.6.0)

| Layer | Class | Line Coverage |
|---|---|---|
| Helpers | `CircularBuffer<T>` | 93% |
| Helpers | `TextTransformHelper` | 100% |
| Models | `AppSettings` | 100% |
| Models | `ClipboardItem` | 79% |
| Services | `ClipboardStorageService` | 71% |
| Services | `HistoryPersistenceService` | 83% |
| Services | `SettingsService` | 90% |
| ViewModels | `BaseViewModel` | 100% |

> **Note:** WPF views, Win32 hooks (`ClipboardMonitorService`, `HotkeyService`) and XAML converters are excluded from unit coverage as they require a live WPF runtime.

To regenerate the HTML coverage report:

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:"coverage/report" -reporttypes:Html
```


```
src/ClipboardManager/
├── Models/          – ClipboardItem, AppSettings
├── ViewModels/      – HistoryViewModel, SettingsViewModel (MVVM)
├── Views/           – HistoryWindow, SettingsWindow (WPF)
├── Services/        – ClipboardMonitorService, HotkeyService, SettingsService
├── Helpers/         – RelayCommand, CircularBuffer, TextTransformHelper
└── Resources/       – Styles (XAML), Icons
tests/
└── ClipboardManager.Tests/   – xUnit unit tests
```