# DiscordNative

A fast, lightweight, and fully native Discord desktop client engineered for **Windows 11 & Windows 10** using **C# (.NET 8)**, **WinUI 3**, and the **Windows App SDK** — with zero Electron, CEF, or WebView wrappers.

![DiscordNative Showcase](docs/assets/banner1.gif)

---

## Highlights

<!-- demo: preview and animated showcase -->
![Interface Demo](docs/assets/banner2.gif)

- **Fluent Design & Personalization**: Deep integration with Windows 11 design standards, including Mica / Acrylic materials (`SystemBackdrop`), dark and light mode synchronization, rounded geometries, and customizable wallpaper background with adjustable blur and opacity inspired by [Files](https://github.com/files-community/Files).
- **Lightweight Architecture**: Instant startup and reduced memory usage compared to Chromium-based clients.
- **Multilingual Support**: Extensible localization architecture (`en-US`, `fr-FR`).
- **Core Discord Experience**: Real-time WebSocket Gateway v9 connection, REST endpoints, quick switcher, rich profile popouts, reaction management, and voice connectivity.

---

## Architecture

```
DiscordNative.sln
├── src/
│   ├── DiscordNative.Protocol/       Standalone .NET class library (Gateway, REST, Models)
│   └── DiscordNative.App/            WinUI 3 application with MVVM architecture
└── docs/
    └── assets/                       Showcase assets and media
```

---

## Installation & Distribution

### 1. WinUI 3 Installer (`DiscordNative-Setup-v0.2.0-win-x64.zip`)
- Download and extract the installer bundle from [Releases](https://github.com/djell1l/DiscordNative/releases).
- Launch `DiscordNative.Installer.exe` — a modern Windows 11 setup wizard written in C# and WinUI 3.
- Custom installation path, automatic Desktop and Start Menu shortcut creation.

### 2. Standalone Portable Package (`DiscordNative-v0.2.0-win-x64-portable.zip`)
- Extract anywhere.
- Run `DiscordNative.App.exe` directly without setup.

### 3. Automatic Updates
DiscordNative includes an integrated WinUI 3 `InfoBar` notification banner that checks GitHub Releases on startup and allows you to download new updates in one click.

---

## Building from Source

### Prerequisites
- Windows 10 (version 1809+) or Windows 11
- .NET 8 SDK
- Visual Studio 2022 (v17.10+) with **.NET Desktop Development** and **Windows App SDK** workloads

### Build & Run
```powershell
# Clone the repository
git clone https://github.com/djell1l/DiscordNative.git
cd DiscordNative

# Compile and launch
.\build.ps1
```

---

## Contributors & Credits

- **[djell1l](https://github.com/djell1l)** — Lead developer & creator
- **Claude** & **Gemini** (via **Antigravity**) — AI pair programming collaborators and architecture co-design
- **[SakuraCord](https://github.com/SakuraCordApp/SakuraCord)** — Concept and native client philosophy
- **[Files](https://github.com/files-community/Files)** — UI/UX inspiration for WinUI 3 controls and background customization

---

## Disclaimer

DiscordNative is an unofficial third-party client built for experimental and educational purposes. Using third-party clients may be against Discord's Terms of Service. Use at your own discretion.
