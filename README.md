# 🚀 Winget Upgrade

A modern, high-performance, hardware-accelerated desktop client for **Windows Package Manager (`winget` CLI)** built with **.NET 8 WPF**.

![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)
![Platform: Windows 10%2F11](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6.svg)
![Framework: .NET 8 WPF](https://img.shields.io/badge/Framework-.NET%208%20WPF-512BD4.svg)
![UI: Modern Dark](https://img.shields.io/badge/Theme-Modern%20Dark%20%2F%20OLED-22C55E.svg)

---

## ✨ Key Features

- **⚡ Blazing Fast Performance:** Virtualized smooth 60fps scrolling lists (`VirtualizingStackPanel` with recycling) capable of handling hundreds of installed applications without lag.
- **🎨 Sleek Modern UI:** Fluent dark theme design with neon-green accents, custom scrollbars, and optional **Pure Black OLED Mode**.
- **📋 Integrated Sidebar Console:** Real-time live log stream directly in the sidebar with intelligent auto-scrolling to monitor background `winget` operations.
- **🛑 Emergency Force Cancel:** Dedicated cancel button to instantly and safely terminate active `winget` processes and subprocesses.
- **👁️ Package Ignore Management:**
  - Ignore any faulty or broken point-releases directly from the **3-dots action menu**.
  - Restore and manage ignored packages anytime in the **Settings** panel.
- **📦 Multi-Select & Batch Upgrades:** Check individual packages or click *Upgrade All* with one click.
- **🛡️ UAC & Elevation Ready:** Runs cleanly elevated to enable fully silent background installations without repeated prompt popups.

---

## 🛠️ Tech Stack & Requirements

- **OS:** Windows 10 (Build 1809+) or Windows 11
- **Runtime:** .NET 8.0 Desktop Runtime / SDK
- **Backend:** Windows Package Manager (`winget.exe`)
- **Architecture:** MVVM (Model-View-ViewModel) with decoupled asynchronous services

---

## 🚀 Getting Started

### 1. Clone the repository
```bash
git clone https://github.com/tusharrkabir/WingetUpgrade.git
cd WingetUpgrade
```

### 2. Run the Application
You can run the app directly using the included launch script (which auto-requests Administrator privileges):

```cmd
Run.bat
```

Or run via the .NET CLI:
```bash
dotnet run --project WingetUpgrade/WingetUpgrade.csproj
```

### 3. Build a Release Package
```bash
dotnet publish WingetUpgrade/WingetUpgrade.csproj -c Release -r win-x64 --self-contained false -o ./publish
```

---

## 📁 Project Structure

```
├── WingetUpgrade/
│   ├── Controls/          # Custom UI controls and helpers
│   ├── Converters/        # WPF XAML value converters
│   ├── Models/            # PackageItem and HistoryItem data models
│   ├── Services/          # WingetService, SettingsService, PopularAppsCatalog
│   ├── Themes/            # Styles.xaml (Dark theme, buttons, context menu), Icons.xaml
│   ├── ViewModels/        # MainViewModel (commands, async filters, observable state)
│   ├── Views/             # FullLogWindow, sub-dialogs
│   ├── App.xaml           # Application entry & elevation check
│   └── MainWindow.xaml    # Main dashboard UI & navigation
├── Run.bat                # One-click Admin launcher script
└── README.md
```

---

## 📄 License
This project is licensed under the [MIT License](LICENSE).
