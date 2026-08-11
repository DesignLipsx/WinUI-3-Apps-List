<div align="center">

<img src="FluentDeck/Assets/logo.png" alt="FluentDeck Logo" width="128" height="128"/>

# 🎛️ FluentDeck

### The Ultimate WinUI 3 App Catalog, Fluent Icons & Emoji Explorer for Windows

*Discover modern Windows software, search thousands of Microsoft Fluent System Icons, and browse animated Fluent Emojis — all in a single native desktop dashboard.*

<br/>

![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D4?style=for-the-badge&logo=windows)
![Framework](https://img.shields.io/badge/Framework-WinUI%203%20%7C%20.NET%2010-512BD4?style=for-the-badge&logo=dotnet)
![Architecture](https://img.shields.io/badge/Architecture-x64-FF8C00?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

<br/>

</div>

---

## 🌟 About FluentDeck

**FluentDeck** is a modern, native Windows 11 application designed as an interactive dashboard and catalog explorer for the WinUI 3 ecosystem. It brings together 800+ curated WinUI 3 open-source and store applications, Microsoft's complete Fluent System Icons library, and Fluent Emojis into a smooth, fluent desktop interface.

Built natively using **WinUI 3**, **.NET 10**, and **Skia Sharp (Svg.Skia)**, FluentDeck delivers real-time search, instant filtering, vector rendering, and zero-latency catalog synchronization.

---

## ✨ Key Features

### 🛍️ WinUI 3 App Catalog
- **800+ Apps Index** — Browse an extensive curated catalog of WinUI 3, Windows App SDK, and UWP applications.
- **Dual View Modes** — Seamlessly toggle between **List View** (standard detailed list) and **Grid View** (app cards with high-res WebP icons).
- **Advanced Filtering** — Filter apps by pricing (*Free, FOSS, Paid*) and UI style indicators (*WD, WDM, WDA*).
- **Direct Store & Source Links** — Open Microsoft Store or GitHub repositories directly from app cards.

### 🎨 Fluent System Icons Explorer
- **Complete Vector Library** — Search thousands of official Microsoft Fluent System Icons.
- **Multiple Style Variants** — Toggle between **Regular** (outlined), **Filled** (solid), and **Color** multi-tone variants.
- **High-Performance SVG Rendering** — Powered by `Svg.Skia` for crisp vector scaling at any resolution.

### 😀 Fluent Emoji Browser
- **Animated & Static Emojis** — Browse and search Microsoft's 3D Fluent Emojis.
- **Instant Search** — Filter emojis instantly by keyword and categories.

### 🔄 Intelligent Cloud Sync
- **On-Demand Catalog Sync** — In Store builds, click **Sync** to fetch the latest `apps_data.json` catalog and missing app logos directly from GitHub.
- **Zero-Storage Impact** — Downloaded assets are cached incrementally, avoiding bloated app package downloads.

### 🎛️ Dual-Build System (`Dev` vs `Store`)
- **Dev Build (`-c Dev`)** — Unlocks internal catalog editing, new app creation, guideline tools, and repository publishing.
- **Store Build (`-c Store`)** — Provides a streamlined end-user experience with automated online catalog sync.

---

## 🛠️ Tech Stack & Architecture

FluentDeck follows the **MVVM pattern** with decoupled controls, viewmodels, and data parsers.

```
FluentDeck/
├── FluentDeck.slnx               # Modern Visual Studio solution manifest
├── FluentDeck/
│   ├── Assets/                   # Catalog JSON metadata & WebP app logos
│   │   ├── data/
│   │   │   ├── apps_data.json
│   │   │   ├── category-metadata.json
│   │   │   └── icon_metadata.json
│   │   └── apps/                 # App logo image assets (.webp)
│   │
│   ├── Controls/                 # Custom UI controls
│   │   ├── SvgImage.cs           # Skia-based SVG vector renderer
│   │   └── ApngPlayer.cs         # Animated PNG player
│   │
│   ├── Models/                   # Data models & JSON parsers
│   │   ├── AppItem.cs
│   │   ├── AppsDataParser.cs
│   │   └── CategoryModels.cs
│   │
│   ├── Pages/                    # WinUI 3 XAML views
│   │   ├── Apps/                 # AppsPage, AppsPreviewGridPage, AppsPreviewListPage
│   │   ├── Icons/                # IconsPage, IconsGridPage
│   │   ├── Emoji/                # EmojiPage, EmojiGridPage
│   │   ├── Diagnostics/          # DiagnosticsPage
│   │   └── Settings/             # SettingsPage
│   │
│   ├── ViewModels/               # MVVM ViewModels (INotifyPropertyChanged)
│   │   ├── AppsPageViewModel.cs
│   │   ├── IconsPageViewModel.cs
│   │   └── EmojiPageViewModel.cs
│   │
│   └── Helpers/                  # Utility classes
│       ├── FeatureManager.cs     # Build configuration flag checker
│       └── SearchShortcutHelper.cs # System search keyboard shortcuts
```

### Key Dependencies

| Package | Purpose |
|---|---|
| [Microsoft.WindowsAppSDK](https://github.com/microsoft/WindowsAppSDK) | WinUI 3 native application framework |
| [CommunityToolkit.WinUI.Controls](https://github.com/CommunityToolkit/Windows) | Segmented & Settings controls |
| [Svg.Skia](https://github.com/wieslawsoltes/Svg.Skia) | Vector SVG rendering engine |
| [Microsoft.Graphics.Win2D](https://github.com/microsoft/Win2D) | DirectX 2D rendering pipeline |

---

## 📋 Prerequisites & Building

### Prerequisites
- **Windows 10** Build 19041+ or **Windows 11**
- [**.NET 10 SDK**](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Visual Studio 2022 / 2026** with **Windows App SDK** workload enabled

### 🚀 Building from Command Line

Clone the repository:
```bash
git clone https://github.com/jishnu-kv/WinUI-3-Apps-List.git
cd WinUI-3-Apps-List/FluentDeck
```

#### Build Store Version (End-User App)
```bash
dotnet build -c Store
```

#### Build Dev Version (Catalog Editor)
```bash
dotnet build -c Dev
```

#### Run App
```bash
dotnet run --project FluentDeck -c Dev
```

---

## 🤝 Contributing

Contributions, new app submissions, and feature suggestions are welcome!

1. **Fork** the repository.
2. **Create a feature branch** (`git checkout -b feature/new-app`).
3. **Commit your changes** (`git commit -m "Add NewApp to catalog"`).
4. **Push to branch** (`git push origin feature/new-app`).
5. **Open a Pull Request**.

---

## 📜 License

This project is licensed under the **MIT License** — see the [LICENSE](../LICENSE) file for details.

<div align="center">
Made with ❤️ for the Windows & WinUI community.
</div>
