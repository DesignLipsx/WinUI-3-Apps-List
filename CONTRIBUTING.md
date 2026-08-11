# Contributing to the WinUI Apps List

Thank you for your interest in contributing! We welcome submissions to expand and improve our curated list of WinUI-aligned applications.

To ensure consistency and ease of parsing (including rendering on the [Fluentdeck website](https://fluentdeck.vercel.app/apps)), please follow these guidelines when adding new apps.

---

## 🛠️ Step-by-Step Guide

1. **Fork the Repository**: Create a fork of this repository to your own GitHub account.
2. **Add Your App**: Place your app in the appropriate section(s) within [README.md](README.md) following the strict alphabetical sorting and format guidelines below.
3. **Add to Newly Added Apps**: Make sure to also add your new entry to the `### 🆕 Newly Added Apps!` section at the top of the README, keeping it sorted alphabetically.
4. **Submit a Pull Request**: Push your changes to your fork and open a pull request back to this repository.

---

## 📝 Format Guidelines

Every app entry must adhere to the following template:

```markdown
- `INDICATOR` [AppName](AppURL) <sup>`FOSS`</sup> <!-- logo: LogoURL -->
```

### 1. Indicators
Specify the design implementation using one of the core abbreviations:
* `WD`: Apps matching the WinUI 3 Design Only.
* `WDM`: Apps utilizing WinUI 3 design and Mica Material.
* `WDA`: Apps utilizing WinUI 3 design and Acrylic Material.

### 2. Badges
* Use `<sup>`FOSS`</sup>` if the application is Free and Open Source Software.
* Use `💰` if the app is paid.
* Use `🎨` if it is a theme.
* Use `📆 Planned` for planned or in-development apps.
* Use `❎ Discontinued` if the application has been discontinued.

### 3. Logo URLs (Used by Fluentdeck)
* You can add a logo link inside an HTML comment at the end of your app entry line in `README.md`: `<!-- logo: LogoURL -->`.
  * A GitHub Actions workflow will automatically extract the logo comment from `README.md` and sync it into `apps_data.json`.
* Alternatively, you can add or edit logo entries directly inside `apps_data.json`.
* **Important**: Please use raw content/direct URLs (e.g., `https://raw.githubusercontent.com/...` or store image URLs) so they render correctly on the website.
* If no logo is available, omit the logo comment / entry entirely.

---

## 💡 Examples

### 1. Open Source App with Logo (Mica Material)
```markdown
- `WDM` [Ambie](https://github.com/jenius-apps/ambie) <sup>`FOSS`</sup> <!-- logo: https://raw.githubusercontent.com/jenius-apps/ambie/refs/heads/main/images/logo_transparent.png -->
```

### 2. Store App (Paid, WinUI Design Only)
```markdown
- `WD` [SpectroTime](https://apps.microsoft.com/detail/9p5mxj239vml) `💰` <!-- logo: https://store-images.s-microsoft.com/image/apps.25120.14570648972267707.c29ed1c9-0f44-46e7-a403-e05af7f471fd.22cb3a32-a2d5-4a26-81b8-de82943bce79?h=115 -->
```

### 3. Planned App without Logo
```markdown
- `WD` [ClipCore](https://github.com/Kleaopsy/ClipCore) `📆 Planned` <sup>`FOSS`</sup>
```
