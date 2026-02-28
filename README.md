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

## Architecture
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